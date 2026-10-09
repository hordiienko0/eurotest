using Eurocar.Api.Domain;
using Eurocar.Api.Features.Cars;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Features;

public static class MiscEndpoints
{
    public static void MapMisc(this RouteGroupBuilder api)
    {
        api.MapGet("/cars/search", (string? vin, AppDb db) => CarQueries.SearchAsync(db, vin));
        api.MapGet("/cars/{id:int}", async (int id, AppDb db) =>
            Map.Car(await db.Cars.AsNoTracking().Include(c => c.Zone).Include(c => c.KeySlot).FirstOrDefaultAsync(c => c.Id == id)
                    ?? throw Errors.NotFound("Авто")));

        api.MapGet("/keys/summary", async (AppDb db) =>
        {
            var slots = await db.KeySlots.AsNoTracking().Select(s => new { s.Cabinet, s.Status }).ToListAsync();
            return new KeysSummaryDto(
                slots.Count(s => s.Status == KeySlotStatus.InBox),
                slots.Count(s => s.Status == KeySlotStatus.Issued),
                slots.Count(s => s.Status == KeySlotStatus.Empty),
                slots.GroupBy(s => s.Cabinet).OrderBy(g => g.Key)
                    .Select(g => new CabinetCountDto(g.Key, g.Count(s => s.Status != KeySlotStatus.Empty))).ToList());
        });
        api.MapGet("/keys/cabinets/{n:int}", async (int n, AppDb db) =>
        {
            var slots = await db.KeySlots.AsNoTracking().Where(s => s.Cabinet == n).OrderBy(s => s.Number).ToListAsync();
            if (slots.Count == 0) throw Errors.NotFound($"Шафу {n}");
            var ids = slots.Select(s => s.Id).ToList();
            var cars = await db.Cars.AsNoTracking().Include(c => c.Zone).Include(c => c.KeySlot)
                .Where(c => c.KeySlotId != null && ids.Contains(c.KeySlotId.Value))
                .ToDictionaryAsync(c => c.KeySlotId!.Value);
            return slots.Select(s => new KeyCellDto(s.Number, s.Status, cars.TryGetValue(s.Id, out var c) ? Map.Car(c) : null)).ToList();
        });

        api.MapGet("/operators", (AppDb db) => db.Operators.AsNoTracking().Where(o => o.IsActive).OrderBy(o => o.Id)
            .Select(o => new OperatorDto(o.Id, o.DisplayName)).ToListAsync());
        api.MapGet("/settings", async (AppDb db) => Map.Settings(await db.Settings.AsNoTracking().FirstAsync()));
        api.MapPut("/settings", async (SettingsDto d, AppDb db) =>
        {
            var s = await db.Settings.FirstAsync();
            s.RequirePhoto = d.RequirePhoto;
            s.CellLabelMode = d.CellLabelMode;
            s.ExternalFeedEnabled = d.ExternalFeedEnabled;
            await db.SaveChangesAsync();
            return Map.Settings(s);
        });

        api.MapGet("/photos/{id:int}", async (int id, AppDb db, IPhotoStorage photos, HttpContext ctx) =>
        {
            var p = await db.Photos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            var stream = p is null ? null : photos.OpenRead(p.StorageKey);
            if (stream is null) return Results.NotFound();
            ctx.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.Stream(stream, p!.ContentType);
        });

        api.MapPost("/demo/reset", (AppDb db, IConfiguration cfg, TimeProvider clock, IPhotoStorage photos) =>
        {
            if (!DbBootstrap.IsDemo(cfg)) return Results.NotFound();
            DbBootstrap.Reset(db, clock, photos);
            return Results.NoContent();
        });
    }
}
