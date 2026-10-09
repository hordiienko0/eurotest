using Eurocar.Api.Domain;

namespace Eurocar.Tests;

public class DemoSeederTests
{
    [Fact]
    public void Seed_creates_prototype_world_without_constraint_violations()
    {
        using var t = TestDb.Create(seedDemo: true);
        var db = t.Db;

        Assert.Equal(8, db.ZoneGroups.Count());
        Assert.Equal(39, db.Zones.Count()); // 9 own + 14 partners + 16 dealers
        Assert.Equal(1200, db.KeySlots.Count());
        Assert.Equal(3, db.Operators.Count());
        Assert.Equal(3, db.Moves.Count(m => m.State == MoveState.Completed));
        Assert.False(db.Zones.Single(z => z.Code == "P07").IsOnline);
        Assert.Equal(db.Cars.Count(c => c.Status == CarStatus.Parked), db.KeySlots.Count(s => s.Status == KeySlotStatus.InBox));
        Assert.InRange(db.Cars.Count(), 1500, 2600);
    }
}
