using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Features.Cars;

public static class CarQueries
{
    public static async Task<IReadOnlyList<CarDto>> SearchAsync(AppDb db, string? q)
    {
        var needle = new string((q ?? "").Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (needle.Length < 3) return [];
        var cars = await db.Cars.AsNoTracking().Include(c => c.Zone).Include(c => c.KeySlot)
            .Where(c => c.Vin.Contains(needle)).OrderBy(c => c.Id).Take(8).ToListAsync();
        return cars.Select(Map.Car).ToList();
    }
}
