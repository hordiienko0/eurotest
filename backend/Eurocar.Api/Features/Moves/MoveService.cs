using Eurocar.Api.Domain;
using Eurocar.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Features.Moves;

/// <summary>
/// Move lifecycle: (client-only "selected") → Start → InTransit → SetTarget/Photo → Complete | Cancel.
/// </summary>
public class MoveService(AppDb db, IPhotoStorage photos, TimeProvider clock)
{
    const long MaxPhotoBytes = 10 * 1024 * 1024;
    const int SqliteConstraint = 19;
    // Raster formats only: SVG is served from our origin and could carry script.
    static readonly HashSet<string> PhotoTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/heic", "image/heif" };

    DateTime Now => clock.GetUtcNow().UtcDateTime;

    IQueryable<Move> Full() => db.Moves
        .Include(m => m.Car).ThenInclude(c => c.Zone)
        .Include(m => m.Car).ThenInclude(c => c.KeySlot)
        .Include(m => m.Operator)
        .Include(m => m.FromZone)
        .Include(m => m.ToZone)
        .Include(m => m.Photo);

    public async Task<MoveDto> GetAsync(int id) =>
        Map.Move(await Full().AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ?? throw Errors.NotFound("Операцію"));

    public async Task<MoveDto?> ActiveAsync(int operatorId)
    {
        var m = await Full().AsNoTracking().FirstOrDefaultAsync(x => x.OperatorId == operatorId && x.State == MoveState.InTransit);
        return m is null ? null : Map.Move(m);
    }

    public async Task<IReadOnlyList<MoveDto>> JournalAsync(int take = 200) =>
        (await Full().AsNoTracking().Where(m => m.State == MoveState.Completed).OrderByDescending(m => m.No).Take(take).ToListAsync())
        .Select(Map.Move).ToList();

    public async Task<MoveDto> StartAsync(int carId, int operatorId)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == operatorId && o.IsActive)) throw Errors.OperatorRequired();
        if (await db.Moves.AnyAsync(m => m.OperatorId == operatorId && m.State == MoveState.InTransit)) throw Errors.ActiveMoveExists();
        var car = await db.Cars.Include(c => c.KeySlot).FirstOrDefaultAsync(c => c.Id == carId) ?? throw Errors.NotFound("Авто");
        if (car.Status == CarStatus.InTransit || car.ZoneId is null) throw Errors.CarBusy();

        var move = new Move
        {
            CarId = car.Id, OperatorId = operatorId, FromZoneId = car.ZoneId.Value, FromSpot = car.SpotCode,
            State = MoveState.InTransit, StartedAt = Now,
        };
        car.Status = CarStatus.InTransit;
        car.ZoneId = null;
        car.SpotCode = null;
        if (car.KeySlot is not null) car.KeySlot.Status = KeySlotStatus.Issued;
        db.Moves.Add(move);
        await SaveAsync(Errors.CarBusy);
        return await GetAsync(move.Id);
    }

    public async Task<MoveDto> SetTargetAsync(int id, int operatorId, SetTargetDto d)
    {
        var m = await LoadActiveAsync(id, operatorId);
        if (string.IsNullOrWhiteSpace(d.ZoneCode))
        {
            m.ToZone = null;
            m.ToZoneId = null;
            m.ToSpot = null;
        }
        else
        {
            var zone = await db.Zones.FirstOrDefaultAsync(z => z.Code == d.ZoneCode && !z.IsDeleted) ?? throw Errors.NotFound($"Зону {d.ZoneCode}");
            string? spot = null;
            if (zone.Kind == ZoneKind.External)
            {
                if (!zone.IsOnline) throw Errors.ZoneOffline();
                await EnsureExternalHasRoomAsync(zone);
            }
            else
            {
                spot = ZoneLayout.NormalizeSpot(d.SpotCode) ?? throw Errors.SpotRequired();
                if (!ZoneLayout.SpotIds(zone.Capacity, zone.Rows!.Value, zone.PerRow!.Value).Contains(spot)) throw Errors.SpotNotInZone(zone.Code, spot);
                if (zone.Id == m.FromZoneId && spot == m.FromSpot) throw Errors.SpotIsOrigin();
                if (await IsSpotTakenAsync(zone.Id, spot, m.Id)) throw Errors.SpotOccupied(spot);
            }
            m.ToZone = zone;
            m.ToZoneId = zone.Id;
            m.ToSpot = spot;
        }
        await SaveAsync(() => Errors.SpotOccupied(m.ToSpot ?? ""));
        return await GetAsync(m.Id);
    }

    public async Task<MoveDto> AttachPhotoAsync(int id, int operatorId, Stream content, string contentType, long length)
    {
        if (length <= 0 || length > MaxPhotoBytes || !PhotoTypes.Contains(contentType))
            throw Errors.PhotoInvalid();
        var m = await LoadActiveAsync(id, operatorId);
        var key = await photos.SaveAsync(content, contentType);
        var old = m.Photo;
        m.Photo = new Photo { StorageKey = key, ContentType = contentType, TakenAt = Now, SizeBytes = length };
        await db.SaveChangesAsync();
        if (old is not null)
        {
            db.Photos.Remove(old);
            await db.SaveChangesAsync();
            photos.Delete(old.StorageKey);
        }
        return await GetAsync(m.Id);
    }

    public async Task<MoveDto> CompleteAsync(int id, int operatorId)
    {
        var m = await LoadActiveAsync(id, operatorId);
        if (m.ToZone is null) throw Errors.TargetRequired();
        var settings = await db.Settings.AsNoTracking().FirstAsync();
        if (settings.RequirePhoto && m.PhotoId is null) throw Errors.PhotoRequired();

        var car = m.Car;
        var to = m.ToZone;
        if (to.Kind == ZoneKind.Mapped)
        {
            if (await IsSpotTakenAsync(to.Id, m.ToSpot!, m.Id)) throw Errors.SpotOccupied(m.ToSpot!);
            car.Status = CarStatus.Parked;
            car.ZoneId = to.Id;
            car.SpotCode = m.ToSpot;
            car.KeySlot ??= await db.KeySlots.Where(s => s.Status == KeySlotStatus.Empty)
                .OrderBy(s => s.Cabinet).ThenBy(s => s.Number).FirstOrDefaultAsync() ?? throw Errors.KeyBoxFull();
            car.KeySlot.Status = KeySlotStatus.InBox;
            m.KeyLabel = car.KeySlot.Label;
        }
        else
        {
            await EnsureExternalHasRoomAsync(to);
            car.Status = CarStatus.AtExternal;
            car.ZoneId = to.Id;
            car.SpotCode = null;
            if (car.KeySlot is not null)
            {
                car.KeySlot.Status = KeySlotStatus.Empty;
                car.KeySlot = null;
                car.KeySlotId = null;
            }
            m.KeyLabel = "передано з авто";
        }

        m.No = (await db.Moves.MaxAsync(x => x.No) ?? 0) + 1;
        m.State = MoveState.Completed;
        m.CompletedAt = Now;
        await SaveAsync(() => Errors.SpotOccupied(m.ToSpot ?? to.Code));
        return await GetAsync(m.Id);
    }

    public async Task<MoveDto> CancelAsync(int id, int operatorId, string? fallbackSpot)
    {
        var m = await LoadActiveAsync(id, operatorId);
        var car = m.Car;
        var from = m.FromZone;
        if (from.Kind == ZoneKind.External)
        {
            car.Status = CarStatus.AtExternal;
            car.ZoneId = from.Id;
            car.SpotCode = null;
        }
        else
        {
            var fallback = ZoneLayout.NormalizeSpot(fallbackSpot);
            var spot = fallback ?? m.FromSpot!;
            var valid = ZoneLayout.SpotIds(from.Capacity, from.Rows!.Value, from.PerRow!.Value);
            if (fallback is not null && !valid.Contains(fallback)) throw Errors.SpotNotInZone(from.Code, fallback);
            if (!valid.Contains(spot)) throw Errors.OriginGone(spot);
            if (await IsSpotTakenAsync(from.Id, spot, m.Id))
                throw fallback is null ? Errors.OriginTaken(spot) : Errors.SpotOccupied(spot);
            car.Status = CarStatus.Parked;
            car.ZoneId = from.Id;
            car.SpotCode = spot;
        }
        if (car.KeySlot is not null) car.KeySlot.Status = KeySlotStatus.InBox;
        m.State = MoveState.Cancelled;
        m.CompletedAt = Now;
        m.ToZone = null;
        m.ToZoneId = null;
        m.ToSpot = null;
        await SaveAsync(Errors.Conflict);
        return await GetAsync(m.Id);
    }

    async Task<Move> LoadActiveAsync(int id, int operatorId)
    {
        var m = await db.Moves
            .Include(x => x.Car).ThenInclude(c => c.KeySlot)
            .Include(x => x.FromZone).Include(x => x.ToZone).Include(x => x.Photo)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw Errors.NotFound("Операцію");
        if (m.State != MoveState.InTransit) throw Errors.Conflict();
        if (m.OperatorId != operatorId) throw Errors.NotYourMove();
        return m;
    }

    async Task EnsureExternalHasRoomAsync(Zone zone)
    {
        if (await db.Cars.CountAsync(c => c.ZoneId == zone.Id) >= zone.Capacity) throw Errors.ZoneFull();
    }

    async Task<bool> IsSpotTakenAsync(int zoneId, string spot, int exceptMoveId) =>
        await db.Cars.AnyAsync(c => c.ZoneId == zoneId && c.SpotCode == spot)
        || await db.Moves.AnyAsync(x => x.Id != exceptMoveId && x.State == MoveState.InTransit && x.ToZoneId == zoneId && x.ToSpot == spot);

    /// <summary>Unique indexes are the last line of defence against two devices racing for the same spot/key.</summary>
    async Task SaveAsync(Func<DomainException> onConflict)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteErrorCode: SqliteConstraint } se)
        {
            throw se.Message.Contains("KeySlotId") ? Errors.KeySlotTaken() : onConflict();
        }
    }
}
