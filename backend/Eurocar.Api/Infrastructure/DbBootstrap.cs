using Eurocar.Api.Domain;
using Eurocar.Api.Seed;

namespace Eurocar.Api.Infrastructure;

public static class DbBootstrap
{
    public static bool IsDemo(IConfiguration cfg) => string.Equals(cfg["Seed:Mode"], "Demo", StringComparison.OrdinalIgnoreCase);

    public static void Init(AppDb db, IConfiguration cfg, TimeProvider clock)
    {
        db.Database.EnsureCreated();
        if (IsDemo(cfg) && !db.Zones.Any())
            DemoSeeder.Seed(db, clock.GetUtcNow().UtcDateTime);
        if (!db.Settings.Any())
        {
            db.Settings.Add(new AppSettings());
            db.SaveChanges();
        }
    }

    public static void Reset(AppDb db, TimeProvider clock, IPhotoStorage photos)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        photos.Clear();
        DemoSeeder.Seed(db, clock.GetUtcNow().UtcDateTime);
    }
}
