namespace Eurocar.Api.Domain;

/// <summary>Spot grid rules shared with the Angular client (frontend/src/app/shared/zone-layout.ts).</summary>
public static class ZoneLayout
{
    public const string Letters = "ABCDEFGHIJKLMNOPQRST";
    public const int M2PerCar = 25;
    public const int MaxCapacity = 300;

    public static int CapacityFromArea(int areaM2) => Math.Min(MaxCapacity, Math.Max(0, areaM2) / M2PerCar);

    public static (int Rows, int PerRow) LayoutFor(int capacity)
    {
        var per = Math.Clamp((int)Math.Ceiling(Math.Sqrt(capacity * 0.7)), 5, 15);
        var rows = (int)Math.Ceiling(capacity / (double)per);
        if (rows % 2 == 1) rows++;
        return (Math.Min(20, rows), per);
    }

    public static IReadOnlyList<string> SpotIds(int capacity, int rows, int perRow)
    {
        var ids = new List<string>(capacity);
        for (var r = 0; r < rows && ids.Count < capacity; r++)
            for (var n = 1; n <= perRow && ids.Count < capacity; n++)
                ids.Add($"{Letters[r]}{n}");
        return ids;
    }

    public static string? NormalizeSpot(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
    }
}
