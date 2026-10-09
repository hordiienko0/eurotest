using Eurocar.Api.Domain;
using Eurocar.Api.Features;
using Eurocar.Api.Features.Zones;

namespace Eurocar.Tests;

public class ZoneServiceTests : IDisposable
{
    readonly TestDb _t = TestDb.Create(seedDemo: true);
    readonly ZoneService _svc;

    public ZoneServiceTests() => _svc = new ZoneService(_t.Db);
    public void Dispose() => _t.Dispose();

    static async Task<string> CodeOf(Func<Task> act) => (await Assert.ThrowsAsync<DomainException>(act)).Code;

    [Fact]
    public async Task Overview_groups_all_zones_with_totals()
    {
        var o = await _svc.OverviewAsync();
        Assert.Equal(8, o.Groups.Count);
        Assert.Equal(39, o.Groups.Sum(g => g.Zones.Count));
        Assert.Equal(1290, o.Totals.OwnCap);
        Assert.Equal(29, o.Totals.Online);
        Assert.Equal(3, o.Totals.Ops);
    }

    [Fact]
    public async Task Detail_of_mapped_zone_lists_every_spot()
    {
        var d = await _svc.DetailAsync("T2");
        Assert.Equal(60, d.Spots.Count);
        Assert.Equal(d.Used, d.Spots.Count(s => s.Car is not null));
    }

    [Fact]
    public async Task Create_temporary_zone_gets_next_T_code_and_layout()
    {
        var z = await _svc.CreateAsync(new ZoneUpsertDto("3.2", " Поле за терміналом ", 1000, Priority.Normal));
        Assert.Equal("T3", z.Code);
        Assert.Equal("Поле за терміналом", z.Name);
        Assert.Equal(40, z.Capacity);
    }

    [Fact]
    public async Task Shrinking_zone_reassigns_cars_on_removed_spots()
    {
        var before = (await _svc.DetailAsync("T2")).Used;
        Assert.True(before <= 40);

        await _svc.UpdateAsync("T2", new ZoneUpsertDto("3.2", "Поле біля траси М-06", 1000, Priority.Low));

        using var db = _t.NewContext();
        var zone = db.Zones.Single(z => z.Code == "T2");
        var valid = ZoneLayout.SpotIds(40, 8, 6).ToHashSet();
        var spots = db.Cars.Where(c => c.ZoneId == zone.Id).Select(c => c.SpotCode!).ToList();
        Assert.Equal(40, zone.Capacity);
        Assert.Equal(before, spots.Count);
        Assert.All(spots, s => Assert.Contains(s, valid));
        Assert.Equal(spots.Count, spots.Distinct().Count());
    }

    [Fact]
    public async Task Shrinking_below_occupancy_is_rejected()
        => Assert.Equal("CapacityBelowOccupancy", await CodeOf(() => _svc.UpdateAsync("T1", new ZoneUpsertDto("3.2", "Майданчик-1", 25, Priority.Normal))));

    [Fact]
    public async Task Tiny_area_is_rejected()
        => Assert.Equal("AreaTooSmall", await CodeOf(() => _svc.CreateAsync(new ZoneUpsertDto("3.3", "Мала", 20, Priority.Normal))));

    [Fact]
    public async Task Own_zones_are_not_editable()
        => Assert.Equal("ZoneNotEditable", await CodeOf(() => _svc.UpdateAsync("S1", new ZoneUpsertDto("3.1", "S1", 7500, Priority.High))));

    [Fact]
    public async Task Delete_non_empty_zone_is_rejected()
        => Assert.Equal("ZoneNotEmpty", await CodeOf(() => _svc.DeleteAsync("T1")));

    [Fact]
    public async Task Delete_empty_zone_hides_it()
    {
        var z = await _svc.CreateAsync(new ZoneUpsertDto("3.3", "Транзит · ворота 3", 500, Priority.Normal));
        await _svc.DeleteAsync(z.Code);
        var o = await _svc.OverviewAsync();
        Assert.DoesNotContain(o.Groups.SelectMany(g => g.Zones), x => x.Code == z.Code);
        Assert.Equal("NotFound", await CodeOf(() => _svc.DetailAsync(z.Code)));
    }
}
