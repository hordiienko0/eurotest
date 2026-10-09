using Eurocar.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Infrastructure;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<ZoneGroup> ZoneGroups => Set<ZoneGroup>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<KeySlot> KeySlots => Set<KeySlot>();
    public DbSet<Move> Moves => Set<Move>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ZoneGroup>().HasKey(x => x.Code);

        b.Entity<Zone>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Zone>().Property(x => x.Kind).HasConversion<string>();
        b.Entity<Zone>().Property(x => x.Priority).HasConversion<string>();

        b.Entity<Car>().HasIndex(x => x.Vin).IsUnique();
        // One car per spot, one car per key slot.
        b.Entity<Car>().HasIndex(x => new { x.ZoneId, x.SpotCode }).IsUnique().HasFilter("\"SpotCode\" IS NOT NULL");
        b.Entity<Car>().HasIndex(x => x.KeySlotId).IsUnique().HasFilter("\"KeySlotId\" IS NOT NULL");
        b.Entity<Car>().Property(x => x.Status).HasConversion<string>();

        b.Entity<KeySlot>().HasIndex(x => new { x.Cabinet, x.Number }).IsUnique();
        b.Entity<KeySlot>().Property(x => x.Status).HasConversion<string>();

        b.Entity<Move>().Property(x => x.State).HasConversion<string>();
        // One active move per car; one active reservation per spot.
        b.Entity<Move>().HasIndex(x => x.CarId).IsUnique().HasFilter("\"State\" = 'InTransit'");
        b.Entity<Move>().HasIndex(x => new { x.ToZoneId, x.ToSpot }).IsUnique().HasFilter("\"State\" = 'InTransit' AND \"ToSpot\" IS NOT NULL");
        b.Entity<Move>().HasOne(x => x.FromZone).WithMany().HasForeignKey(x => x.FromZoneId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Move>().HasOne(x => x.ToZone).WithMany().HasForeignKey(x => x.ToZoneId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<AppSettings>().Property(x => x.CellLabelMode).HasConversion<string>();
    }
}
