using Eurocar.Api.Domain;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Features.Zones;

public class ZoneService(AppDb db)
{
    public async Task<ZonesOverviewDto> OverviewAsync()
    {
        var groups = await db.ZoneGroups.AsNoTracking().OrderBy(g => g.Code).ToListAsync();
        var zones = await db.Zones.AsNoTracking().Where(z => !z.IsDeleted).OrderBy(z => z.Id).ToListAsync();
        var used = await UsedByZoneAsync();
        var editable = groups.Where(g => g.IsEditable).Select(g => g.Code).ToHashSet();
        int Used(Zone z) => used.GetValueOrDefault(z.Id);

        var own = zones.Where(z => z.Kind == ZoneKind.Mapped).ToList();
        var ext = zones.Where(z => z.Kind == ZoneKind.External).ToList();
        var totals = new TotalsDto(own.Sum(Used), own.Sum(z => z.Capacity), ext.Sum(Used), ext.Sum(z => z.Capacity),
            ext.Count(z => z.IsOnline), ext.Count, await db.Moves.CountAsync(m => m.State == MoveState.Completed));

        return new ZonesOverviewDto(totals, groups.Select(g => new ZoneGroupDto(g.Code, g.Name, g.IsEditable,
            zones.Where(z => z.GroupCode == g.Code).Select(z => Map.ZoneRow(z, Used(z), editable.Contains(z.GroupCode))).ToList())).ToList());
    }

    public async Task<ZoneDetailDto> DetailAsync(string code)
    {
        var z = await FindAsync(code, track: false);
        var group = await db.ZoneGroups.AsNoTracking().SingleAsync(g => g.Code == z.GroupCode);
        var cars = await db.Cars.AsNoTracking().Include(c => c.Zone).Include(c => c.KeySlot)
            .Where(c => c.ZoneId == z.Id).OrderBy(c => c.Id).ToListAsync();

        var spots = new List<SpotDto>();
        if (z.Kind == ZoneKind.Mapped)
        {
            var bySpot = cars.Where(c => c.SpotCode != null).ToDictionary(c => c.SpotCode!);
            var reserved = (await db.Moves
                .Where(m => m.State == MoveState.InTransit && m.ToZoneId == z.Id && m.ToSpot != null)
                .Select(m => m.ToSpot!).ToListAsync()).ToHashSet();
            spots = ZoneLayout.SpotIds(z.Capacity, z.Rows!.Value, z.PerRow!.Value)
                .Select(s => new SpotDto(s, bySpot.TryGetValue(s, out var c) ? Map.Car(c) : null, reserved.Contains(s)))
                .ToList();
        }

        return new ZoneDetailDto(z.Code, z.Name, z.GroupCode, group.Name, z.Kind, z.Capacity, cars.Count, z.Rows, z.PerRow,
            z.AreaM2, z.Priority, z.StripLabel, z.IsOnline, Map.Utc(z.LastSyncedAt), spots,
            z.Kind == ZoneKind.External ? cars.Select(Map.Car).ToList() : []);
    }

    public async Task<ZoneRowDto> CreateAsync(ZoneUpsertDto d)
    {
        var group = await EditableGroupAsync(d.GroupCode);
        var (name, capacity) = Validate(d, used: 0);
        var prefix = group.Code == "3.2" ? "T" : "X";
        var codes = (await db.Zones.Select(z => z.Code).ToListAsync()).ToHashSet();
        var n = 1;
        while (codes.Contains(prefix + n)) n++;

        var (rows, perRow) = ZoneLayout.LayoutFor(capacity);
        var zone = new Zone
        {
            Code = prefix + n, GroupCode = group.Code, Name = name, Kind = ZoneKind.Mapped, Capacity = capacity,
            Rows = rows, PerRow = perRow, AreaM2 = d.AreaM2, Priority = d.Priority, StripLabel = "В’ЇЗД",
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync();
        return Map.ZoneRow(zone, 0, editable: true);
    }

    public async Task<ZoneRowDto> UpdateAsync(string code, ZoneUpsertDto d)
    {
        var zone = await FindAsync(code, track: true);
        await EditableGroupAsync(zone.GroupCode);
        var cars = await db.Cars.Where(c => c.ZoneId == zone.Id).OrderBy(c => c.Id).ToListAsync();
        var (name, capacity) = Validate(d, cars.Count);

        zone.Name = name;
        zone.AreaM2 = d.AreaM2;
        zone.Priority = d.Priority;
        if (capacity != zone.Capacity)
        {
            var (rows, perRow) = ZoneLayout.LayoutFor(capacity);
            zone.Capacity = capacity;
            zone.Rows = rows;
            zone.PerRow = perRow;
            var valid = ZoneLayout.SpotIds(capacity, rows, perRow);
            var validSet = valid.ToHashSet();

            // Reservations on spots that no longer exist are dropped; the operator picks again.
            var reservations = await db.Moves.Where(m => m.State == MoveState.InTransit && m.ToZoneId == zone.Id).ToListAsync();
            foreach (var m in reservations.Where(m => m.ToSpot != null && !validSet.Contains(m.ToSpot)))
            {
                m.ToZoneId = null;
                m.ToSpot = null;
            }

            var taken = cars.Where(c => c.SpotCode != null && validSet.Contains(c.SpotCode)).Select(c => c.SpotCode!)
                .Concat(reservations.Where(m => m.ToZoneId == zone.Id && m.ToSpot != null).Select(m => m.ToSpot!))
                .ToHashSet();
            var free = new Queue<string>(valid.Where(s => !taken.Contains(s)));
            foreach (var car in cars.Where(c => c.SpotCode == null || !validSet.Contains(c.SpotCode)))
                car.SpotCode = free.TryDequeue(out var spot) ? spot : throw Errors.CapacityBelowOccupancy(cars.Count);
        }

        await db.SaveChangesAsync();
        return Map.ZoneRow(zone, cars.Count, editable: true);
    }

    public async Task DeleteAsync(string code)
    {
        var zone = await FindAsync(code, track: true);
        await EditableGroupAsync(zone.GroupCode);
        var busy = await db.Cars.AnyAsync(c => c.ZoneId == zone.Id)
            || await db.Moves.AnyAsync(m => m.State == MoveState.InTransit && (m.ToZoneId == zone.Id || m.FromZoneId == zone.Id));
        if (busy) throw Errors.ZoneNotEmpty();
        zone.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    Task<Dictionary<int, int>> UsedByZoneAsync() => db.Cars
        .Where(c => c.ZoneId != null)
        .GroupBy(c => c.ZoneId!.Value)
        .Select(g => new { g.Key, N = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.N);

    async Task<Zone> FindAsync(string code, bool track)
    {
        var query = track ? db.Zones : db.Zones.AsNoTracking();
        return await query.FirstOrDefaultAsync(z => z.Code == code && !z.IsDeleted) ?? throw Errors.NotFound($"Зону {code}");
    }

    async Task<ZoneGroup> EditableGroupAsync(string groupCode)
    {
        var g = await db.ZoneGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Code == groupCode);
        return g is { IsEditable: true } ? g : throw Errors.ZoneNotEditable();
    }

    static (string Name, int Capacity) Validate(ZoneUpsertDto d, int used)
    {
        var name = d.Name?.Trim() ?? "";
        if (name.Length == 0) throw Errors.Validation("Вкажіть назву.");
        var capacity = ZoneLayout.CapacityFromArea(d.AreaM2);
        if (capacity < 1) throw Errors.AreaTooSmall();
        if (capacity < used) throw Errors.CapacityBelowOccupancy(used);
        return (name, capacity);
    }
}
