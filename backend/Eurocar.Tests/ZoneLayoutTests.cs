using Eurocar.Api.Domain;

namespace Eurocar.Tests;

public class ZoneLayoutTests
{
    [Theory]
    [InlineData(1000, 40)]
    [InlineData(2500, 100)]
    [InlineData(10000, 300)]
    [InlineData(20, 0)]
    public void CapacityFromArea_uses_25_m2_per_car_capped_at_300(int area, int expected)
        => Assert.Equal(expected, ZoneLayout.CapacityFromArea(area));

    [Theory]
    [InlineData(60, 10, 7)]
    [InlineData(40, 8, 6)]
    [InlineData(300, 20, 15)]
    public void LayoutFor_matches_prototype(int capacity, int rows, int perRow)
        => Assert.Equal((rows, perRow), ZoneLayout.LayoutFor(capacity));

    [Fact]
    public void SpotIds_fill_rows_until_capacity()
    {
        var ids = ZoneLayout.SpotIds(40, 8, 6);
        Assert.Equal(40, ids.Count);
        Assert.Equal("A1", ids[0]);
        Assert.Equal("A6", ids[5]);
        Assert.Equal("B1", ids[6]);
        Assert.Equal("G4", ids[^1]);
    }

    [Theory]
    [InlineData(" d 7 ", "D7")]
    [InlineData("b12", "B12")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void NormalizeSpot_strips_spaces_and_uppercases(string? input, string? expected)
        => Assert.Equal(expected, ZoneLayout.NormalizeSpot(input));
}
