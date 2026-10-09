using Eurocar.Api.Domain;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Integration;

/// <summary>Source of stock and online status for partner/dealer yards. Real adapters (IT-Enterprise, CSV) plug in here later.</summary>
public interface IExternalStockProvider
{
    Task PollAsync(AppDb db, CancellationToken ct);
}

/// <summary>Port of the prototype's feed(): add or remove one car in a random online yard, refresh sync times.</summary>
public sealed class SimulatedExternalStockProvider : IExternalStockProvider
{
    readonly TimeProvider _clock;
    readonly Random _rnd;

    public SimulatedExternalStockProvider(TimeProvider clock) : this(clock, Random.Shared) { }
    public SimulatedExternalStockProvider(TimeProvider clock, Random rnd) { _clock = clock; _rnd = rnd; }

    public async Task PollAsync(AppDb db, CancellationToken ct)
    {
        var live = await db.Zones.Where(z => z.Kind == ZoneKind.External && !z.IsDeleted && z.IsOnline).OrderBy(z => z.Id).ToListAsync(ct);
        if (live.Count == 0) return;
        var zone = live[_rnd.Next(live.Count)];

        var count = await db.Cars.CountAsync(c => c.ZoneId == zone.Id, ct);
        // Cars with journal history (or in a move) are never removed.
        var removable = await db.Cars
            .Where(c => c.ZoneId == zone.Id && c.Status == CarStatus.AtExternal && !db.Moves.Any(m => m.CarId == c.Id))
            .OrderBy(c => c.Id).ToListAsync(ct);
        var add = count < zone.Capacity && (removable.Count == 0 || _rnd.NextDouble() < 0.5);
        if (add)
        {
            string vin;
            do vin = CarCatalog.MakeVin(_rnd); while (await db.Cars.AnyAsync(c => c.Vin == vin, ct));
            db.Cars.Add(new Car
            {
                Vin = vin, Status = CarStatus.AtExternal, ZoneId = zone.Id,
                Model = CarCatalog.Models[_rnd.Next(CarCatalog.Models.Length)],
                Color = CarCatalog.Colors[_rnd.Next(CarCatalog.Colors.Length)],
            });
        }
        else if (removable.Count > 0)
        {
            db.Cars.Remove(removable[_rnd.Next(removable.Count)]);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        zone.LastSyncedAt = now;
        for (var i = 0; i < 4; i++) live[_rnd.Next(live.Count)].LastSyncedAt = now;
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ExternalStockPoller(IServiceScopeFactory scopes, IExternalStockProvider provider, IConfiguration cfg, ILogger<ExternalStockPoller> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!cfg.GetValue("ExternalFeed:Enabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(cfg.GetValue("ExternalFeed:IntervalSeconds", 9)));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDb>();
                var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
                if (settings is null || !settings.ExternalFeedEnabled) continue;
                await provider.PollAsync(db, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogWarning(e, "External stock poll failed");
            }
        }
    }
}
