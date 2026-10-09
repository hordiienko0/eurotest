using Eurocar.Api.Domain;
using Eurocar.Api.Features.Cars;
using Eurocar.Api.Integration;

namespace Eurocar.Tests;

public class SimulatorTests
{
    [Fact]
    public async Task Simulator_keeps_history_cars_and_capacity()
    {
        using var t = TestDb.Create(seedDemo: true);
        var sim = new SimulatedExternalStockProvider(TimeProvider.System, new Random(1));
        var withHistory = t.Db.Moves.Select(m => m.CarId).ToHashSet();
        var p07Synced = t.Db.Zones.Single(z => z.Code == "P07").LastSyncedAt;

        for (var i = 0; i < 300; i++) await sim.PollAsync(t.Db, CancellationToken.None);

        using var db = t.NewContext();
        Assert.All(withHistory, id => Assert.True(db.Cars.Any(c => c.Id == id)));
        foreach (var z in db.Zones.Where(z => z.Kind == ZoneKind.External).ToList())
            Assert.True(db.Cars.Count(c => c.ZoneId == z.Id) <= z.Capacity, $"{z.Code} over capacity");
        Assert.Equal(p07Synced, db.Zones.Single(z => z.Code == "P07").LastSyncedAt);
    }

    [Fact]
    public async Task Search_ignores_case_and_spaces_and_needs_three_chars()
    {
        using var t = TestDb.Create(seedDemo: true);
        var vin = t.Db.Cars.OrderBy(c => c.Id).First().Vin;
        var part = vin.Substring(3, 6).ToLowerInvariant();

        var found = await CarQueries.SearchAsync(t.Db, $" {part[..3]} {part[3..]} ");

        Assert.Contains(found, c => c.Vin == vin);
        Assert.True(found.Count <= 8);
        Assert.Empty(await CarQueries.SearchAsync(t.Db, vin[^2..]));
    }
}
