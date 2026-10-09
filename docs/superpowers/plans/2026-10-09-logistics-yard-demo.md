# Євро Лоджистікс — Yard Logistics Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a working demo of the "Логістика авто · Соломоново" prototype: an Angular client that looks 1:1 like the design and a small C# API with deterministic mock data.

**Architecture:** One ASP.NET Core Minimal API project (feature folders, EF Core + SQLite file, `EnsureCreated`, deterministic demo seed, background simulator for external warehouses) owns all business rules: moves, spot/key uniqueness, zone edits. The Angular 20+ SPA (standalone components, signals, zoneless) ports the prototype's templates and `renderVals()` logic into components and signal stores, and polls the API every 10 s.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core Sqlite, xUnit · Angular 20+ (CLI latest), TypeScript, plain CSS (Modernist design-system file), Vitest/Karma (whatever the CLI ships).

**Spec:** `docs/superpowers/specs/2026-10-09-logistics-yard-design.md`. Section 0, «Обсяг ДЕМО», overrides the rest of the spec. The prototype source is `docs/design/Логистика.dc.html`. Its markup (lines 19–504) and logic (lines 507–908) are the reference for every UI task.

## Global Constraints

- Backend targets `net10.0`. If `dotnet --version` prints 9.x and installing the .NET 10 SDK is not possible, use `net9.0` and EF Core 9; nothing in this plan needs .NET 10-only APIs.
- The database is SQLite at `<content root>/data/eurocar.db`, created with `EnsureCreated()` (no migrations). Demo reset deletes and recreates it.
- The API listens on `http://localhost:5080` (`"Urls"` in `appsettings.json`; delete `Properties/launchSettings.json`). Run it from `backend/Eurocar.Api`.
- All routes are under `/api/v1`. JSON is camelCase, enums are serialized as strings (`JsonStringEnumConverter`), and all timestamps are UTC ISO strings with `Z`.
- Errors are ProblemDetails `{ status, title, code }`. `title` is a Ukrainian message shown verbatim in the UI.
- The current operator comes from the `X-Operator-Id` header via `ICurrentOperator`. There is no login.
- Spot codes are `A1…T15`, with row letters `ABCDEFGHIJKLMNOPQRST`. Capacity is `floor(area / 25)` capped at 300.
- Key label format is `Шафа 03 · комірка 41` (two-digit pad). External handover label: `передано з авто`.
- Angular uses standalone components, `signal`/`computed`/`effect`, `input()`/`output()`, built-in control flow (`@if`, `@for`), and zoneless change detection. No NgRx and no UI component libraries.
- Visuals: `frontend/src/styles/modernist.css` (copy of `docs/design/modernist-styles.css`) plus the palette overrides from prototype lines 14–17. Font is Archivo 400–800. All UI text is Ukrainian and copied exactly from the prototype.
- Breakpoints: mobile below 760 px (bottom nav plus action bar), clock shown from 1100 px.
- Angular dev server runs on 4200 with `proxy.conf.json` forwarding `/api` to `http://localhost:5080`.
- Every commit message ends with the trailer line `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. The commit commands below pass it as a second `-m`.

## Review Focus

1. **Spot typed with lowercase or spaces** (`" d 7 "`) must be accepted as `D7`. Pinned by `ZoneLayoutTests.NormalizeSpot_*` (Task 1) and `MoveServiceTests.SetTarget_normalizes_spot_input` (Task 4).
2. **Two operators target the same free spot.** The second must get `SpotOccupied` («Місце B1 зайняте.»), not a silent double-booking. Pinned by `MoveServiceTests.Second_operator_cannot_reserve_same_spot` (Task 4), backed by a partial unique index (Task 2).
3. **Origin spot taken while the car is in transit, then the operator cancels.** Must return `OriginTaken` and accept a fallback spot. Pinned by `MoveServiceTests.Cancel_when_origin_taken_requires_fallback` (Task 4).
4. **Shrinking a temporary zone that has cars on the removed spots.** Cars must be moved to valid free spots and none lost. Pinned by `ZoneServiceTests.Shrinking_zone_reassigns_cars_on_removed_spots` (Task 3).
5. **The external-stock simulator runs for a long time.** It must never delete a car that has journal history and never exceed a zone's capacity. Pinned by `SimulatorTests.Simulator_keeps_history_cars_and_capacity` (Task 5).
6. **Page reload mid-move** (frontend, no automated test). The active move, timer, target and photo must come back from `GET /moves/active`. Manual check in Task 10, Step 6.

## File Map

```
backend/
  Eurocar.slnx (or .sln)               solution
  Eurocar.Api/
    Program.cs                         composition root
    appsettings.json                   Urls, Seed, Photos, ExternalFeed
    Domain/ZoneLayout.cs               spot ids, layout, capacity, spot normalization
    Domain/Enums.cs                    ZoneKind, Priority, CarStatus, KeySlotStatus, MoveState, CellLabelMode
    Domain/Entities.cs                 ZoneGroup, Zone, Car, KeySlot, Move, Photo, Operator, AppSettings
    Domain/DomainException.cs          DomainException + Errors factory (Ukrainian messages)
    Domain/CarCatalog.cs               models, colors, VIN generator
    Infrastructure/AppDb.cs            DbContext + unique/partial indexes
    Infrastructure/DbBootstrap.cs      EnsureCreated, demo seed, reset
    Infrastructure/DomainExceptionHandler.cs   DomainException → ProblemDetails
    Infrastructure/PhotoStorage.cs     IPhotoStorage + LocalDiskPhotoStorage
    Infrastructure/CurrentOperator.cs  ICurrentOperator (X-Operator-Id)
    Seed/DemoSeeder.cs                 deterministic prototype world
    Features/Dtos.cs                   API records
    Features/Map.cs                    entity → DTO
    Features/Zones/ZoneService.cs      overview, detail, create/update/delete
    Features/Zones/ZonesEndpoints.cs
    Features/Moves/MoveService.cs      start, target, photo, complete, cancel, journal
    Features/Moves/MovesEndpoints.cs
    Features/Cars/CarQueries.cs        VIN search
    Features/MiscEndpoints.cs          cars, keys, operators, settings, photos, demo reset
    Integration/ExternalStock.cs       IExternalStockProvider, SimulatedExternalStockProvider, ExternalStockPoller
  Eurocar.Tests/
    TestDb.cs, ZoneLayoutTests.cs, DemoSeederTests.cs, ZoneServiceTests.cs, MoveServiceTests.cs, SimulatorTests.cs
frontend/
  proxy.conf.json
  src/index.html, src/styles.css, src/styles/modernist.css
  src/app/app.ts|html|css              shell: header, tabs, VIN search, clock, offline banner, toast, lightbox, mobile bar + nav
  src/app/app.config.ts, app.routes.ts
  src/app/core/  models.ts api.ts errors.ts notify.ts connection.ts operator.ts settings.ts clock.ts viewport.ts
                 search.ts lightbox.ts mobile-bar.ts poll.ts move-store.ts
  src/app/shared/ format.ts image.ts zone-layout.ts zone-layout.spec.ts
  src/app/features/zones/  zones-page.ts|html, zone-dialog.ts|html
  src/app/features/move/   move-page.ts|html, view-models.ts, zone-map-aerial.ts|html, zone-map-scheme.ts, move-panel.ts|html
  src/app/features/keys/   keys-page.ts|html
  src/app/features/log/    log-page.ts|html
README.md
```

---

### Task 1: Backend scaffold + `ZoneLayout`

**Files:**
- Create: `.gitignore`, `backend/Eurocar.Api/*` (template), `backend/Eurocar.Tests/*` (template)
- Create: `backend/Eurocar.Api/Domain/ZoneLayout.cs`
- Test: `backend/Eurocar.Tests/ZoneLayoutTests.cs`

**Interfaces:**
- Produces: `static class ZoneLayout { const string Letters; const int M2PerCar = 25; const int MaxCapacity = 300; int CapacityFromArea(int areaM2); (int Rows, int PerRow) LayoutFor(int capacity); IReadOnlyList<string> SpotIds(int capacity, int rows, int perRow); string? NormalizeSpot(string? input); }` in namespace `Eurocar.Api.Domain`.

- [ ] **Step 1: Scaffold the solution**

Run from `D:\ЄврокарМВП` (bash):
```bash
dotnet --version
dotnet new gitignore
printf '\n# app\ndata/\nnode_modules/\n.angular/\ndist/\n' >> .gitignore
mkdir -p backend && cd backend
dotnet new sln -n Eurocar
dotnet new web -n Eurocar.Api -o Eurocar.Api
dotnet new xunit -n Eurocar.Tests -o Eurocar.Tests
dotnet sln add Eurocar.Api/Eurocar.Api.csproj Eurocar.Tests/Eurocar.Tests.csproj
dotnet add Eurocar.Api package Microsoft.EntityFrameworkCore.Sqlite
dotnet add Eurocar.Tests reference Eurocar.Api/Eurocar.Api.csproj
dotnet add Eurocar.Tests package Microsoft.EntityFrameworkCore.Sqlite
rm Eurocar.Tests/UnitTest1.cs Eurocar.Api/Properties/launchSettings.json
cd ..
```
Expected: `dotnet --version` prints `10.x` (see Global Constraints for 9.x), and every command succeeds.

- [ ] **Step 2: Write the failing test**

`backend/Eurocar.Tests/ZoneLayoutTests.cs`:
```csharp
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
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test backend`
Expected: build error `The name 'ZoneLayout' does not exist in the current context`.

- [ ] **Step 4: Write minimal implementation**

`backend/Eurocar.Api/Domain/ZoneLayout.cs`:
```csharp
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
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test backend`
Expected: `Passed! - Failed: 0, Passed: 12`.

- [ ] **Step 6: Commit**

```bash
git add .gitignore backend
git commit -m "feat(api): scaffold backend and zone layout rules" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Domain model, database and demo seed

**Files:**
- Create: `backend/Eurocar.Api/Domain/Enums.cs`, `Domain/Entities.cs`, `Domain/DomainException.cs`, `Domain/CarCatalog.cs`
- Create: `backend/Eurocar.Api/Infrastructure/AppDb.cs`, `Infrastructure/DbBootstrap.cs`
- Create: `backend/Eurocar.Api/Seed/DemoSeeder.cs`
- Modify: `backend/Eurocar.Api/Program.cs` (replace), `backend/Eurocar.Api/appsettings.json` (replace)
- Test: `backend/Eurocar.Tests/TestDb.cs`, `backend/Eurocar.Tests/DemoSeederTests.cs`

**Interfaces:**
- Consumes: `ZoneLayout` (Task 1).
- Produces:
  - Enums `ZoneKind {Mapped, External}`, `Priority {High, Normal, Low}`, `CarStatus {Parked, InTransit, AtExternal}`, `KeySlotStatus {Empty, InBox, Issued}`, `MoveState {InTransit, Completed, Cancelled}`, `CellLabelMode {Vin, Model}`.
  - Entities with the exact properties shown below. `KeySlot.Label` is a get-only property (`Шафа 01 · комірка 03`).
  - `DomainException(string code, string message, int status = 400)` with `Code` and `Status`, plus the `Errors` static factory.
  - `CarCatalog.Models`, `CarCatalog.Colors`, `CarCatalog.MakeVin(Random)`.
  - `AppDb` with `DbSet`s `ZoneGroups, Zones, Cars, KeySlots, Moves, Photos, Operators, Settings`.
  - `DemoSeeder.Seed(AppDb db, DateTime nowUtc)`.
  - `DbBootstrap.IsDemo(IConfiguration)`, `DbBootstrap.Init(AppDb, IConfiguration, TimeProvider)`.
  - Test helper `TestDb.Create(bool seedDemo = false)` with `.Db` and `.NewContext()`.

- [ ] **Step 1: Write enums, entities, errors and catalog**

`backend/Eurocar.Api/Domain/Enums.cs`:
```csharp
namespace Eurocar.Api.Domain;

public enum ZoneKind { Mapped, External }
public enum Priority { High, Normal, Low }
public enum CarStatus { Parked, InTransit, AtExternal }
public enum KeySlotStatus { Empty, InBox, Issued }
public enum MoveState { InTransit, Completed, Cancelled }
public enum CellLabelMode { Vin, Model }
```

`backend/Eurocar.Api/Domain/Entities.cs`:
```csharp
namespace Eurocar.Api.Domain;

public class ZoneGroup
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsEditable { get; set; }
}

public class Zone
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string GroupCode { get; set; } = "";
    public string Name { get; set; } = "";
    public ZoneKind Kind { get; set; }
    public int Capacity { get; set; }
    public int? Rows { get; set; }
    public int? PerRow { get; set; }
    public int? AreaM2 { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    public string StripLabel { get; set; } = "В’ЇЗД";
    public bool IsDeleted { get; set; }
    public bool IsOnline { get; set; } = true;
    public DateTime? LastSyncedAt { get; set; }
    public string? ExternalRef { get; set; }
}

public class Car
{
    public int Id { get; set; }
    public string Vin { get; set; } = "";
    public string Model { get; set; } = "";
    public string Color { get; set; } = "";
    public CarStatus Status { get; set; }
    public int? ZoneId { get; set; }
    public Zone? Zone { get; set; }
    public string? SpotCode { get; set; }
    public int? KeySlotId { get; set; }
    public KeySlot? KeySlot { get; set; }
    public string? ExternalRef { get; set; }
}

public class KeySlot
{
    public int Id { get; set; }
    public int Cabinet { get; set; }
    public int Number { get; set; }
    public KeySlotStatus Status { get; set; }
    public string Label => $"Шафа {Cabinet:00} · комірка {Number:00}";
}

public class Move
{
    public int Id { get; set; }
    public int? No { get; set; }
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;
    public int FromZoneId { get; set; }
    public Zone FromZone { get; set; } = null!;
    public string? FromSpot { get; set; }
    public int? ToZoneId { get; set; }
    public Zone? ToZone { get; set; }
    public string? ToSpot { get; set; }
    public MoveState State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? PhotoId { get; set; }
    public Photo? Photo { get; set; }
    public string? KeyLabel { get; set; }
    public string? ExternalRef { get; set; }
}

public class Photo
{
    public int Id { get; set; }
    public string StorageKey { get; set; } = "";
    public string ContentType { get; set; } = "image/jpeg";
    public DateTime TakenAt { get; set; }
    public long SizeBytes { get; set; }
}

public class Operator
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string? ExternalId { get; set; }
}

public class AppSettings
{
    public int Id { get; set; }
    public bool RequirePhoto { get; set; } = true;
    public CellLabelMode CellLabelMode { get; set; } = CellLabelMode.Vin;
    public bool ExternalFeedEnabled { get; set; } = true;
}
```

`backend/Eurocar.Api/Domain/DomainException.cs`:
```csharp
namespace Eurocar.Api.Domain;

public sealed class DomainException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

/// <summary>All business errors. Messages are shown to operators verbatim.</summary>
public static class Errors
{
    public static DomainException NotFound(string what) => new("NotFound", $"{what} не знайдено.", 404);
    public static DomainException Validation(string message) => new("Validation", message);
    public static DomainException OperatorRequired() => new("OperatorRequired", "Оберіть оператора.");
    public static DomainException CarBusy() => new("CarBusy", "Авто вже в дорозі.", 409);
    public static DomainException NotYourMove() => new("NotYourMove", "Цю операцію почав інший оператор.", 403);
    public static DomainException SpotRequired() => new("SpotRequired", "Вкажіть номер місця.");
    public static DomainException SpotNotInZone(string zone, string spot) => new("SpotNotInZone", $"У зоні {zone} немає місця {spot}.");
    public static DomainException SpotIsOrigin() => new("SpotIsOrigin", "Це початкове місце авто.");
    public static DomainException SpotOccupied(string spot) => new("SpotOccupied", $"Місце {spot} зайняте.", 409);
    public static DomainException OriginTaken(string spot) => new("OriginTaken", $"Початкове місце {spot} вже зайняте. Оберіть інше місце для повернення.", 409);
    public static DomainException ZoneOffline() => new("ZoneOffline", "Склад зараз не на зв’язку.", 409);
    public static DomainException TargetRequired() => new("TargetRequired", "Оберіть зону й місце призначення.");
    public static DomainException PhotoRequired() => new("PhotoRequired", "Сфотографуйте авто на новому місці.");
    public static DomainException PhotoInvalid() => new("PhotoInvalid", "Потрібне зображення до 10 МБ.");
    public static DomainException KeyBoxFull() => new("KeyBoxFull", "У ключниці немає вільних комірок.", 409);
    public static DomainException ZoneNotEditable() => new("ZoneNotEditable", "Цю зону не можна змінювати.", 403);
    public static DomainException AreaTooSmall() => new("AreaTooSmall", "Площа має бути не меншою за 25 м².");
    public static DomainException CapacityBelowOccupancy(int used) => new("CapacityBelowOccupancy", $"На майданчику {used} авто, місткість не може бути меншою.");
    public static DomainException ZoneNotEmpty() => new("ZoneNotEmpty", "Видалити можна лише порожню зону.", 409);
    public static DomainException Conflict() => new("Conflict", "Хтось щойно змінив це. Дані оновлено.", 409);
}
```

`backend/Eurocar.Api/Domain/CarCatalog.cs`:
```csharp
using System.Text;

namespace Eurocar.Api.Domain;

public static class CarCatalog
{
    public static readonly string[] Models = ["Fabia", "Scala", "Octavia", "Kamiq", "Karoq", "Kodiaq", "Superb", "Enyaq", "Elroq"];
    public static readonly string[] Colors = ["білий", "чорний", "сірий", "сріблястий", "синій", "червоний"];
    const string VinChars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";

    /// <summary>Škoda-like VIN: "TMB" + 8 letters/digits + 6 digits (as in the prototype).</summary>
    public static string MakeVin(Random r)
    {
        var sb = new StringBuilder("TMB", 17);
        for (var i = 0; i < 8; i++) sb.Append(VinChars[r.Next(VinChars.Length)]);
        for (var i = 0; i < 6; i++) sb.Append((char)('0' + r.Next(10)));
        return sb.ToString();
    }
}
```

- [ ] **Step 2: Write the DbContext**

`backend/Eurocar.Api/Infrastructure/AppDb.cs`:
```csharp
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
```

- [ ] **Step 3: Write the failing seeder test and test helper**

`backend/Eurocar.Tests/TestDb.cs`:
```csharp
using Eurocar.Api.Infrastructure;
using Eurocar.Api.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Tests;

/// <summary>In-memory SQLite database that lives as long as this object (real constraints, no files).</summary>
public sealed class TestDb : IDisposable
{
    readonly SqliteConnection _connection;
    public AppDb Db { get; }

    TestDb(SqliteConnection connection, AppDb db) { _connection = connection; Db = db; }

    public static TestDb Create(bool seedDemo = false)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        if (seedDemo) DemoSeeder.Seed(db, DateTime.UtcNow);
        return new TestDb(connection, db);
    }

    /// <summary>A second context on the same database, for asserting what was actually saved.</summary>
    public AppDb NewContext() => new(new DbContextOptionsBuilder<AppDb>().UseSqlite(_connection).Options);

    public void Dispose() { Db.Dispose(); _connection.Dispose(); }
}
```

`backend/Eurocar.Tests/DemoSeederTests.cs`:
```csharp
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
```

- [ ] **Step 4: Run test to verify it fails**

Run: `dotnet test backend`
Expected: build error `The type or namespace name 'Seed' does not exist in the namespace 'Eurocar.Api'`.

- [ ] **Step 5: Write the seeder and bootstrap**

`backend/Eurocar.Api/Seed/DemoSeeder.cs`:
```csharp
using Eurocar.Api.Domain;
using Eurocar.Api.Infrastructure;

namespace Eurocar.Api.Seed;

/// <summary>Deterministic copy of the prototype's seed() (docs/design/Логистика.dc.html, lines 543–578).</summary>
public static class DemoSeeder
{
    static readonly (string Code, string Name, bool Editable)[] Groups =
    [
        ("3.1", "Контрольовані склади ТОВ «Євро Лоджистікс» (Соломоново)", false),
        ("3.2", "Тимчасові майданчики зберігання", true),
        ("3.3", "Транзитні зони", true),
        ("3.4", "Зовнішні партнерські склади", false),
        ("3.5", "Дилерські майданчики (External Yard)", false),
        ("3.6", "Rail yard / зона вагонного приймання", false),
        ("3.7", "Service / Repair zone", false),
        ("3.8", "Quarantine / Hold area", false),
    ];

    static readonly string[] PartnerCities =
        ["Київ", "Бровари", "Бориспіль", "Житомир", "Вінниця", "Львів", "Ужгород", "Мукачево", "Одеса", "Дніпро", "Харків", "Полтава", "Черкаси", "Рівне"];

    static readonly string[] DealerCities =
        ["Київ · Оболонь", "Київ · Теремки", "Київ · Лівий берег", "Львів", "Одеса", "Дніпро", "Харків", "Запоріжжя", "Вінниця", "Івано-Франківськ", "Тернопіль", "Луцьк", "Хмельницький", "Чернівці", "Миколаїв", "Ужгород"];

    public static void Seed(AppDb db, DateTime nowUtc)
    {
        var r = new Random(11);

        db.ZoneGroups.AddRange(Groups.Select(g => new ZoneGroup { Code = g.Code, Name = g.Name, IsEditable = g.Editable }));
        var ivanenko = new Operator { DisplayName = "Іваненко А." };
        var petrenko = new Operator { DisplayName = "Петренко С." };
        db.Operators.AddRange(ivanenko, petrenko, new Operator { DisplayName = "Коваль О." });
        db.Settings.Add(new AppSettings());

        var slots = new List<KeySlot>();
        for (var c = 1; c <= 12; c++)
            for (var n = 1; n <= 100; n++)
                slots.Add(new KeySlot { Cabinet = c, Number = n, Status = KeySlotStatus.Empty });
        db.KeySlots.AddRange(slots);

        var own = new List<(Zone Zone, double Fill)>
        {
            (Mapped("S1", "3.1", "Соломоново · зона S1", 300, 20, 15, Priority.High, "В’ЇЗД · КПП"), 0.82),
            (Mapped("S2", "3.1", "Соломоново · зона S2", 300, 20, 15, Priority.Normal, "В’ЇЗД · КПП"), 0.74),
            (Mapped("S3", "3.1", "Соломоново · зона S3", 300, 20, 15, Priority.Normal, "В’ЇЗД · КПП"), 0.61),
            (Mapped("T1", "3.2", "Майданчик-1", 100, 10, 10, Priority.Normal, "В’ЇЗД", 2500), 0.8),
            (MappedAuto("T2", "3.2", "Поле біля траси М-06", 60, Priority.Low, "В’ЇЗД", 1500), 0.5),
            (MappedAuto("X1", "3.3", "Транзит · ворота 2", 40, Priority.High, "ВОРОТА 2", 1000), 0.4),
            (Mapped("R1", "3.6", "Рампа вивантаження вагонів", 120, 8, 15, Priority.High, "ЗАЛІЗНИЦЯ · ВИВАНТАЖЕННЯ"), 0.55),
            (Mapped("SV", "3.7", "Сервіс і ремонт", 40, 4, 10, Priority.Normal, "БОКСИ СТО"), 0.6),
            (Mapped("QH", "3.8", "Карантин / утримання", 30, 2, 15, Priority.Low, "ОГОРОЖА"), 0.4),
        };

        var ext = new List<(Zone Zone, double Fill)>();
        for (var i = 0; i < PartnerCities.Length; i++)
            ext.Add((External($"P{i + 1:00}", "3.4", "Склад партнера · " + PartnerCities[i], 40 + r.Next(11) * 10, Priority.Normal, nowUtc.AddSeconds(-r.Next(240))), 0.35 + r.NextDouble() * 0.55));
        for (var i = 0; i < DealerCities.Length; i++)
            ext.Add((External($"D{i + 1:00}", "3.5", "Дилер · " + DealerCities[i], 20 + r.Next(7) * 10, i < 3 ? Priority.High : Priority.Normal, nowUtc.AddSeconds(-r.Next(240))), 0.35 + r.NextDouble() * 0.55));
        var p07 = ext.Single(x => x.Zone.Code == "P07").Zone;
        p07.IsOnline = false;
        p07.LastSyncedAt = nowUtc.AddMinutes(-47);

        var allZones = own.Select(x => x.Zone).Concat(ext.Select(x => x.Zone)).ToList();
        db.Zones.AddRange(allZones);

        var slotOrder = slots.OrderBy(_ => r.Next()).ToList();
        var nextSlot = 0;
        var vins = new HashSet<string>();
        var cars = new List<Car>();
        Car NewCar(Zone zone, CarStatus status)
        {
            string vin;
            do vin = CarCatalog.MakeVin(r); while (!vins.Add(vin));
            return new Car
            {
                Vin = vin,
                Model = CarCatalog.Models[r.Next(CarCatalog.Models.Length)],
                Color = CarCatalog.Colors[r.Next(CarCatalog.Colors.Length)],
                Zone = zone,
                Status = status,
            };
        }

        foreach (var (zone, fill) in own)
            foreach (var spot in ZoneLayout.SpotIds(zone.Capacity, zone.Rows!.Value, zone.PerRow!.Value))
            {
                if (r.NextDouble() >= fill) continue;
                var slot = slotOrder[nextSlot++];
                slot.Status = KeySlotStatus.InBox;
                var car = NewCar(zone, CarStatus.Parked);
                car.SpotCode = spot;
                car.KeySlot = slot;
                cars.Add(car);
            }
        foreach (var (zone, fill) in ext)
            for (var i = 0; i < (int)Math.Round(zone.Capacity * fill); i++)
                cars.Add(NewCar(zone, CarStatus.AtExternal));
        db.Cars.AddRange(cars);

        Zone Z(string code) => allZones.Single(z => z.Code == code);
        Move Done(int no, Car car, string fromZone, string? fromSpot, int startedSecondsAgo, int durationSeconds, Operator op, string keyLabel) => new()
        {
            No = no, Car = car, Operator = op, FromZone = Z(fromZone), FromSpot = fromSpot,
            ToZone = car.Zone, ToSpot = car.SpotCode, State = MoveState.Completed,
            StartedAt = nowUtc.AddSeconds(-startedSecondsAgo),
            CompletedAt = nowUtc.AddSeconds(-startedSecondsAgo + durationSeconds),
            KeyLabel = keyLabel,
        };
        var inService = cars.First(c => c.Zone == Z("SV"));
        var atDealer = cars.First(c => c.Zone == Z("D01"));
        var inS1 = cars.First(c => c.Zone == Z("S1"));
        db.Moves.AddRange(
            Done(1, inService, "R1", "A2", 9800, 301, ivanenko, inService.KeySlot!.Label),
            Done(2, atDealer, "S2", "K3", 7200, 158, ivanenko, "передано з авто"),
            Done(3, inS1, "R1", "B6", 3600, 414, petrenko, inS1.KeySlot!.Label));

        db.SaveChanges();
    }

    static Zone Mapped(string code, string group, string name, int capacity, int rows, int perRow, Priority priority, string strip, int? area = null) => new()
    {
        Code = code, GroupCode = group, Name = name, Kind = ZoneKind.Mapped, Capacity = capacity,
        Rows = rows, PerRow = perRow, AreaM2 = area, Priority = priority, StripLabel = strip,
    };

    static Zone MappedAuto(string code, string group, string name, int capacity, Priority priority, string strip, int area)
    {
        var (rows, perRow) = ZoneLayout.LayoutFor(capacity);
        return Mapped(code, group, name, capacity, rows, perRow, priority, strip, area);
    }

    static Zone External(string code, string group, string name, int capacity, Priority priority, DateTime syncedAt) => new()
    {
        Code = code, GroupCode = group, Name = name, Kind = ZoneKind.External, Capacity = capacity,
        Priority = priority, StripLabel = "", IsOnline = true, LastSyncedAt = syncedAt,
    };
}
```

`backend/Eurocar.Api/Infrastructure/DbBootstrap.cs`:
```csharp
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
}
```

- [ ] **Step 6: Replace Program.cs and appsettings.json**

`backend/Eurocar.Api/Program.cs`:
```csharp
using System.Text.Json.Serialization;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("Db") ?? $"Data Source={Path.Combine(dataDir, "eurocar.db")}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    DbBootstrap.Init(scope.ServiceProvider.GetRequiredService<AppDb>(), app.Configuration, TimeProvider.System);

app.Run();

public partial class Program;
```

`backend/Eurocar.Api/appsettings.json`:
```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning", "Microsoft.EntityFrameworkCore": "Warning" } },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5080",
  "Seed": { "Mode": "Demo" },
  "Photos": { "Path": "data/photos" },
  "ExternalFeed": { "Enabled": true, "IntervalSeconds": 9 }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test backend`
Expected: `Passed! - Failed: 0, Passed: 13`.

- [ ] **Step 8: Commit**

```bash
git add backend
git commit -m "feat(api): domain model, SQLite context and deterministic demo seed" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Zones API (overview, detail, create/edit/delete)

**Files:**
- Create: `backend/Eurocar.Api/Features/Dtos.cs`, `Features/Map.cs`
- Create: `backend/Eurocar.Api/Features/Zones/ZoneService.cs`, `Features/Zones/ZonesEndpoints.cs`
- Create: `backend/Eurocar.Api/Infrastructure/DomainExceptionHandler.cs`
- Modify: `backend/Eurocar.Api/Program.cs`
- Test: `backend/Eurocar.Tests/ZoneServiceTests.cs`

**Interfaces:**
- Consumes: entities, `Errors`, `ZoneLayout`, `AppDb` (Tasks 1–2).
- Produces:
  - All DTO records in `Eurocar.Api.Features`, exactly as written in Step 1. Later tasks and the Angular `models.ts` mirror them.
  - `Map.Car(Car)`, `Map.ZoneRow(Zone, int used, bool editable)`, `Map.Move(Move)`, `Map.Settings(AppSettings)`.
  - `ZoneService`: `OverviewAsync()`, `DetailAsync(string code)`, `CreateAsync(ZoneUpsertDto)`, `UpdateAsync(string code, ZoneUpsertDto)`, `DeleteAsync(string code)`.
  - `RouteGroupBuilder.MapZones()`.
  - HTTP: `GET /api/v1/zones`, `GET /api/v1/zones/{code}`, `POST /api/v1/zones`, `PUT /api/v1/zones/{code}`, `DELETE /api/v1/zones/{code}`.

- [ ] **Step 1: Write DTOs and mapping**

`backend/Eurocar.Api/Features/Dtos.cs`:
```csharp
using Eurocar.Api.Domain;

namespace Eurocar.Api.Features;

public record CarDto(int Id, string Vin, string Model, string Color, CarStatus Status, string? ZoneCode, string? ZoneName,
    string? SpotCode, int? KeyCabinet, int? KeyNumber, KeySlotStatus? KeyStatus);

public record ZoneRowDto(string Code, string Name, string GroupCode, ZoneKind Kind, int Capacity, int Used, int? AreaM2,
    Priority Priority, bool IsOnline, DateTime? LastSyncedAt, bool Editable);
public record ZoneGroupDto(string Code, string Name, bool Editable, IReadOnlyList<ZoneRowDto> Zones);
public record TotalsDto(int OwnUsed, int OwnCap, int ExtUsed, int ExtCap, int Online, int ExtCount, int Ops);
public record ZonesOverviewDto(TotalsDto Totals, IReadOnlyList<ZoneGroupDto> Groups);
public record SpotDto(string Code, CarDto? Car, bool Reserved);
public record ZoneDetailDto(string Code, string Name, string GroupCode, string GroupName, ZoneKind Kind, int Capacity, int Used,
    int? Rows, int? PerRow, int? AreaM2, Priority Priority, string StripLabel, bool IsOnline, DateTime? LastSyncedAt,
    IReadOnlyList<SpotDto> Spots, IReadOnlyList<CarDto> Cars);
public record ZoneUpsertDto(string GroupCode, string Name, int AreaM2, Priority Priority);

public record PlaceDto(string ZoneCode, string ZoneName, string? SpotCode);
public record MoveDto(int Id, int? No, MoveState State, CarDto Car, int OperatorId, string OperatorName, PlaceDto From, PlaceDto? To,
    DateTime StartedAt, DateTime? CompletedAt, string? PhotoUrl, DateTime? PhotoTakenAt, string? KeyLabel);
public record StartMoveDto(int CarId);
public record SetTargetDto(string? ZoneCode, string? SpotCode);
public record CancelMoveDto(string? FallbackSpot);

public record CabinetCountDto(int Cabinet, int Count);
public record KeysSummaryDto(int InBox, int Issued, int Empty, IReadOnlyList<CabinetCountDto> Cabinets);
public record KeyCellDto(int Number, KeySlotStatus Status, CarDto? Car);

public record OperatorDto(int Id, string DisplayName);
public record SettingsDto(bool RequirePhoto, CellLabelMode CellLabelMode, bool ExternalFeedEnabled);
```

`backend/Eurocar.Api/Features/Map.cs`:
```csharp
using Eurocar.Api.Domain;

namespace Eurocar.Api.Features;

public static class Map
{
    /// <summary>Requires Car.Zone and Car.KeySlot to be loaded.</summary>
    public static CarDto Car(Car c) => new(c.Id, c.Vin, c.Model, c.Color, c.Status, c.Zone?.Code, c.Zone?.Name, c.SpotCode,
        c.KeySlot?.Cabinet, c.KeySlot?.Number, c.KeySlot?.Status);

    public static ZoneRowDto ZoneRow(Zone z, int used, bool editable) => new(z.Code, z.Name, z.GroupCode, z.Kind, z.Capacity, used,
        z.AreaM2, z.Priority, z.IsOnline, Utc(z.LastSyncedAt), editable);

    /// <summary>Requires Car(+Zone,+KeySlot), Operator, FromZone, ToZone and Photo to be loaded.</summary>
    public static MoveDto Move(Move m) => new(m.Id, m.No, m.State, Car(m.Car), m.OperatorId, m.Operator.DisplayName,
        new PlaceDto(m.FromZone.Code, m.FromZone.Name, m.FromSpot),
        m.ToZone is null ? null : new PlaceDto(m.ToZone.Code, m.ToZone.Name, m.ToSpot),
        Utc(m.StartedAt), Utc(m.CompletedAt), m.PhotoId is null ? null : $"/api/v1/photos/{m.PhotoId}", Utc(m.Photo?.TakenAt), m.KeyLabel);

    public static SettingsDto Settings(AppSettings s) => new(s.RequirePhoto, s.CellLabelMode, s.ExternalFeedEnabled);

    // SQLite returns DateTimeKind.Unspecified; everything we store is UTC.
    public static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);
    public static DateTime? Utc(DateTime? d) => d is null ? null : Utc(d.Value);
}
```

- [ ] **Step 2: Write the failing tests**

`backend/Eurocar.Tests/ZoneServiceTests.cs`:
```csharp
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
```
(OwnCap = 300·3 + 100 + 60 + 40 + 120 + 40 + 30 = 1290. Online = 30 external − P07 = 29.)

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test backend`
Expected: build error `The type or namespace name 'Zones' does not exist in the namespace 'Eurocar.Api.Features'`.

- [ ] **Step 4: Implement ZoneService**

`backend/Eurocar.Api/Features/Zones/ZoneService.cs`:
```csharp
using Eurocar.Api.Domain;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Features.Zones;

public class ZoneService(AppDb db)
{
    public async Task<ZonesOverviewDto> OverviewAsync()
    {
        var groups = await db.ZoneGroups.AsNoTracking().OrderBy(g => g.Code).ToListAsync();
        var zones = await db.Zones.AsNoTracking().Where(z => !z.IsDeleted).OrderBy(z => z.Id).ToListAsync();
        var used = await UsedByZoneAsync();
        var editable = groups.Where(g => g.IsEditable).Select(g => g.Code).ToHashSet();
        int Used(Zone z) => used.GetValueOrDefault(z.Id);

        var own = zones.Where(z => z.Kind == ZoneKind.Mapped).ToList();
        var ext = zones.Where(z => z.Kind == ZoneKind.External).ToList();
        var totals = new TotalsDto(own.Sum(Used), own.Sum(z => z.Capacity), ext.Sum(Used), ext.Sum(z => z.Capacity),
            ext.Count(z => z.IsOnline), ext.Count, await db.Moves.CountAsync(m => m.State == MoveState.Completed));

        return new ZonesOverviewDto(totals, groups.Select(g => new ZoneGroupDto(g.Code, g.Name, g.IsEditable,
            zones.Where(z => z.GroupCode == g.Code).Select(z => Map.ZoneRow(z, Used(z), editable.Contains(z.GroupCode))).ToList())).ToList());
    }

    public async Task<ZoneDetailDto> DetailAsync(string code)
    {
        var z = await FindAsync(code, track: false);
        var group = await db.ZoneGroups.AsNoTracking().SingleAsync(g => g.Code == z.GroupCode);
        var cars = await db.Cars.AsNoTracking().Include(c => c.Zone).Include(c => c.KeySlot)
            .Where(c => c.ZoneId == z.Id).OrderBy(c => c.Id).ToListAsync();

        var spots = new List<SpotDto>();
        if (z.Kind == ZoneKind.Mapped)
        {
            var bySpot = cars.Where(c => c.SpotCode != null).ToDictionary(c => c.SpotCode!);
            var reserved = (await db.Moves
                .Where(m => m.State == MoveState.InTransit && m.ToZoneId == z.Id && m.ToSpot != null)
                .Select(m => m.ToSpot!).ToListAsync()).ToHashSet();
            spots = ZoneLayout.SpotIds(z.Capacity, z.Rows!.Value, z.PerRow!.Value)
                .Select(s => new SpotDto(s, bySpot.TryGetValue(s, out var c) ? Map.Car(c) : null, reserved.Contains(s)))
                .ToList();
        }

        return new ZoneDetailDto(z.Code, z.Name, z.GroupCode, group.Name, z.Kind, z.Capacity, cars.Count, z.Rows, z.PerRow,
            z.AreaM2, z.Priority, z.StripLabel, z.IsOnline, Map.Utc(z.LastSyncedAt), spots,
            z.Kind == ZoneKind.External ? cars.Select(Map.Car).ToList() : []);
    }

    public async Task<ZoneRowDto> CreateAsync(ZoneUpsertDto d)
    {
        var group = await EditableGroupAsync(d.GroupCode);
        var (name, capacity) = Validate(d, used: 0);
        var prefix = group.Code == "3.2" ? "T" : "X";
        var codes = (await db.Zones.Select(z => z.Code).ToListAsync()).ToHashSet();
        var n = 1;
        while (codes.Contains(prefix + n)) n++;

        var (rows, perRow) = ZoneLayout.LayoutFor(capacity);
        var zone = new Zone
        {
            Code = prefix + n, GroupCode = group.Code, Name = name, Kind = ZoneKind.Mapped, Capacity = capacity,
            Rows = rows, PerRow = perRow, AreaM2 = d.AreaM2, Priority = d.Priority, StripLabel = "В’ЇЗД",
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync();
        return Map.ZoneRow(zone, 0, editable: true);
    }

    public async Task<ZoneRowDto> UpdateAsync(string code, ZoneUpsertDto d)
    {
        var zone = await FindAsync(code, track: true);
        await EditableGroupAsync(zone.GroupCode);
        var cars = await db.Cars.Where(c => c.ZoneId == zone.Id).OrderBy(c => c.Id).ToListAsync();
        var (name, capacity) = Validate(d, cars.Count);

        zone.Name = name;
        zone.AreaM2 = d.AreaM2;
        zone.Priority = d.Priority;
        if (capacity != zone.Capacity)
        {
            var (rows, perRow) = ZoneLayout.LayoutFor(capacity);
            zone.Capacity = capacity;
            zone.Rows = rows;
            zone.PerRow = perRow;
            var valid = ZoneLayout.SpotIds(capacity, rows, perRow);
            var validSet = valid.ToHashSet();

            // Reservations on spots that no longer exist are dropped; the operator picks again.
            var reservations = await db.Moves.Where(m => m.State == MoveState.InTransit && m.ToZoneId == zone.Id).ToListAsync();
            foreach (var m in reservations.Where(m => m.ToSpot != null && !validSet.Contains(m.ToSpot)))
            {
                m.ToZoneId = null;
                m.ToSpot = null;
            }

            var taken = cars.Where(c => c.SpotCode != null && validSet.Contains(c.SpotCode)).Select(c => c.SpotCode!)
                .Concat(reservations.Where(m => m.ToZoneId == zone.Id && m.ToSpot != null).Select(m => m.ToSpot!))
                .ToHashSet();
            var free = new Queue<string>(valid.Where(s => !taken.Contains(s)));
            foreach (var car in cars.Where(c => c.SpotCode == null || !validSet.Contains(c.SpotCode)))
                car.SpotCode = free.TryDequeue(out var spot) ? spot : throw Errors.CapacityBelowOccupancy(cars.Count);
        }

        await db.SaveChangesAsync();
        return Map.ZoneRow(zone, cars.Count, editable: true);
    }

    public async Task DeleteAsync(string code)
    {
        var zone = await FindAsync(code, track: true);
        await EditableGroupAsync(zone.GroupCode);
        var busy = await db.Cars.AnyAsync(c => c.ZoneId == zone.Id)
            || await db.Moves.AnyAsync(m => m.State == MoveState.InTransit && (m.ToZoneId == zone.Id || m.FromZoneId == zone.Id));
        if (busy) throw Errors.ZoneNotEmpty();
        zone.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    Task<Dictionary<int, int>> UsedByZoneAsync() => db.Cars
        .Where(c => c.ZoneId != null)
        .GroupBy(c => c.ZoneId!.Value)
        .Select(g => new { g.Key, N = g.Count() })
        .ToDictionaryAsync(x => x.Key, x => x.N);

    async Task<Zone> FindAsync(string code, bool track)
    {
        var query = track ? db.Zones : db.Zones.AsNoTracking();
        return await query.FirstOrDefaultAsync(z => z.Code == code && !z.IsDeleted) ?? throw Errors.NotFound($"Зону {code}");
    }

    async Task<ZoneGroup> EditableGroupAsync(string groupCode)
    {
        var g = await db.ZoneGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Code == groupCode);
        return g is { IsEditable: true } ? g : throw Errors.ZoneNotEditable();
    }

    static (string Name, int Capacity) Validate(ZoneUpsertDto d, int used)
    {
        var name = d.Name?.Trim() ?? "";
        if (name.Length == 0) throw Errors.Validation("Вкажіть назву.");
        var capacity = ZoneLayout.CapacityFromArea(d.AreaM2);
        if (capacity < 1) throw Errors.AreaTooSmall();
        if (capacity < used) throw Errors.CapacityBelowOccupancy(used);
        return (name, capacity);
    }
}
```

- [ ] **Step 5: Add endpoints, error handler and wire Program.cs**

`backend/Eurocar.Api/Features/Zones/ZonesEndpoints.cs`:
```csharp
namespace Eurocar.Api.Features.Zones;

public static class ZonesEndpoints
{
    public static void MapZones(this RouteGroupBuilder api)
    {
        api.MapGet("/zones", (ZoneService s) => s.OverviewAsync());
        api.MapGet("/zones/{code}", (string code, ZoneService s) => s.DetailAsync(code));
        api.MapPost("/zones", (ZoneUpsertDto d, ZoneService s) => s.CreateAsync(d));
        api.MapPut("/zones/{code}", (string code, ZoneUpsertDto d, ZoneService s) => s.UpdateAsync(code, d));
        api.MapDelete("/zones/{code}", async (string code, ZoneService s) =>
        {
            await s.DeleteAsync(code);
            return Results.NoContent();
        });
    }
}
```

`backend/Eurocar.Api/Infrastructure/DomainExceptionHandler.cs`:
```csharp
using System.Text.Json;
using Eurocar.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Eurocar.Api.Infrastructure;

/// <summary>Turns DomainException into ProblemDetails { status, title, code }.</summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not DomainException d) return false;
        var problem = new ProblemDetails { Status = d.Status, Title = d.Message, Extensions = { ["code"] = d.Code } };
        ctx.Response.StatusCode = d.Status;
        await ctx.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", ct);
        return true;
    }
}
```

`backend/Eurocar.Api/Program.cs` (full replacement):
```csharp
using System.Text.Json.Serialization;
using Eurocar.Api.Features.Zones;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("Db") ?? $"Data Source={Path.Combine(dataDir, "eurocar.db")}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ZoneService>();

var app = builder.Build();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
    DbBootstrap.Init(scope.ServiceProvider.GetRequiredService<AppDb>(), app.Configuration, TimeProvider.System);

var api = app.MapGroup("/api/v1");
api.MapZones();

app.Run();

public partial class Program;
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test backend`
Expected: `Passed! - Failed: 0, Passed: 22`.

- [ ] **Step 7: Smoke-test the endpoint**

Run (two terminals, bash):
```bash
cd backend/Eurocar.Api && dotnet run
curl -s http://localhost:5080/api/v1/zones | head -c 400
curl -s -i http://localhost:5080/api/v1/zones/NOPE | head -n 12
```
Expected: the first curl returns JSON starting `{"totals":{"ownUsed":` and containing `"kind":"Mapped"`. The second returns `404` with `"code":"NotFound"` and `"title":"Зону NOPE не знайдено."`. Stop the API with Ctrl+C.

- [ ] **Step 8: Commit**

```bash
git add backend
git commit -m "feat(api): zones overview, detail and editable zone CRUD" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Move workflow API (start → target → photo → complete / cancel)

**Files:**
- Create: `backend/Eurocar.Api/Infrastructure/PhotoStorage.cs`, `Infrastructure/CurrentOperator.cs`
- Create: `backend/Eurocar.Api/Features/Moves/MoveService.cs`, `Features/Moves/MovesEndpoints.cs`
- Modify: `backend/Eurocar.Api/Program.cs`
- Test: `backend/Eurocar.Tests/MoveServiceTests.cs`

**Interfaces:**
- Consumes: entities, `Errors`, `ZoneLayout`, `AppDb`, DTOs, `Map.Move` (Tasks 1–3).
- Produces:
  - `IPhotoStorage { Task<string> SaveAsync(Stream, string contentType, CancellationToken = default); Stream? OpenRead(string key); void Delete(string key); void Clear(); }` and `LocalDiskPhotoStorage`.
  - `ICurrentOperator { int RequireId(); }` and `HeaderCurrentOperator` (header `X-Operator-Id`).
  - `MoveService(AppDb, IPhotoStorage, TimeProvider)`: `GetAsync(int id)`, `ActiveAsync(int operatorId)` (returns `MoveDto?`), `JournalAsync(int take = 200)`, `StartAsync(int carId, int operatorId)`, `SetTargetAsync(int id, int operatorId, SetTargetDto)` (null/blank `ZoneCode` clears the target), `AttachPhotoAsync(int id, int operatorId, Stream, string contentType, long length)`, `CompleteAsync(int id, int operatorId)`, `CancelAsync(int id, int operatorId, string? fallbackSpot)`.
  - HTTP:
    - `GET /moves` (journal, completed only, newest first)
    - `GET /moves/active` (200 MoveDto, or 204 when there is none)
    - `POST /moves` with body `{carId}`
    - `PUT /moves/{id}/target` with body `{zoneCode, spotCode}`
    - `POST /moves/{id}/photo` (multipart field `file`)
    - `POST /moves/{id}/complete`
    - `POST /moves/{id}/cancel` with body `{fallbackSpot}`

- [ ] **Step 1: Write the failing tests**

`backend/Eurocar.Tests/MoveServiceTests.cs`:
```csharp
using Eurocar.Api.Domain;
using Eurocar.Api.Features;
using Eurocar.Api.Features.Moves;
using Eurocar.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Tests;

public class MoveServiceTests : IDisposable
{
    const int Op1 = 1, Op2 = 2;
    readonly TestDb _t = TestDb.Create();
    readonly MoveService _svc;
    readonly int _car1, _car2, _car3; // car1: S1·A1 key 1-1, car2: S1·A2 key 1-2, car3: at P01, no key

    public MoveServiceTests()
    {
        var db = _t.Db;
        // S1 has 2 rows × 2 spots: A1 A2 B1 B2.
        var s1 = new Zone { Code = "S1", GroupCode = "3.1", Name = "Соломоново · зона S1", Kind = ZoneKind.Mapped, Capacity = 4, Rows = 2, PerRow = 2 };
        var p01 = new Zone { Code = "P01", GroupCode = "3.4", Name = "Склад партнера · Київ", Kind = ZoneKind.External, Capacity = 10, IsOnline = true };
        var p02 = new Zone { Code = "P02", GroupCode = "3.4", Name = "Склад партнера · Бровари", Kind = ZoneKind.External, Capacity = 10, IsOnline = false };
        var slots = Enumerable.Range(1, 3)
            .Select(n => new KeySlot { Cabinet = 1, Number = n, Status = n <= 2 ? KeySlotStatus.InBox : KeySlotStatus.Empty }).ToList();
        db.Zones.AddRange(s1, p01, p02);
        db.KeySlots.AddRange(slots);
        db.Operators.AddRange(new Operator { DisplayName = "Іваненко А." }, new Operator { DisplayName = "Петренко С." });
        db.Settings.Add(new AppSettings { RequirePhoto = true });
        var c1 = Parked("TMB00000000000001", s1, "A1", slots[0]);
        var c2 = Parked("TMB00000000000002", s1, "A2", slots[1]);
        var c3 = new Car { Vin = "TMB00000000000003", Model = "Kamiq", Color = "сірий", Zone = p01, Status = CarStatus.AtExternal };
        db.Cars.AddRange(c1, c2, c3);
        db.SaveChanges();
        (_car1, _car2, _car3) = (c1.Id, c2.Id, c3.Id);
        _svc = new MoveService(db, new FakePhotos(), TimeProvider.System);
    }

    public void Dispose() => _t.Dispose();

    static Car Parked(string vin, Zone zone, string spot, KeySlot slot) =>
        new() { Vin = vin, Model = "Octavia", Color = "білий", Zone = zone, SpotCode = spot, KeySlot = slot, Status = CarStatus.Parked };

    static async Task<string> CodeOf(Func<Task> act) => (await Assert.ThrowsAsync<DomainException>(act)).Code;

    Task<MoveDto> Photo(int moveId, int op = Op1) => _svc.AttachPhotoAsync(moveId, op, new MemoryStream([1, 2, 3]), "image/jpeg", 3);

    Car Saved(int carId)
    {
        using var db = _t.NewContext();
        return db.Cars.Include(c => c.KeySlot).Include(c => c.Zone).AsNoTracking().Single(c => c.Id == carId);
    }

    [Fact]
    public async Task Start_frees_spot_and_issues_key()
    {
        var m = await _svc.StartAsync(_car1, Op1);

        Assert.Equal(MoveState.InTransit, m.State);
        Assert.Equal(new PlaceDto("S1", "Соломоново · зона S1", "A1"), m.From);
        var car = Saved(_car1);
        Assert.Equal(CarStatus.InTransit, car.Status);
        Assert.Null(car.ZoneId);
        Assert.Null(car.SpotCode);
        Assert.Equal(KeySlotStatus.Issued, car.KeySlot!.Status);
    }

    [Fact]
    public async Task Start_twice_throws_CarBusy()
    {
        await _svc.StartAsync(_car1, Op1);
        Assert.Equal("CarBusy", await CodeOf(() => _svc.StartAsync(_car1, Op2)));
    }

    [Theory]
    [InlineData("A2", "SpotOccupied")]
    [InlineData("A1", "SpotIsOrigin")]
    [InlineData("C1", "SpotNotInZone")]
    [InlineData(" ", "SpotRequired")]
    public async Task SetTarget_rejects_bad_spots(string spot, string code)
    {
        var m = await _svc.StartAsync(_car1, Op1);
        Assert.Equal(code, await CodeOf(() => _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("S1", spot))));
    }

    [Fact]
    public async Task SetTarget_normalizes_spot_input()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        var updated = await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("S1", " b 1 "));
        Assert.Equal("B1", updated.To!.SpotCode);
    }

    [Fact]
    public async Task SetTarget_null_zone_clears_target()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("S1", "B1"));
        var cleared = await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto(null, null));
        Assert.Null(cleared.To);
    }

    [Fact]
    public async Task SetTarget_offline_external_throws_ZoneOffline()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        Assert.Equal("ZoneOffline", await CodeOf(() => _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("P02", null))));
    }

    [Fact]
    public async Task Second_operator_cannot_reserve_same_spot()
    {
        var m1 = await _svc.StartAsync(_car1, Op1);
        await _svc.SetTargetAsync(m1.Id, Op1, new SetTargetDto("S1", "B1"));
        var m2 = await _svc.StartAsync(_car2, Op2);
        Assert.Equal("SpotOccupied", await CodeOf(() => _svc.SetTargetAsync(m2.Id, Op2, new SetTargetDto("S1", "B1"))));
    }

    [Fact]
    public async Task Other_operator_cannot_touch_move()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        Assert.Equal("NotYourMove", await CodeOf(() => _svc.SetTargetAsync(m.Id, Op2, new SetTargetDto("S1", "B1"))));
    }

    [Fact]
    public async Task Complete_requires_target_and_photo()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        Assert.Equal("TargetRequired", await CodeOf(() => _svc.CompleteAsync(m.Id, Op1)));
        await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("S1", "B1"));
        Assert.Equal("PhotoRequired", await CodeOf(() => _svc.CompleteAsync(m.Id, Op1)));
    }

    [Fact]
    public async Task Complete_to_mapped_parks_car_keeps_key_and_numbers_journal()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("S1", "B1"));
        await Photo(m.Id);

        var done = await _svc.CompleteAsync(m.Id, Op1);

        Assert.Equal(MoveState.Completed, done.State);
        Assert.Equal(1, done.No);
        Assert.Equal("Шафа 01 · комірка 01", done.KeyLabel);
        Assert.NotNull(done.PhotoUrl);
        var car = Saved(_car1);
        Assert.Equal(("S1", "B1", CarStatus.Parked), (car.Zone!.Code, car.SpotCode, car.Status));
        Assert.Equal(KeySlotStatus.InBox, car.KeySlot!.Status);
        Assert.Single(await _svc.JournalAsync());
    }

    [Fact]
    public async Task Complete_to_external_releases_key()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("P01", null));
        await Photo(m.Id);

        var done = await _svc.CompleteAsync(m.Id, Op1);

        Assert.Equal("передано з авто", done.KeyLabel);
        var car = Saved(_car1);
        Assert.Equal(CarStatus.AtExternal, car.Status);
        Assert.Null(car.KeySlotId);
        using var db = _t.NewContext();
        Assert.Equal(KeySlotStatus.Empty, db.KeySlots.Single(s => s.Number == 1).Status);
    }

    [Fact]
    public async Task Complete_from_external_assigns_first_free_key()
    {
        var m = await _svc.StartAsync(_car3, Op1);
        Assert.Equal(new PlaceDto("P01", "Склад партнера · Київ", null), m.From);
        await _svc.SetTargetAsync(m.Id, Op1, new SetTargetDto("S1", "B2"));
        await Photo(m.Id);

        var done = await _svc.CompleteAsync(m.Id, Op1);

        Assert.Equal("Шафа 01 · комірка 03", done.KeyLabel);
    }

    [Fact]
    public async Task Cancel_returns_car_and_key_to_origin()
    {
        var m = await _svc.StartAsync(_car1, Op1);
        var cancelled = await _svc.CancelAsync(m.Id, Op1, null);

        Assert.Equal(MoveState.Cancelled, cancelled.State);
        var car = Saved(_car1);
        Assert.Equal(("S1", "A1", CarStatus.Parked), (car.Zone!.Code, car.SpotCode, car.Status));
        Assert.Equal(KeySlotStatus.InBox, car.KeySlot!.Status);
        Assert.Null(await _svc.ActiveAsync(Op1));
    }

    [Fact]
    public async Task Cancel_when_origin_taken_requires_fallback()
    {
        var m1 = await _svc.StartAsync(_car1, Op1);           // car1 leaves A1
        var m2 = await _svc.StartAsync(_car2, Op2);
        await _svc.SetTargetAsync(m2.Id, Op2, new SetTargetDto("S1", "A1"));
        await Photo(m2.Id, Op2);
        await _svc.CompleteAsync(m2.Id, Op2);                  // car2 now parked on A1

        Assert.Equal("OriginTaken", await CodeOf(() => _svc.CancelAsync(m1.Id, Op1, null)));
        await _svc.CancelAsync(m1.Id, Op1, "b1");

        Assert.Equal("B1", Saved(_car1).SpotCode);
    }

    sealed class FakePhotos : IPhotoStorage
    {
        public Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default) => Task.FromResult(Guid.NewGuid().ToString("N"));
        public Stream? OpenRead(string key) => null;
        public void Delete(string key) { }
        public void Clear() { }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend`
Expected: build error `The type or namespace name 'Moves' does not exist in the namespace 'Eurocar.Api.Features'`.

- [ ] **Step 3: Implement photo storage and current operator**

`backend/Eurocar.Api/Infrastructure/PhotoStorage.cs`:
```csharp
namespace Eurocar.Api.Infrastructure;

public interface IPhotoStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default);
    Stream? OpenRead(string key);
    void Delete(string key);
    void Clear();
}

/// <summary>Files on local disk (Docker volume later). Swap for Blob/S3 behind the same interface.</summary>
public sealed class LocalDiskPhotoStorage : IPhotoStorage
{
    readonly string _root;

    public LocalDiskPhotoStorage(IConfiguration cfg, IHostEnvironment env)
    {
        _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, cfg["Photos:Path"] ?? "data/photos"));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        var ext = contentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        var key = $"{Guid.NewGuid():N}{ext}";
        await using var file = File.Create(PathOf(key));
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Stream? OpenRead(string key) => File.Exists(PathOf(key)) ? File.OpenRead(PathOf(key)) : null;

    public void Delete(string key)
    {
        if (File.Exists(PathOf(key))) File.Delete(PathOf(key));
    }

    public void Clear()
    {
        foreach (var f in Directory.EnumerateFiles(_root)) File.Delete(f);
    }

    string PathOf(string key) => Path.Combine(_root, Path.GetFileName(key));
}
```

`backend/Eurocar.Api/Infrastructure/CurrentOperator.cs`:
```csharp
using Eurocar.Api.Domain;

namespace Eurocar.Api.Infrastructure;

/// <summary>Who is acting. Today: X-Operator-Id header; later: Entra ID claims.</summary>
public interface ICurrentOperator
{
    int RequireId();
}

public sealed class HeaderCurrentOperator(IHttpContextAccessor http) : ICurrentOperator
{
    public int RequireId() =>
        int.TryParse(http.HttpContext?.Request.Headers["X-Operator-Id"], out var id) ? id : throw Errors.OperatorRequired();
}
```

- [ ] **Step 4: Implement MoveService**

`backend/Eurocar.Api/Features/Moves/MoveService.cs`:
```csharp
using Eurocar.Api.Domain;
using Eurocar.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Eurocar.Api.Features.Moves;

/// <summary>
/// Move lifecycle: (client-only "selected") → Start → InTransit → SetTarget/Photo → Complete | Cancel.
/// </summary>
public class MoveService(AppDb db, IPhotoStorage photos, TimeProvider clock)
{
    const long MaxPhotoBytes = 10 * 1024 * 1024;
    const int SqliteConstraint = 19;

    DateTime Now => clock.GetUtcNow().UtcDateTime;

    IQueryable<Move> Full() => db.Moves
        .Include(m => m.Car).ThenInclude(c => c.Zone)
        .Include(m => m.Car).ThenInclude(c => c.KeySlot)
        .Include(m => m.Operator)
        .Include(m => m.FromZone)
        .Include(m => m.ToZone)
        .Include(m => m.Photo);

    public async Task<MoveDto> GetAsync(int id) =>
        Map.Move(await Full().AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ?? throw Errors.NotFound("Операцію"));

    public async Task<MoveDto?> ActiveAsync(int operatorId)
    {
        var m = await Full().AsNoTracking().FirstOrDefaultAsync(x => x.OperatorId == operatorId && x.State == MoveState.InTransit);
        return m is null ? null : Map.Move(m);
    }

    public async Task<IReadOnlyList<MoveDto>> JournalAsync(int take = 200) =>
        (await Full().AsNoTracking().Where(m => m.State == MoveState.Completed).OrderByDescending(m => m.No).Take(take).ToListAsync())
        .Select(Map.Move).ToList();

    public async Task<MoveDto> StartAsync(int carId, int operatorId)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == operatorId && o.IsActive)) throw Errors.OperatorRequired();
        var car = await db.Cars.Include(c => c.KeySlot).FirstOrDefaultAsync(c => c.Id == carId) ?? throw Errors.NotFound("Авто");
        if (car.Status == CarStatus.InTransit || car.ZoneId is null) throw Errors.CarBusy();

        var move = new Move
        {
            CarId = car.Id, OperatorId = operatorId, FromZoneId = car.ZoneId.Value, FromSpot = car.SpotCode,
            State = MoveState.InTransit, StartedAt = Now,
        };
        car.Status = CarStatus.InTransit;
        car.ZoneId = null;
        car.SpotCode = null;
        if (car.KeySlot is not null) car.KeySlot.Status = KeySlotStatus.Issued;
        db.Moves.Add(move);
        await SaveAsync(Errors.CarBusy);
        return await GetAsync(move.Id);
    }

    public async Task<MoveDto> SetTargetAsync(int id, int operatorId, SetTargetDto d)
    {
        var m = await LoadActiveAsync(id, operatorId);
        if (string.IsNullOrWhiteSpace(d.ZoneCode))
        {
            m.ToZone = null;
            m.ToZoneId = null;
            m.ToSpot = null;
        }
        else
        {
            var zone = await db.Zones.FirstOrDefaultAsync(z => z.Code == d.ZoneCode && !z.IsDeleted) ?? throw Errors.NotFound($"Зону {d.ZoneCode}");
            string? spot = null;
            if (zone.Kind == ZoneKind.External)
            {
                if (!zone.IsOnline) throw Errors.ZoneOffline();
            }
            else
            {
                spot = ZoneLayout.NormalizeSpot(d.SpotCode) ?? throw Errors.SpotRequired();
                if (!ZoneLayout.SpotIds(zone.Capacity, zone.Rows!.Value, zone.PerRow!.Value).Contains(spot)) throw Errors.SpotNotInZone(zone.Code, spot);
                if (zone.Id == m.FromZoneId && spot == m.FromSpot) throw Errors.SpotIsOrigin();
                if (await IsSpotTakenAsync(zone.Id, spot, m.Id)) throw Errors.SpotOccupied(spot);
            }
            m.ToZone = zone;
            m.ToZoneId = zone.Id;
            m.ToSpot = spot;
        }
        await SaveAsync(() => Errors.SpotOccupied(m.ToSpot ?? ""));
        return await GetAsync(m.Id);
    }

    public async Task<MoveDto> AttachPhotoAsync(int id, int operatorId, Stream content, string contentType, long length)
    {
        if (length <= 0 || length > MaxPhotoBytes || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw Errors.PhotoInvalid();
        var m = await LoadActiveAsync(id, operatorId);
        var key = await photos.SaveAsync(content, contentType);
        var old = m.Photo;
        m.Photo = new Photo { StorageKey = key, ContentType = contentType, TakenAt = Now, SizeBytes = length };
        await db.SaveChangesAsync();
        if (old is not null)
        {
            db.Photos.Remove(old);
            await db.SaveChangesAsync();
            photos.Delete(old.StorageKey);
        }
        return await GetAsync(m.Id);
    }

    public async Task<MoveDto> CompleteAsync(int id, int operatorId)
    {
        var m = await LoadActiveAsync(id, operatorId);
        if (m.ToZone is null) throw Errors.TargetRequired();
        var settings = await db.Settings.AsNoTracking().FirstAsync();
        if (settings.RequirePhoto && m.PhotoId is null) throw Errors.PhotoRequired();

        var car = m.Car;
        var to = m.ToZone;
        if (to.Kind == ZoneKind.Mapped)
        {
            if (await IsSpotTakenAsync(to.Id, m.ToSpot!, m.Id)) throw Errors.SpotOccupied(m.ToSpot!);
            car.Status = CarStatus.Parked;
            car.ZoneId = to.Id;
            car.SpotCode = m.ToSpot;
            car.KeySlot ??= await db.KeySlots.Where(s => s.Status == KeySlotStatus.Empty)
                .OrderBy(s => s.Cabinet).ThenBy(s => s.Number).FirstOrDefaultAsync() ?? throw Errors.KeyBoxFull();
            car.KeySlot.Status = KeySlotStatus.InBox;
            m.KeyLabel = car.KeySlot.Label;
        }
        else
        {
            car.Status = CarStatus.AtExternal;
            car.ZoneId = to.Id;
            car.SpotCode = null;
            if (car.KeySlot is not null)
            {
                car.KeySlot.Status = KeySlotStatus.Empty;
                car.KeySlot = null;
                car.KeySlotId = null;
            }
            m.KeyLabel = "передано з авто";
        }

        m.No = (await db.Moves.MaxAsync(x => x.No) ?? 0) + 1;
        m.State = MoveState.Completed;
        m.CompletedAt = Now;
        await SaveAsync(() => Errors.SpotOccupied(m.ToSpot ?? to.Code));
        return await GetAsync(m.Id);
    }

    public async Task<MoveDto> CancelAsync(int id, int operatorId, string? fallbackSpot)
    {
        var m = await LoadActiveAsync(id, operatorId);
        var car = m.Car;
        var from = m.FromZone;
        if (from.Kind == ZoneKind.External)
        {
            car.Status = CarStatus.AtExternal;
            car.ZoneId = from.Id;
            car.SpotCode = null;
        }
        else
        {
            var fallback = ZoneLayout.NormalizeSpot(fallbackSpot);
            var spot = fallback ?? m.FromSpot!;
            var valid = ZoneLayout.SpotIds(from.Capacity, from.Rows!.Value, from.PerRow!.Value);
            if (fallback is not null && !valid.Contains(fallback)) throw Errors.SpotNotInZone(from.Code, fallback);
            if (!valid.Contains(spot) || await IsSpotTakenAsync(from.Id, spot, m.Id))
                throw fallback is null ? Errors.OriginTaken(spot) : Errors.SpotOccupied(spot);
            car.Status = CarStatus.Parked;
            car.ZoneId = from.Id;
            car.SpotCode = spot;
        }
        if (car.KeySlot is not null) car.KeySlot.Status = KeySlotStatus.InBox;
        m.State = MoveState.Cancelled;
        m.CompletedAt = Now;
        m.ToZone = null;
        m.ToZoneId = null;
        m.ToSpot = null;
        await SaveAsync(Errors.Conflict);
        return await GetAsync(m.Id);
    }

    async Task<Move> LoadActiveAsync(int id, int operatorId)
    {
        var m = await db.Moves
            .Include(x => x.Car).ThenInclude(c => c.KeySlot)
            .Include(x => x.FromZone).Include(x => x.ToZone).Include(x => x.Photo)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw Errors.NotFound("Операцію");
        if (m.State != MoveState.InTransit) throw Errors.Conflict();
        if (m.OperatorId != operatorId) throw Errors.NotYourMove();
        return m;
    }

    async Task<bool> IsSpotTakenAsync(int zoneId, string spot, int exceptMoveId) =>
        await db.Cars.AnyAsync(c => c.ZoneId == zoneId && c.SpotCode == spot)
        || await db.Moves.AnyAsync(x => x.Id != exceptMoveId && x.State == MoveState.InTransit && x.ToZoneId == zoneId && x.ToSpot == spot);

    /// <summary>Unique indexes are the last line of defence against two devices racing for the same spot/key.</summary>
    async Task SaveAsync(Func<DomainException> onConflict)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteErrorCode: SqliteConstraint })
        {
            throw onConflict();
        }
    }
}
```

- [ ] **Step 5: Add endpoints and wire Program.cs**

`backend/Eurocar.Api/Features/Moves/MovesEndpoints.cs`:
```csharp
using Eurocar.Api.Infrastructure;

namespace Eurocar.Api.Features.Moves;

public static class MovesEndpoints
{
    public static void MapMoves(this RouteGroupBuilder api)
    {
        api.MapGet("/moves", (MoveService s) => s.JournalAsync());
        api.MapGet("/moves/active", async (MoveService s, ICurrentOperator op) =>
            await s.ActiveAsync(op.RequireId()) is { } m ? Results.Ok(m) : Results.NoContent());
        api.MapPost("/moves", (StartMoveDto d, MoveService s, ICurrentOperator op) => s.StartAsync(d.CarId, op.RequireId()));
        api.MapPut("/moves/{id:int}/target", (int id, SetTargetDto d, MoveService s, ICurrentOperator op) =>
            s.SetTargetAsync(id, op.RequireId(), d));
        api.MapPost("/moves/{id:int}/photo", async (int id, IFormFile file, MoveService s, ICurrentOperator op) =>
        {
            await using var stream = file.OpenReadStream();
            return await s.AttachPhotoAsync(id, op.RequireId(), stream, file.ContentType, file.Length);
        }).DisableAntiforgery();
        api.MapPost("/moves/{id:int}/complete", (int id, MoveService s, ICurrentOperator op) => s.CompleteAsync(id, op.RequireId()));
        api.MapPost("/moves/{id:int}/cancel", (int id, CancelMoveDto? d, MoveService s, ICurrentOperator op) =>
            s.CancelAsync(id, op.RequireId(), d?.FallbackSpot));
    }
}
```

In `backend/Eurocar.Api/Program.cs`:
- add `using Eurocar.Api.Features.Moves;`
- after `builder.Services.AddScoped<ZoneService>();` add:
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOperator, HeaderCurrentOperator>();
builder.Services.AddSingleton<IPhotoStorage, LocalDiskPhotoStorage>();
builder.Services.AddScoped<MoveService>();
```
- after `api.MapZones();` add `api.MapMoves();`.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test backend`
Expected: `Passed! - Failed: 0, Passed: 39`.

- [ ] **Step 7: Commit**

```bash
git add backend
git commit -m "feat(api): move workflow with spot/key guarantees and photo upload" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Search, keys, operators, settings, photos, demo reset + external stock simulator

**Files:**
- Create: `backend/Eurocar.Api/Features/Cars/CarQueries.cs`, `Features/MiscEndpoints.cs`
- Create: `backend/Eurocar.Api/Integration/ExternalStock.cs`
- Modify: `backend/Eurocar.Api/Infrastructure/DbBootstrap.cs` (add `Reset`), `backend/Eurocar.Api/Program.cs`
- Test: `backend/Eurocar.Tests/SimulatorTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 1–4.
- Produces:
  - `CarQueries.SearchAsync(AppDb, string? q)`: at least 3 characters after removing spaces and uppercasing, at most 8 results.
  - `DbBootstrap.Reset(AppDb, TimeProvider, IPhotoStorage)`.
  - `IExternalStockProvider { Task PollAsync(AppDb db, CancellationToken ct); }`, `SimulatedExternalStockProvider(TimeProvider[, Random])`, `ExternalStockPoller`.
  - `RouteGroupBuilder.MapMisc()`.
  - HTTP:
    - `GET /cars/search?vin=`, `GET /cars/{id}`
    - `GET /keys/summary`, `GET /keys/cabinets/{n}`
    - `GET /operators`, `GET /settings`, `PUT /settings`
    - `GET /photos/{id}`
    - `POST /demo/reset` (204, or 404 when not in Demo mode)

- [ ] **Step 1: Write the failing tests**

`backend/Eurocar.Tests/SimulatorTests.cs`:
```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend`
Expected: build error `The type or namespace name 'Integration' does not exist in the namespace 'Eurocar.Api'`.

- [ ] **Step 3: Implement search and the simulator**

`backend/Eurocar.Api/Features/Cars/CarQueries.cs`:
```csharp
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
```

`backend/Eurocar.Api/Integration/ExternalStock.cs`:
```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend`
Expected: `Passed! - Failed: 0, Passed: 41`.

- [ ] **Step 5: Add reset, misc endpoints and final Program.cs**

Add to `backend/Eurocar.Api/Infrastructure/DbBootstrap.cs`, inside the class:
```csharp
    public static void Reset(AppDb db, TimeProvider clock, IPhotoStorage photos)
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        photos.Clear();
        DemoSeeder.Seed(db, clock.GetUtcNow().UtcDateTime);
    }
```

`backend/Eurocar.Api/Features/MiscEndpoints.cs`:
```csharp
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
```

`backend/Eurocar.Api/Program.cs` (final, full replacement):
```csharp
using System.Text.Json.Serialization;
using Eurocar.Api.Features;
using Eurocar.Api.Features.Moves;
using Eurocar.Api.Features.Zones;
using Eurocar.Api.Infrastructure;
using Eurocar.Api.Integration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("Db") ?? $"Data Source={Path.Combine(dataDir, "eurocar.db")}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOperator, HeaderCurrentOperator>();
builder.Services.AddSingleton<IPhotoStorage, LocalDiskPhotoStorage>();
builder.Services.AddScoped<ZoneService>();
builder.Services.AddScoped<MoveService>();
builder.Services.AddSingleton<IExternalStockProvider, SimulatedExternalStockProvider>();
builder.Services.AddHostedService<ExternalStockPoller>();

var app = builder.Build();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
    DbBootstrap.Init(scope.ServiceProvider.GetRequiredService<AppDb>(), app.Configuration, TimeProvider.System);

var api = app.MapGroup("/api/v1");
api.MapZones();
api.MapMoves();
api.MapMisc();

app.Run();

public partial class Program;
```

- [ ] **Step 6: Run tests, then smoke-test the whole flow**

Run: `dotnet test backend`
Expected: `Passed! - Failed: 0, Passed: 41`.

Then (bash; start the API in one terminal with `cd backend/Eurocar.Api && dotnet run`):
```bash
H='-H X-Operator-Id:1 -H Content-Type:application/json'
curl -s "http://localhost:5080/api/v1/cars/search?vin=tmb" | head -c 200; echo
CAR=$(curl -s "http://localhost:5080/api/v1/zones/S1" | grep -o '"car":{"id":[0-9]*' | head -1 | grep -o '[0-9]*$')
MOVE=$(curl -s $H -d "{\"carId\":$CAR}" http://localhost:5080/api/v1/moves | grep -o '^{"id":[0-9]*' | grep -o '[0-9]*$')
curl -s $H -X PUT -d '{"zoneCode":"P01","spotCode":null}' http://localhost:5080/api/v1/moves/$MOVE/target | head -c 120; echo
curl -s $H -X POST http://localhost:5080/api/v1/moves/$MOVE/complete; echo
curl -s -H X-Operator-Id:1 -F "file=@docs/design/modernist-styles.css;type=image/jpeg" http://localhost:5080/api/v1/moves/$MOVE/photo | head -c 80; echo
curl -s $H -X POST http://localhost:5080/api/v1/moves/$MOVE/complete | grep -o '"no":[0-9]*'
curl -s http://localhost:5080/api/v1/keys/summary | head -c 120; echo
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5080/api/v1/demo/reset
```
Expected, in order:
1. A JSON array of cars.
2. The move with `"to":{"zoneCode":"P01"`.
3. The first `complete` returns `"code":"PhotoRequired"`.
4. The photo upload returns move JSON. The server only checks the `image/*` type, so any small file works.
5. `"no":4`.
6. The keys summary JSON.
7. `204`.

Stop the API.

- [ ] **Step 7: Commit**

```bash
git add backend
git commit -m "feat(api): search, keys, settings, photos, demo reset and external stock simulator" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Angular scaffold, design styles, core services, shared helpers

**Files:**
- Create: `frontend/` (Angular CLI), `frontend/proxy.conf.json`, `frontend/src/styles/modernist.css`
- Modify: `frontend/package.json` (start script), `frontend/src/index.html`, `frontend/src/styles.css`, `frontend/src/app/app.config.ts`
- Create: `frontend/src/app/core/{models,api,errors,notify,connection,operator,settings,clock,viewport,search,lightbox,mobile-bar,poll,move-store}.ts`
- Create: `frontend/src/app/shared/{format,image,zone-layout}.ts`
- Test: `frontend/src/app/shared/zone-layout.spec.ts`

**Interfaces:**
- Consumes: the HTTP API from Tasks 3–5 (DTO shapes in `Features/Dtos.cs`).
- Produces (later tasks use exactly these names):
  - `models.ts`: `ZoneKind, Priority, CarStatus, KeySlotStatus, MoveState, CellLabelMode, Car, ZoneRow, ZoneGroup, Totals, ZonesOverview, Spot, ZoneDetail, ZoneUpsert, Place, Move, KeysSummary, KeyCell, Operator, Settings`.
  - `Api` (root service, every method returns a `Promise`): `zones()`, `zone(code)`, `createZone(d)`, `updateZone(code,d)`, `deleteZone(code)`, `searchCars(vin)`, `activeMove()`, `journal()`, `startMove(carId)`, `setTarget(id, zoneCode|null, spotCode|null)`, `uploadPhoto(id, blob)`, `completeMove(id)`, `cancelMove(id, fallbackSpot|null)`, `keysSummary()`, `cabinet(n)`, `operators()`, `settings()`, `resetDemo()`.
  - `errorText(e): string`, `errorCode(e): string | null`.
  - Root services:
    - `Notify`: `message` signal, `show(text, ms?)`, `error(e)`, `mock(feature)`.
    - `Connection`: `online` signal, plus `connectionInterceptor`.
    - `OperatorStore`: `list`, `id`, `current`, `load()`, `set(id)`, plus `operatorInterceptor`.
    - `SettingsStore`: `value`, `load()`.
    - `Clock`: `now`.
    - `Viewport`: `width`, `mob`, `wide`.
    - `SearchStore`: `query`, `needle`, `results`, `noResults`, `matches(vin)`, `clear()`.
    - `Lightbox`: `current`, `open(src, caption)`, `close()`.
    - `MobileBarStore`: `bar`. Type `MobileBar { kicker; title; kind: 'btn'|'photo'|'text'; label?; action?(); onFile?(f) }`.
    - `MoveStore`, described below.
  - `pollEvery(ms, fn)`: call only in an injection context; it stops itself on destroy.
  - `MoveStore` signals: `selected`, `active`, `result`, `busy`, `originTaken`, `version`, `phase` (`'idle'|'selected'|'moving'|'done'`), `car`, `from`, `to`.
  - `MoveStore` methods:
    - `resume()`, `select(car): boolean`, `reset()`, `start()`
    - `setTarget(zoneCode|null, spotCode?, quiet?) → Promise<string|null>` (error text, or null on success)
    - `photo(file)`, `finish()`
    - `cancel(fallback?) → Promise<string|null>`
  - `format.ts`: `pad, hms, dur, ago, zonePlural, keyLabel, locLabel, vinTail, moveDuration, val, PRI_LABEL, PRI_CLASS, CAR_COLOR`.
  - `image.ts`: `compressImage(file, max?, quality?) → Promise<Blob>`.
  - `zone-layout.ts`: `LETTERS, ROAD, GATE, Pt, Geom, spotIds, geom, spotBox, canvasSize, pathFromGate, pathBetween, pathLength, nearestFree`.

- [ ] **Step 1: Scaffold the Angular app**

Run from `D:\ЄврокарМВП` (bash):
```bash
npx -y @angular/cli@latest new frontend --style=css --ssr=false --routing --skip-git --zoneless --defaults
mkdir -p frontend/src/styles && cp docs/design/modernist-styles.css frontend/src/styles/modernist.css
cd frontend && npx ng version | head -n 20 && cd ..
```
Expected: the project is generated with `src/app/app.ts`, `app.html`, `app.config.ts` and `app.routes.ts`, and Angular is version 20 or later. If the CLI generated `app.component.ts` instead, it is Angular 19 or older: delete `frontend` and rerun with `@angular/cli@latest`.

`frontend/proxy.conf.json`:
```json
{
  "/api": { "target": "http://localhost:5080", "secure": false, "changeOrigin": true }
}
```

In `frontend/package.json`, set `"start": "ng serve --proxy-config proxy.conf.json"` in `"scripts"`.

`frontend/src/index.html`:
```html
<!doctype html>
<html lang="uk">
<head>
  <meta charset="utf-8">
  <title>Євро Лоджистікс · Логістика авто</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Archivo:wght@400;500;600;700;800&display=swap">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

`frontend/src/styles.css`:
```css
@import './styles/modernist.css';

/* Palette overrides from the prototype (docs/design/Логистика.dc.html, lines 14–17). */
:root {
  --color-bg: #ffffff;
  --color-surface: #eef1f0;
  --color-accent: #0e3a2f;
  --color-accent-100: oklch(0.96 0.02 170);
  --color-accent-200: oklch(0.91 0.035 170);
  --color-accent-300: oklch(0.82 0.05 170);
  --color-accent-400: oklch(0.68 0.07 170);
  --color-accent-500: oklch(0.50 0.07 170);
  --color-accent-600: oklch(0.40 0.06 170);
  --color-accent-700: oklch(0.33 0.055 170);
  --color-accent-800: oklch(0.27 0.045 170);
  --color-accent-900: oklch(0.21 0.035 170);
  --color-accent-2: #78faae;
  --color-accent-2-100: oklch(0.97 0.04 157);
  --color-accent-2-200: oklch(0.94 0.08 157);
  --color-accent-2-300: oklch(0.91 0.13 157);
  --color-accent-2-400: oklch(0.89 0.17 157);
  --color-accent-2-500: oklch(0.80 0.17 157);
  --color-accent-2-600: oklch(0.68 0.15 157);
  --color-accent-2-700: oklch(0.55 0.12 157);
  --color-accent-2-800: oklch(0.42 0.09 157);
  --color-accent-2-900: oklch(0.32 0.06 157);
}
body { margin: 0; background: var(--color-bg); color: var(--color-text); font-family: var(--font-body); }
a { color: var(--color-accent-700); }
a:hover { color: var(--color-accent-500); }

/* Building blocks shared by pages; values copied from the prototype's inline styles. */
.kicker { font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.08em; color: var(--color-accent-500); }
.kicker.muted { color: var(--color-neutral-600); }
.title-xl { font-size: 40px; font-weight: 800; letter-spacing: -0.02em; line-height: 1.1; }
.title-lg { font-size: 32px; font-weight: 800; letter-spacing: -0.01em; line-height: 1.1; }
.tnum { font-variant-numeric: tabular-nums; }
.stats { display: grid; border-top: 2px solid var(--color-text); border-bottom: 2px solid var(--color-text); }
.stats > div { padding: var(--space-3); }
.stats > div:first-child { padding-left: 0; }
.stats > div + div { border-left: 1px solid var(--color-divider); }
.stat-n { font-size: 28px; font-weight: 800; white-space: nowrap; }
.split { display: flex; flex-wrap: wrap; flex: 1; border-bottom: 2px solid var(--color-text); }
.split > section { flex: 1 1 680px; min-width: 0; padding: var(--space-6); border-right: 2px solid var(--color-text); display: flex; flex-direction: column; gap: var(--space-4); box-sizing: border-box; }
.split > aside { flex: 1 1 360px; max-width: 100%; display: flex; flex-direction: column; box-sizing: border-box; }
.cells { display: grid; gap: 2px; background: var(--color-neutral-300); border: 2px solid var(--color-text); }
.cell { all: unset; box-sizing: border-box; display: flex; flex-direction: column; justify-content: space-between; min-width: 0; overflow: hidden; cursor: pointer; }
.cell-n { font-size: 10px; font-weight: 700; opacity: 0.7; }
.cell-l { font-size: 10px; font-weight: 600; line-height: 1.1; word-break: break-all; }
.legend { display: flex; gap: var(--space-6); flex-wrap: wrap; font-size: 13px; }
.legend > div { display: flex; gap: 8px; align-items: center; }
.sw { display: inline-block; width: 14px; height: 14px; }
.err { font-size: 13px; color: var(--color-accent-2-900); background: var(--color-accent-2-100); padding: 4px 8px; }
.row-line { display: flex; flex-wrap: wrap; align-items: center; border-bottom: 1px solid var(--color-divider); }
.input-lg { font-size: 16px; min-height: 44px; box-sizing: border-box; }
.file-hidden { position: absolute; opacity: 0; width: 1px; height: 1px; pointer-events: none; }
/* While offline, actions inside pages are visibly disabled. */
.offline main .btn, .offline main .cell, .offline main [role='button'] { pointer-events: none; opacity: 0.45; }
```

- [ ] **Step 2: Write the failing zone-layout test**

`frontend/src/app/shared/zone-layout.spec.ts`:
```ts
import { geom, nearestFree, pathBetween, pathFromGate, pathLength, spotIds } from './zone-layout';

describe('zone-layout (mirrors backend ZoneLayout)', () => {
  it('fills rows until capacity', () => {
    const ids = spotIds(40, 8, 6);
    expect(ids.length).toBe(40);
    expect(ids[0]).toBe('A1');
    expect(ids[6]).toBe('B1');
    expect(ids[39]).toBe('G4');
  });

  it('computes spot geometry like the prototype', () => {
    const a1 = geom('A1');
    expect(a1.cx).toBeCloseTo(2.6);
    expect(a1.cy).toBeCloseTo(4.5);
    expect(a1.ax).toBeCloseTo(0.8);
    expect(geom('B1').ax).toBeCloseTo(6.4);
  });

  it('builds routes from the gate and between spots', () => {
    expect(pathLength(pathFromGate(geom('A1')))).toBeCloseTo(5.3);
    expect(pathLength(pathBetween(geom('A1'), geom('A3')))).toBeCloseTo(5.6);
    expect(pathLength(pathBetween(geom('A1'), geom('B1')))).toBeCloseTo(12);
  });

  it('orders free spots by distance', () => {
    const ids = spotIds(4, 2, 2);
    expect(nearestFree(ids, s => s !== 'A1', geom('A1'))).toEqual(['A2', 'B1', 'B2']);
  });
});
```

- [ ] **Step 3: Run test to verify it fails**

Run: `cd frontend && npx ng test --watch=false`
Expected: FAIL. The compile error is `Cannot find module './zone-layout'`.

- [ ] **Step 4: Implement zone-layout**

`frontend/src/app/shared/zone-layout.ts`:
```ts
/** Spot grid + aerial-map geometry. Ported from the prototype (Логистика.dc.html, lines 532–541). */
export const LETTERS = 'ABCDEFGHIJKLMNOPQRST';
export const ROAD = 3.1;
export const GATE = { cx: 0.8, cy: 1 };
export type Pt = [number, number];
export interface Geom { cx: number; cy: number; ax: number; }

export function spotIds(capacity: number, rows: number, perRow: number): string[] {
  const out: string[] = [];
  for (let r = 0; r < rows && out.length < capacity; r++)
    for (let n = 1; n <= perRow && out.length < capacity; n++) out.push(LETTERS[r] + n);
  return out;
}

export function geom(spot: string): Geom {
  const r = LETTERS.indexOf(spot[0]), n = +spot.slice(1), p = r >> 1, col = r % 2;
  return { cx: 1.6 + p * 5.6 + col * 2 + 1, cy: 3 + n + 0.5, ax: col ? (p + 1) * 5.6 + 0.8 : p * 5.6 + 0.8 };
}

/** Top-left corner of a spot in map units; odd rows face the other aisle. */
export function spotBox(spot: string): { x: number; y: number; flipped: boolean } {
  const r = LETTERS.indexOf(spot[0]), n = +spot.slice(1), p = r >> 1, col = r % 2;
  return { x: 1.6 + p * 5.6 + col * 2, y: 3 + n, flipped: col === 1 };
}

export const canvasSize = (rows: number, perRow: number) => ({ W: 1.6 + (rows / 2) * 5.6, H: perRow + 6 });

export const pathFromGate = (s: Geom): Pt[] => [[0.8, 1], [0.8, ROAD], [s.ax, ROAD], [s.ax, s.cy], [s.cx, s.cy]];

export const pathBetween = (a: Geom, b: Geom): Pt[] =>
  a.ax === b.ax
    ? [[a.cx, a.cy], [a.ax, a.cy], [a.ax, b.cy], [b.cx, b.cy]]
    : [[a.cx, a.cy], [a.ax, a.cy], [a.ax, ROAD], [b.ax, ROAD], [b.ax, b.cy], [b.cx, b.cy]];

export const pathLength = (pts: Pt[]) =>
  pts.reduce((sum, p, i) => (i ? sum + Math.abs(p[0] - pts[i - 1][0]) + Math.abs(p[1] - pts[i - 1][1]) : 0), 0);

export function nearestFree(ids: string[], isFree: (s: string) => boolean, ref: { cx: number; cy: number }, n = 6): string[] {
  return ids
    .filter(isFree)
    .map(s => { const g = geom(s); return { s, d: Math.abs(g.cx - ref.cx) + Math.abs(g.cy - ref.cy) }; })
    .sort((a, b) => a.d - b.d)
    .slice(0, n)
    .map(x => x.s);
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `cd frontend && npx ng test --watch=false`
Expected: PASS for all 4 `zone-layout` specs and the CLI's default `app.spec.ts`.

- [ ] **Step 6: Write formatting/image helpers, models, API client and core services**

`frontend/src/app/shared/format.ts`:
```ts
import type { Move, Priority } from '../core/models';

export const pad = (n: number) => String(n).padStart(2, '0');

export const hms = (t: number | string) => {
  const d = new Date(t);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
};

export const dur = (ms: number) => {
  const s = Math.max(0, Math.floor(ms / 1000));
  return pad(Math.floor(s / 60)) + ':' + pad(s % 60);
};

export const ago = (ms: number) => {
  const m = Math.floor(ms / 60000);
  return m < 1 ? 'щойно' : m < 60 ? m + ' хв тому' : Math.floor(m / 60) + ' год тому';
};

export const zonePlural = (n: number) => {
  const a = n % 10, b = n % 100;
  return a === 1 && b !== 11 ? 'зона' : a >= 2 && a <= 4 && (b < 10 || b >= 20) ? 'зони' : 'зон';
};

export const keyLabel = (cabinet?: number | null, num?: number | null) =>
  cabinet && num ? `Шафа ${pad(cabinet)} · комірка ${pad(num)}` : '';

export const locLabel = (l?: { zoneCode: string | null; spotCode?: string | null } | null) =>
  l?.zoneCode ? l.zoneCode + (l.spotCode ? ' · ' + l.spotCode : '') : '—';

export const vinTail = (vin: string) => vin.slice(-6);

export const moveDuration = (m: Move) => (m.completedAt ? dur(Date.parse(m.completedAt) - Date.parse(m.startedAt)) : '00:00');

/** Value of an <input>/<select> from a DOM event (templates cannot cast). */
export const val = (e: Event) => (e.target as HTMLInputElement).value;

export const PRI_LABEL: Record<Priority, string> = { High: 'Високий', Normal: 'Звичайний', Low: 'Низький' };
export const PRI_CLASS: Record<Priority, string> = { High: 'tag-accent-2', Normal: 'tag-neutral', Low: 'tag-outline' };

export const CAR_COLOR: Record<string, string> = {
  'білий': 'var(--color-neutral-100)', 'сріблястий': 'var(--color-neutral-300)', 'сірий': 'var(--color-neutral-500)',
  'синій': 'var(--color-neutral-600)', 'червоний': 'var(--color-neutral-700)', 'чорний': 'var(--color-neutral-900)',
};
```

`frontend/src/app/shared/image.ts`:
```ts
/** Downscale a camera photo before upload (phones produce 5–12 MB files). */
export async function compressImage(file: File, max = 1600, quality = 0.8): Promise<Blob> {
  const url = URL.createObjectURL(file);
  try {
    const img = await new Promise<HTMLImageElement>((resolve, reject) => {
      const i = new Image();
      i.onload = () => resolve(i);
      i.onerror = reject;
      i.src = url;
    });
    const s = Math.min(1, max / Math.max(img.width, img.height));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(img.width * s);
    canvas.height = Math.round(img.height * s);
    canvas.getContext('2d')!.drawImage(img, 0, 0, canvas.width, canvas.height);
    return await new Promise<Blob>((resolve, reject) =>
      canvas.toBlob(b => (b ? resolve(b) : reject(new Error('encode failed'))), 'image/jpeg', quality));
  } finally {
    URL.revokeObjectURL(url);
  }
}
```

`frontend/src/app/core/models.ts`:
```ts
/** Mirrors backend/Eurocar.Api/Features/Dtos.cs (camelCase JSON, string enums, UTC ISO dates). */
export type ZoneKind = 'Mapped' | 'External';
export type Priority = 'High' | 'Normal' | 'Low';
export type CarStatus = 'Parked' | 'InTransit' | 'AtExternal';
export type KeySlotStatus = 'Empty' | 'InBox' | 'Issued';
export type MoveState = 'InTransit' | 'Completed' | 'Cancelled';
export type CellLabelMode = 'Vin' | 'Model';

export interface Car {
  id: number; vin: string; model: string; color: string; status: CarStatus;
  zoneCode: string | null; zoneName: string | null; spotCode: string | null;
  keyCabinet: number | null; keyNumber: number | null; keyStatus: KeySlotStatus | null;
}
export interface ZoneRow {
  code: string; name: string; groupCode: string; kind: ZoneKind; capacity: number; used: number; areaM2: number | null;
  priority: Priority; isOnline: boolean; lastSyncedAt: string | null; editable: boolean;
}
export interface ZoneGroup { code: string; name: string; editable: boolean; zones: ZoneRow[]; }
export interface Totals { ownUsed: number; ownCap: number; extUsed: number; extCap: number; online: number; extCount: number; ops: number; }
export interface ZonesOverview { totals: Totals; groups: ZoneGroup[]; }
export interface Spot { code: string; car: Car | null; reserved: boolean; }
export interface ZoneDetail {
  code: string; name: string; groupCode: string; groupName: string; kind: ZoneKind; capacity: number; used: number;
  rows: number | null; perRow: number | null; areaM2: number | null; priority: Priority; stripLabel: string;
  isOnline: boolean; lastSyncedAt: string | null; spots: Spot[]; cars: Car[];
}
export interface ZoneUpsert { groupCode: string; name: string; areaM2: number; priority: Priority; }
export interface Place { zoneCode: string; zoneName: string; spotCode: string | null; }
export interface Move {
  id: number; no: number | null; state: MoveState; car: Car; operatorId: number; operatorName: string;
  from: Place; to: Place | null; startedAt: string; completedAt: string | null;
  photoUrl: string | null; photoTakenAt: string | null; keyLabel: string | null;
}
export interface KeysSummary { inBox: number; issued: number; empty: number; cabinets: { cabinet: number; count: number }[]; }
export interface KeyCell { number: number; status: KeySlotStatus; car: Car | null; }
export interface Operator { id: number; displayName: string; }
export interface Settings { requirePhoto: boolean; cellLabelMode: CellLabelMode; externalFeedEnabled: boolean; }
```

`frontend/src/app/core/api.ts`:
```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Car, KeyCell, KeysSummary, Move, Operator, Settings, ZoneDetail, ZoneRow, ZonesOverview, ZoneUpsert } from './models';

const BASE = '/api/v1';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  private get<T>(url: string) { return firstValueFrom(this.http.get<T>(BASE + url)); }
  private send<T>(method: 'POST' | 'PUT' | 'DELETE', url: string, body?: unknown) {
    return firstValueFrom(this.http.request<T>(method, BASE + url, { body }));
  }

  zones() { return this.get<ZonesOverview>('/zones'); }
  zone(code: string) { return this.get<ZoneDetail>(`/zones/${encodeURIComponent(code)}`); }
  createZone(d: ZoneUpsert) { return this.send<ZoneRow>('POST', '/zones', d); }
  updateZone(code: string, d: ZoneUpsert) { return this.send<ZoneRow>('PUT', `/zones/${encodeURIComponent(code)}`, d); }
  deleteZone(code: string) { return this.send<void>('DELETE', `/zones/${encodeURIComponent(code)}`); }

  searchCars(vin: string) { return this.get<Car[]>(`/cars/search?vin=${encodeURIComponent(vin)}`); }

  async activeMove(): Promise<Move | null> {
    const res = await firstValueFrom(this.http.get<Move>(`${BASE}/moves/active`, { observe: 'response' }));
    return res.body ?? null; // 204 → null
  }
  journal() { return this.get<Move[]>('/moves'); }
  startMove(carId: number) { return this.send<Move>('POST', '/moves', { carId }); }
  setTarget(id: number, zoneCode: string | null, spotCode: string | null = null) {
    return this.send<Move>('PUT', `/moves/${id}/target`, { zoneCode, spotCode });
  }
  uploadPhoto(id: number, photo: Blob) {
    const form = new FormData();
    form.append('file', photo, 'photo.jpg');
    return firstValueFrom(this.http.post<Move>(`${BASE}/moves/${id}/photo`, form));
  }
  completeMove(id: number) { return this.send<Move>('POST', `/moves/${id}/complete`, {}); }
  cancelMove(id: number, fallbackSpot: string | null = null) { return this.send<Move>('POST', `/moves/${id}/cancel`, { fallbackSpot }); }

  keysSummary() { return this.get<KeysSummary>('/keys/summary'); }
  cabinet(n: number) { return this.get<KeyCell[]>(`/keys/cabinets/${n}`); }

  operators() { return this.get<Operator[]>('/operators'); }
  settings() { return this.get<Settings>('/settings'); }
  resetDemo() { return this.send<void>('POST', '/demo/reset'); }
}
```

`frontend/src/app/core/errors.ts`:
```ts
import { HttpErrorResponse } from '@angular/common/http';

export function errorCode(e: unknown): string | null {
  return e instanceof HttpErrorResponse ? (e.error?.code ?? null) : null;
}

/** Server titles are already Ukrainian operator-facing messages. */
export function errorText(e: unknown): string {
  if (e instanceof HttpErrorResponse) {
    if (e.status === 0) return 'Немає з’єднання з сервером.';
    if (e.error?.code && typeof e.error.title === 'string') return e.error.title;
    return `Помилка сервера (${e.status}). Спробуйте ще раз.`;
  }
  return 'Щось пішло не так.';
}
```

`frontend/src/app/core/notify.ts`:
```ts
import { Injectable, signal } from '@angular/core';
import { errorText } from './errors';

@Injectable({ providedIn: 'root' })
export class Notify {
  readonly message = signal<string | null>(null);
  private timer?: ReturnType<typeof setTimeout>;

  show(text: string, ms = 4000) {
    this.message.set(text);
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.message.set(null), ms);
  }
  error(e: unknown) { this.show(errorText(e)); }
  /** For demo-only buttons whose real behaviour needs an integration. */
  mock(feature: string) { this.show(`${feature}: буде доступно після інтеграції.`); }
}
```

`frontend/src/app/core/connection.ts`:
```ts
import { HttpErrorResponse, HttpEventType, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class Connection {
  readonly online = signal(navigator.onLine);
  constructor() {
    addEventListener('online', () => this.online.set(true));
    addEventListener('offline', () => this.online.set(false));
  }
}

/** Status 0 = server unreachable → banner; any response → back online. */
export const connectionInterceptor: HttpInterceptorFn = (req, next) => {
  const conn = inject(Connection);
  return next(req).pipe(tap({
    next: e => { if (e.type === HttpEventType.Response) conn.online.set(true); },
    error: (e: HttpErrorResponse) => conn.online.set(e.status !== 0),
  }));
};
```

`frontend/src/app/core/operator.ts`:
```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Api } from './api';
import { Operator } from './models';

const KEY = 'eurocar.operatorId';

/** Pilot "identity": operator picked from a list. Replaced by Entra ID later. */
@Injectable({ providedIn: 'root' })
export class OperatorStore {
  private readonly api = inject(Api);
  readonly list = signal<Operator[]>([]);
  readonly id = signal<number | null>(Number(localStorage.getItem(KEY)) || null);
  readonly current = computed(() => this.list().find(o => o.id === this.id()) ?? null);

  async load() {
    const ops = await this.api.operators();
    this.list.set(ops);
    if (ops.length && !ops.some(o => o.id === this.id())) this.set(ops[0].id);
  }

  set(id: number) {
    this.id.set(id);
    localStorage.setItem(KEY, String(id));
  }
}

export const operatorInterceptor: HttpInterceptorFn = (req, next) => {
  const id = inject(OperatorStore).id();
  return next(id ? req.clone({ setHeaders: { 'X-Operator-Id': String(id) } }) : req);
};
```

`frontend/src/app/core/settings.ts`:
```ts
import { Injectable, inject, signal } from '@angular/core';
import { Api } from './api';
import { Settings } from './models';

@Injectable({ providedIn: 'root' })
export class SettingsStore {
  private readonly api = inject(Api);
  readonly value = signal<Settings>({ requirePhoto: true, cellLabelMode: 'Vin', externalFeedEnabled: true });
  async load() {
    try { this.value.set(await this.api.settings()); } catch { /* keep defaults; the offline banner explains */ }
  }
}
```

`frontend/src/app/core/clock.ts`:
```ts
import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Clock {
  readonly now = signal(Date.now());
  constructor() { setInterval(() => this.now.set(Date.now()), 1000); }
}
```

`frontend/src/app/core/viewport.ts`:
```ts
import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Viewport {
  readonly width = signal(window.innerWidth);
  readonly mob = computed(() => this.width() < 760);
  readonly wide = computed(() => this.width() >= 1100);
  constructor() { addEventListener('resize', () => this.width.set(window.innerWidth)); }
}
```

`frontend/src/app/core/search.ts`:
```ts
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Api } from './api';
import { Car } from './models';

/** Header VIN search; maps and the key box highlight matches. */
@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly api = inject(Api);
  readonly query = signal('');
  readonly needle = computed(() => this.query().trim().toUpperCase().replace(/\s/g, ''));
  readonly results = signal<Car[]>([]);
  private readonly searchedFor = signal('');
  readonly noResults = computed(() => this.needle().length >= 3 && this.searchedFor() === this.needle() && !this.results().length);
  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      const q = this.needle();
      clearTimeout(this.timer);
      if (q.length < 3) { this.results.set([]); this.searchedFor.set(''); return; }
      this.timer = setTimeout(() => {
        this.api.searchCars(q).then(r => {
          if (this.needle() !== q) return;
          this.results.set(r);
          this.searchedFor.set(q);
        }).catch(() => {});
      }, 250);
    });
  }

  matches(vin: string) {
    const q = this.needle();
    return q.length >= 3 && vin.includes(q);
  }

  clear() { this.query.set(''); }
}
```

`frontend/src/app/core/lightbox.ts`:
```ts
import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Lightbox {
  readonly current = signal<{ src: string; caption: string } | null>(null);
  open(src: string, caption: string) { this.current.set({ src, caption }); }
  close() { this.current.set(null); }
}
```

`frontend/src/app/core/mobile-bar.ts`:
```ts
import { Injectable, signal } from '@angular/core';

/** Sticky action bar above the mobile nav (prototype lines 483–496). Pages set it; the shell renders it. */
export interface MobileBar {
  kicker: string;
  title: string;
  kind: 'btn' | 'photo' | 'text';
  label?: string;
  action?: () => void;
  onFile?: (file: File) => void;
}

@Injectable({ providedIn: 'root' })
export class MobileBarStore {
  readonly bar = signal<MobileBar | null>(null);
}
```

`frontend/src/app/core/poll.ts`:
```ts
import { DestroyRef, inject } from '@angular/core';

/** Re-run fn every ms while the calling component lives. Call from a constructor. */
export function pollEvery(ms: number, fn: () => void) {
  const id = setInterval(fn, ms);
  inject(DestroyRef).onDestroy(() => clearInterval(id));
}
```

`frontend/src/app/core/move-store.ts`:
```ts
import { Injectable, computed, inject, signal } from '@angular/core';
import { compressImage } from '../shared/image';
import { Api } from './api';
import { errorCode, errorText } from './errors';
import { Car, Move, Place } from './models';
import { Notify } from './notify';

export type Phase = 'idle' | 'selected' | 'moving' | 'done';

/**
 * Move workflow state shared by the header search, the map, the key box and the move panel.
 * "selected" lives only here; "moving" is the server's InTransit move; "done" shows the last result.
 */
@Injectable({ providedIn: 'root' })
export class MoveStore {
  private readonly api = inject(Api);
  private readonly notify = inject(Notify);

  readonly selected = signal<Car | null>(null);
  readonly active = signal<Move | null>(null);
  readonly result = signal<Move | null>(null);
  readonly busy = signal(false);
  readonly originTaken = signal(false);
  /** Bumped after every server call so pages reload what they show. */
  readonly version = signal(0);

  readonly phase = computed<Phase>(() =>
    this.active() ? 'moving' : this.result() ? 'done' : this.selected() ? 'selected' : 'idle');
  readonly car = computed(() => this.active()?.car ?? this.selected() ?? this.result()?.car ?? null);
  readonly from = computed<Place | null>(() => {
    const m = this.active() ?? this.result();
    if (m) return m.from;
    const s = this.selected();
    return s?.zoneCode ? { zoneCode: s.zoneCode, zoneName: s.zoneName ?? '', spotCode: s.spotCode } : null;
  });
  readonly to = computed<Place | null>(() => (this.active() ?? this.result())?.to ?? null);

  async resume() {
    try { this.active.set(await this.api.activeMove()); } catch { /* offline banner covers it */ }
  }

  select(car: Car): boolean {
    if (this.active()) { this.notify.show('Спершу завершіть або скасуйте поточну операцію.'); return false; }
    if (!car.zoneCode) { this.notify.show('Авто зараз у дорозі.'); return false; }
    this.result.set(null);
    this.selected.set(car);
    return true;
  }

  reset() {
    this.selected.set(null);
    this.result.set(null);
    this.originTaken.set(false);
  }

  async start() {
    const car = this.selected();
    if (!car) return;
    const { value } = await this.run(() => this.api.startMove(car.id));
    if (value) { this.active.set(value); this.selected.set(null); }
  }

  /** Returns an error message for inline display, or null on success. */
  async setTarget(zoneCode: string | null, spotCode: string | null = null, quiet = false): Promise<string | null> {
    const a = this.active();
    if (!a) return null;
    const { value, error } = await this.run(() => this.api.setTarget(a.id, zoneCode, spotCode), quiet);
    if (value) this.active.set(value);
    return error ? errorText(error) : null;
  }

  async photo(file: File) {
    const a = this.active();
    if (!a) return;
    const { value } = await this.run(async () => this.api.uploadPhoto(a.id, await compressImage(file)));
    if (value) this.active.set(value);
  }

  async finish() {
    const a = this.active();
    if (!a) return;
    const { value } = await this.run(() => this.api.completeMove(a.id));
    if (value) { this.active.set(null); this.result.set(value); }
  }

  /** Returns an error message, or null on success. OriginTaken switches the panel to "choose a return spot". */
  async cancel(fallbackSpot: string | null = null): Promise<string | null> {
    const a = this.active();
    if (!a) return null;
    const { value, error } = await this.run(() => this.api.cancelMove(a.id, fallbackSpot), true);
    if (value) { this.active.set(null); this.reset(); return null; }
    if (errorCode(error) === 'OriginTaken') this.originTaken.set(true);
    else if (!fallbackSpot) this.notify.error(error);
    return errorText(error);
  }

  private async run<T>(fn: () => Promise<T>, quiet = false): Promise<{ value?: T; error?: unknown }> {
    this.busy.set(true);
    try {
      return { value: await fn() };
    } catch (error) {
      if (!quiet) this.notify.error(error);
      // The move changed elsewhere (finished, cancelled, other device): resync.
      if (['Conflict', 'NotFound', 'NotYourMove'].includes(errorCode(error) ?? '')) void this.resume();
      return { error };
    } finally {
      this.busy.set(false);
      this.version.update(v => v + 1);
    }
  }
}
```

`frontend/src/app/app.config.ts`:
```ts
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { connectionInterceptor } from './core/connection';
import { operatorInterceptor } from './core/operator';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([operatorInterceptor, connectionInterceptor])),
  ],
};
```

- [ ] **Step 7: Build to verify everything compiles**

Run: `cd frontend && npx ng build && npx ng test --watch=false`
Expected: the build succeeds (warnings about unused files are fine) and the tests pass.

- [ ] **Step 8: Commit**

```bash
git add frontend
git commit -m "feat(web): Angular scaffold, design styles, API client and core stores" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: App shell: header, tabs, VIN search, clock, banners, lightbox, mobile bar and nav

**Files:**
- Modify: `frontend/src/app/app.ts`, `app.html`, `app.css`, `app.routes.ts`
- Delete: `frontend/src/app/app.spec.ts` (the CLI's title test no longer applies)
- Create (stubs that Tasks 8–12 replace): `frontend/src/app/features/zones/zones-page.ts`, `features/move/move-page.ts`, `features/keys/keys-page.ts`, `features/log/log-page.ts`

**Interfaces:**
- Consumes: all of `core/` and `shared/format.ts` (Task 6).
- Produces:
  - Routes: `/zones`, `/move/:zone` (and `/move`, which redirects to the last zone or `S1`), `/keys/:cabinet?slot=n` (and `/keys`, which redirects to `/keys/1`), `/log`.
  - Page components `ZonesPage`, `MovePage` (`zone` input), `KeysPage` (`cabinet` input, optional `slot` input) and `LogPage`.
  - Every page renders a root `<main>`, which the offline CSS rule relies on.
  - The `eurocar.lastZone` localStorage key is written by `MovePage`.

- [ ] **Step 1: Routes and page stubs**

`frontend/src/app/app.routes.ts`:
```ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'zones' },
  { path: 'zones', loadComponent: () => import('./features/zones/zones-page').then(m => m.ZonesPage) },
  { path: 'move', pathMatch: 'full', redirectTo: () => 'move/' + (localStorage.getItem('eurocar.lastZone') ?? 'S1') },
  { path: 'move/:zone', loadComponent: () => import('./features/move/move-page').then(m => m.MovePage) },
  { path: 'keys', pathMatch: 'full', redirectTo: 'keys/1' },
  { path: 'keys/:cabinet', loadComponent: () => import('./features/keys/keys-page').then(m => m.KeysPage) },
  { path: 'log', loadComponent: () => import('./features/log/log-page').then(m => m.LogPage) },
  { path: '**', redirectTo: 'zones' },
];
```

`frontend/src/app/features/zones/zones-page.ts`:
```ts
import { Component } from '@angular/core';

@Component({ selector: 'app-zones-page', template: `<main style="padding:var(--space-6)"><div class="title-xl">Зони зберігання</div></main>` })
export class ZonesPage {}
```

`frontend/src/app/features/move/move-page.ts`:
```ts
import { Component, input } from '@angular/core';

@Component({ selector: 'app-move-page', template: `<main style="padding:var(--space-6)"><div class="title-lg">Зона {{ zone() }}</div></main>` })
export class MovePage {
  readonly zone = input.required<string>();
}
```

`frontend/src/app/features/keys/keys-page.ts`:
```ts
import { Component, input } from '@angular/core';

@Component({ selector: 'app-keys-page', template: `<main style="padding:var(--space-6)"><div class="title-lg">Шафа {{ cabinet() }}</div></main>` })
export class KeysPage {
  readonly cabinet = input.required<string>();
  readonly slot = input<string>();
}
```

`frontend/src/app/features/log/log-page.ts`:
```ts
import { Component } from '@angular/core';

@Component({ selector: 'app-log-page', template: `<main style="padding:var(--space-6)"><div class="title-xl">Журнал операцій</div></main>` })
export class LogPage {}
```

- [ ] **Step 2: Shell component**

`frontend/src/app/app.ts`:
```ts
import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Clock } from './core/clock';
import { Connection } from './core/connection';
import { Lightbox } from './core/lightbox';
import { MobileBar, MobileBarStore } from './core/mobile-bar';
import { Car } from './core/models';
import { MoveStore } from './core/move-store';
import { Notify } from './core/notify';
import { OperatorStore } from './core/operator';
import { SearchStore } from './core/search';
import { SettingsStore } from './core/settings';
import { Viewport } from './core/viewport';
import { hms, locLabel, val } from './shared/format';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
  host: { '(window:keydown.escape)': 'lightbox.close()' },
})
export class App {
  protected readonly clock = inject(Clock);
  protected readonly vp = inject(Viewport);
  protected readonly search = inject(SearchStore);
  protected readonly notify = inject(Notify);
  protected readonly conn = inject(Connection);
  protected readonly lightbox = inject(Lightbox);
  protected readonly mobileBar = inject(MobileBarStore);
  private readonly moves = inject(MoveStore);
  private readonly router = inject(Router);

  protected readonly tabs = [
    { path: '/zones', label: 'Зони' },
    { path: '/move', label: 'Переміщення' },
    { path: '/keys', label: 'Ключі' },
    { path: '/log', label: 'Журнал' },
  ];
  protected readonly hms = hms;
  protected readonly val = val;
  protected readonly padB = computed(() => (this.vp.mob() ? (this.mobileBar.bar() ? 132 : 64) : 0));

  constructor() {
    const operators = inject(OperatorStore);
    inject(SettingsStore).load();
    operators.load().then(() => this.moves.resume()).catch(() => {});
  }

  protected sub(c: Car) {
    return 'Škoda ' + c.model + ' · ' + (c.zoneCode ? locLabel(c) + ' · ' + c.zoneName : 'у дорозі');
  }

  /** Search pick: on the key box open the key cell, elsewhere open the car on its zone map (prototype pick()/goCar()). */
  protected pick(c: Car) {
    this.search.clear();
    if (this.router.url.startsWith('/keys') && c.keyCabinet) {
      this.router.navigate(['/keys', c.keyCabinet], { queryParams: { slot: c.keyNumber } });
      return;
    }
    if (!c.zoneCode) { this.notify.show('Авто зараз у дорозі.'); return; }
    if (!this.moves.active()) this.moves.select(c);
    this.router.navigate(['/move', c.zoneCode]);
  }

  protected onBarFile(e: Event, bar: MobileBar) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) bar.onFile?.(file);
  }
}
```

`frontend/src/app/app.html`:
```html
<div class="app" [class.offline]="!conn.online()" [style.padding-bottom.px]="padB()">
  <header class="hdr">
    <div class="brand">
      <div class="brand-name">Євро Лоджистікс</div>
      <div class="brand-sub">Логістика авто · Соломоново</div>
    </div>
    @if (!vp.mob()) {
      <nav class="tabs">
        @for (t of tabs; track t.path) {
          <a class="tab" [routerLink]="t.path" routerLinkActive="active">{{ t.label }}</a>
        }
      </nav>
    }
    <div style="flex:1"></div>
    <div class="search">
      <input class="input input-lg" placeholder="Пошук за VIN, напр. 012345" [value]="search.query()" (input)="search.query.set(val($event))">
      @if (search.results().length) {
        <div class="search-pop">
          @for (r of search.results(); track r.id) {
            <div role="button" tabindex="0" class="search-item" (click)="pick(r)" (keydown.enter)="pick(r)">
              <span class="vin">{{ r.vin }}</span>
              <span class="sub">{{ sub(r) }}</span>
            </div>
          }
        </div>
      } @else if (search.noResults()) {
        <div class="search-pop search-empty">Авто з таким VIN не знайдено.</div>
      }
    </div>
    @if (vp.wide()) {
      <div class="clock tnum">{{ hms(clock.now()) }}</div>
    }
  </header>

  @if (!conn.online()) {
    <div class="offline-bar">Немає з’єднання з сервером. Дії тимчасово недоступні — дані оновляться автоматично.</div>
  }

  <router-outlet />

  @if (notify.message(); as m) {
    <div class="toast" [class.mob]="vp.mob()" role="status">{{ m }}</div>
  }

  @if (lightbox.current(); as lb) {
    <div class="dialog-backdrop" (click)="lightbox.close()" style="z-index:60;cursor:zoom-out;background:color-mix(in srgb, var(--color-neutral-900) 88%, transparent)">
      <div style="display:flex;flex-direction:column;gap:var(--space-3);align-items:flex-start;max-width:min(1000px,100%)">
        <img [src]="lb.src" alt="Фото постановки авто" style="max-width:100%;max-height:78vh;object-fit:contain;display:block;border:2px solid var(--color-bg)">
        <div style="display:flex;gap:var(--space-4);align-items:center;width:100%;color:#fff;flex-wrap:wrap">
          <div style="flex:1 1 auto;font-size:15px;font-weight:700">{{ lb.caption }}</div>
          <button class="btn btn-secondary" (click)="lightbox.close()" style="color:#fff;border-color:#fff;min-height:44px">Закрити ✕</button>
        </div>
      </div>
    </div>
  }

  @if (vp.mob() && mobileBar.bar(); as b) {
    <div class="mob-bar">
      <div style="flex:1;min-width:0">
        <div class="mob-kicker">{{ b.kicker }}</div>
        <div class="mob-title">{{ b.title }}</div>
      </div>
      @if (b.kind === 'photo') {
        <label class="btn btn-primary" style="position:relative;min-height:48px;cursor:pointer;color:#fff">Зробити фото<input class="file-hidden" type="file" accept="image/*" capture="environment" (change)="onBarFile($event, b)"></label>
      } @else if (b.kind === 'btn') {
        <button class="btn btn-primary" style="min-height:48px" (click)="b.action?.()">{{ b.label }}</button>
      }
    </div>
  }

  @if (vp.mob()) {
    <nav class="mob-nav">
      @for (t of tabs; track t.path) {
        <a class="tab" [routerLink]="t.path" routerLinkActive="active">{{ t.label }}</a>
      }
    </nav>
  }
</div>
```

`frontend/src/app/app.css`:
```css
.app { min-height: 100vh; display: flex; flex-direction: column; box-sizing: border-box; }
.hdr { display: flex; align-items: stretch; gap: var(--space-6); padding: 0 var(--space-6); background: var(--color-accent); color: #fff; border-bottom: 2px solid var(--color-text); flex-wrap: wrap; }
.brand { display: flex; flex-direction: column; justify-content: center; padding: var(--space-3) 0; }
.brand-name { font-weight: 800; font-size: 18px; letter-spacing: -0.01em; }
.brand-sub { font-size: 12px; color: var(--color-accent-2); }
.tabs { display: flex; gap: var(--space-2); }
.tab { cursor: pointer; display: flex; align-items: center; padding: 0 var(--space-3); font-size: 15px; font-weight: 700; color: #fff; text-decoration: none; border-bottom: 4px solid transparent; border-top: 4px solid transparent; }
.tab:hover { background: var(--color-accent-600); color: #fff; }
.tab.active { border-bottom-color: var(--color-accent-2); }
.search { position: relative; align-self: center; flex: 0 1 240px; min-width: 200px; padding: var(--space-2) 0; }
.search .input { width: 100%; background: var(--color-bg); }
.search-pop { position: absolute; top: 100%; right: 0; width: 400px; max-width: 90vw; background: var(--color-bg); color: var(--color-text); border: 2px solid var(--color-text); box-shadow: var(--shadow-md); z-index: 30; display: flex; flex-direction: column; }
.search-empty { width: 300px; padding: var(--space-3); font-size: 14px; }
.search-item { cursor: pointer; padding: var(--space-2) var(--space-3); border-bottom: 1px solid var(--color-divider); display: flex; flex-direction: column; gap: 2px; }
.search-item:hover { background: var(--color-surface); }
.search-item .vin { font-weight: 700; letter-spacing: 0.04em; }
.search-item .sub { font-size: 13px; color: var(--color-neutral-700); }
.clock { align-self: center; font-weight: 600; font-size: 14px; }
.offline-bar { background: var(--color-accent-2-100); color: var(--color-accent-2-900); padding: var(--space-2) var(--space-6); font-size: 14px; font-weight: 600; border-bottom: 2px solid var(--color-text); }
.toast { position: fixed; left: 50%; transform: translateX(-50%); bottom: 24px; z-index: 70; background: var(--color-text); color: #fff; padding: var(--space-3) var(--space-4); font-size: 14px; max-width: min(520px, 92vw); box-shadow: var(--shadow-lg); }
.toast.mob { bottom: 140px; }
.mob-bar { position: fixed; left: 0; right: 0; bottom: 64px; z-index: 40; background: var(--color-bg); border-top: 2px solid var(--color-text); padding: var(--space-2) var(--space-4); display: flex; gap: var(--space-3); align-items: center; box-shadow: var(--shadow-md); }
.mob-kicker { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: var(--color-accent-500); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.mob-title { font-size: 16px; font-weight: 800; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.mob-nav { position: fixed; left: 0; right: 0; bottom: 0; height: 64px; z-index: 41; background: var(--color-accent); display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); border-top: 2px solid var(--color-text); }
.mob-nav .tab { font-size: 13px; border-bottom: 0; border-top: 4px solid transparent; }
.mob-nav .tab.active { border-top-color: var(--color-accent-2); }
```

Run: `rm frontend/src/app/app.spec.ts`

- [ ] **Step 3: Build and check in the browser**

Run: `cd frontend && npx ng build`. Expected: success.

Then start both apps: in one terminal `cd backend/Eurocar.Api && dotnet run`, in another `cd frontend && npm start`. Open http://localhost:4200 and check:
1. The dark green header shows «Євро Лоджистікс» and four tabs. The active tab has a mint underline.
2. The clock ticks when the window is at least 1100 px wide.
3. Typing `tmb` in the search shows up to 8 VINs. Typing `zzzz` shows «Авто з таким VIN не знайдено.»
4. At 390 px wide (devtools device mode), the tabs move to the bottom bar.
5. Stopping the API and waiting about 10 s (or triggering a search) shows the offline banner.

- [ ] **Step 4: Commit**

```bash
git add -A frontend
git commit -m "feat(web): app shell with tabs, VIN search, clock, banners and mobile nav" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: «Зони» page and zone dialog

**Files:**
- Modify (replace stub): `frontend/src/app/features/zones/zones-page.ts`
- Create: `frontend/src/app/features/zones/zones-page.html`, `features/zones/zone-dialog.ts`, `features/zones/zone-dialog.html`

**Interfaces:**
- Consumes:
  - `Api.zones/createZone/updateZone/deleteZone/resetDemo`, `Clock`, `Notify`, `pollEvery`, `errorText`.
  - From `format.ts`: `ago, zonePlural, PRI_LABEL, PRI_CLASS, val`.
- Produces:
  - `ZoneDialog`: `state` input (`DialogState`), `saved` and `closed` outputs.
  - `DialogState { mode: 'create'|'edit'; code?: string; groupCode: string; name: string; areaM2: number; priority: Priority; used: number }`.

- [ ] **Step 1: Zone dialog**

`frontend/src/app/features/zones/zone-dialog.ts`:
```ts
import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { Api } from '../../core/api';
import { errorText } from '../../core/errors';
import { Priority } from '../../core/models';
import { val } from '../../shared/format';

export interface DialogState {
  mode: 'create' | 'edit';
  code?: string;
  groupCode: string;
  name: string;
  areaM2: number;
  priority: Priority;
  used: number;
}

/** Create/edit a temporary (3.2) or transit (3.3) zone. Prototype lines 428–469, 711–735, 874–883. */
@Component({ selector: 'app-zone-dialog', templateUrl: './zone-dialog.html' })
export class ZoneDialog implements OnInit {
  readonly state = input.required<DialogState>();
  readonly saved = output<void>();
  readonly closed = output<void>();
  private readonly api = inject(Api);

  protected readonly name = signal('');
  protected readonly area = signal(1000);
  protected readonly priority = signal<Priority>('Normal');
  protected readonly groupCode = signal('3.2');
  protected readonly serverError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly val = val;
  protected readonly pris: { value: Priority; label: string }[] = [
    { value: 'High', label: 'Високий' },
    { value: 'Normal', label: 'Звичайний' },
    { value: 'Low', label: 'Низький' },
  ];

  protected readonly cap = computed(() => Math.max(0, Math.min(300, Math.floor((+this.area() || 0) / 25))));
  protected readonly error = computed(() => {
    const cap = this.cap(), used = this.state().used;
    if (!this.name().trim()) return 'name';
    if (cap < 1) return 'Площа має бути не меншою за 25 м².';
    if (cap < used) return `На майданчику ${used} авто, місткість не може бути меншою.`;
    return '';
  });
  protected readonly shownError = computed(() => this.serverError() ?? (this.error() && this.error() !== 'name' ? this.error() : null));
  protected readonly title = computed(() => (this.state().mode === 'create' ? 'Нова зона' : 'Змінити ' + this.state().code));
  protected readonly canDelete = computed(() => this.state().mode === 'edit' && this.state().used === 0);

  ngOnInit() {
    const s = this.state();
    this.name.set(s.name);
    this.area.set(s.areaM2);
    this.priority.set(s.priority);
    this.groupCode.set(s.groupCode);
  }

  protected async save() {
    if (this.error()) return;
    this.saving.set(true);
    this.serverError.set(null);
    const d = { groupCode: this.groupCode(), name: this.name().trim(), areaM2: +this.area(), priority: this.priority() };
    try {
      const s = this.state();
      if (s.mode === 'create') await this.api.createZone(d);
      else await this.api.updateZone(s.code!, d);
      this.saved.emit();
    } catch (e) {
      this.serverError.set(errorText(e));
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove() {
    try {
      await this.api.deleteZone(this.state().code!);
      this.saved.emit();
    } catch (e) {
      this.serverError.set(errorText(e));
    }
  }
}
```

`frontend/src/app/features/zones/zone-dialog.html`:
```html
<div class="dialog-backdrop" style="z-index:50">
  <div class="dialog" style="width:min(480px,100%);background:var(--color-bg);border:2px solid var(--color-text)">
    <div class="dialog-title">{{ title() }}</div>
    @if (state().mode === 'create') {
      <div class="field" style="margin:0">
        <label>Тип</label>
        <div class="seg">
          <label class="seg-opt"><input type="radio" name="dlg-type" [checked]="groupCode() === '3.2'" (change)="groupCode.set('3.2')">Тимчасовий майданчик</label>
          <label class="seg-opt"><input type="radio" name="dlg-type" [checked]="groupCode() === '3.3'" (change)="groupCode.set('3.3')">Транзитна зона</label>
        </div>
      </div>
    }
    <div class="field" style="margin:0">
      <label>Назва</label>
      <input class="input" [value]="name()" (input)="name.set(val($event))" placeholder="Поле за терміналом" style="width:100%;box-sizing:border-box">
    </div>
    <div class="field" style="margin:0">
      <label>Площа, м²</label>
      <input class="input" type="number" min="25" step="25" [value]="area()" (input)="area.set(+val($event))" style="width:100%;box-sizing:border-box">
    </div>
    <div class="field" style="margin:0">
      <label>Пріоритет</label>
      <div class="seg">
        @for (p of pris; track p.value) {
          <label class="seg-opt"><input type="radio" name="dlg-pri" [checked]="priority() === p.value" (change)="priority.set(p.value)">{{ p.label }}</label>
        }
      </div>
    </div>
    <div style="font-size:15px;border-top:2px solid var(--color-text);padding-top:var(--space-3)">
      Місткість: <b>{{ cap() }}</b> місць <span style="color:var(--color-neutral-600);font-size:13px">· 25 м² на авто, не більше 300 місць</span>
    </div>
    @if (shownError(); as e) {
      <div style="font-size:14px;background:var(--color-accent-2-100);color:var(--color-accent-2-900);padding:var(--space-2) var(--space-3)">{{ e }}</div>
    }
    <div class="dialog-actions" style="justify-content:flex-start">
      <button class="btn btn-primary" (click)="save()" [disabled]="!!error() || saving()">Зберегти</button>
      <button class="btn btn-secondary" (click)="closed.emit()">Скасувати</button>
      <div style="flex:1"></div>
      @if (canDelete()) {
        <button class="btn btn-ghost" (click)="remove()">Видалити зону</button>
      }
    </div>
  </div>
</div>
```

- [ ] **Step 2: Zones page**

`frontend/src/app/features/zones/zones-page.ts`:
```ts
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Clock } from '../../core/clock';
import { ZoneGroup, ZoneRow, ZonesOverview } from '../../core/models';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { PRI_CLASS, PRI_LABEL, ago, zonePlural } from '../../shared/format';
import { DialogState, ZoneDialog } from './zone-dialog';

/** Prototype lines 52–89 (markup) and 752–767 (values). */
@Component({ selector: 'app-zones-page', imports: [ZoneDialog], templateUrl: './zones-page.html' })
export class ZonesPage {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notify = inject(Notify);
  private readonly clock = inject(Clock);

  protected readonly data = signal<ZonesOverview | null>(null);
  protected readonly dialog = signal<DialogState | null>(null);
  protected readonly PRI_LABEL = PRI_LABEL;
  protected readonly PRI_CLASS = PRI_CLASS;

  constructor() {
    this.load();
    pollEvery(10_000, () => this.load());
  }

  private async load() {
    try { this.data.set(await this.api.zones()); } catch (e) { if (!this.data()) this.notify.error(e); }
  }

  protected summary(g: ZoneGroup) {
    const used = g.zones.reduce((a, z) => a + z.used, 0), cap = g.zones.reduce((a, z) => a + z.capacity, 0);
    return `${g.zones.length} ${zonePlural(g.zones.length)} · ${used} / ${cap}`;
  }
  protected pct(z: ZoneRow) { return Math.min(100, Math.round((z.used / z.capacity) * 100)); }
  protected free(z: ZoneRow) { return Math.max(0, z.capacity - z.used); }
  protected info(z: ZoneRow) {
    if (z.kind === 'External') {
      const when = z.lastSyncedAt ? ago(this.clock.now() - Date.parse(z.lastSyncedAt)) : '—';
      return (z.isOnline ? '● онлайн · ' : '● немає зв’язку · ') + when;
    }
    return z.areaM2 ? z.areaM2.toLocaleString('uk-UA') + ' м²' : 'карта місць';
  }
  protected infoColor(z: ZoneRow) {
    return z.kind === 'External' ? (z.isOnline ? 'var(--color-accent-2-800)' : 'var(--color-neutral-500)') : 'var(--color-neutral-700)';
  }

  protected open(z: ZoneRow) { this.router.navigate(['/move', z.code]); }
  protected add(g: ZoneGroup) {
    this.dialog.set({ mode: 'create', groupCode: g.code, name: '', areaM2: 1000, priority: 'Normal', used: 0 });
  }
  protected edit(z: ZoneRow) {
    this.dialog.set({ mode: 'edit', code: z.code, groupCode: z.groupCode, name: z.name, areaM2: z.areaM2 ?? z.capacity * 25, priority: z.priority, used: z.used });
  }
  protected async onSaved() {
    this.dialog.set(null);
    await this.load();
  }
  protected async resetDemo() {
    try {
      await this.api.resetDemo();
      location.reload();
    } catch (e) {
      this.notify.error(e);
    }
  }
}
```

`frontend/src/app/features/zones/zones-page.html`:
```html
<main style="padding:var(--space-6);display:flex;flex-direction:column;gap:var(--space-8);box-sizing:border-box;width:100%;max-width:1440px">
  <div>
    <div class="kicker">Усі зони</div>
    <div class="title-xl">Зони зберігання</div>
  </div>

  @if (data(); as d) {
    <div class="stats" style="grid-template-columns:repeat(auto-fit,minmax(200px,1fr))">
      <div style="padding:var(--space-4) var(--space-4) var(--space-4) 0"><div class="stat-n">{{ d.totals.ownUsed }} / {{ d.totals.ownCap }}</div><div style="font-size:14px">власні зони · вільно {{ d.totals.ownCap - d.totals.ownUsed }}</div></div>
      <div style="padding:var(--space-4)"><div class="stat-n">{{ d.totals.extUsed }} / {{ d.totals.extCap }}</div><div style="font-size:14px">партнери й дилери</div></div>
      <div style="padding:var(--space-4)"><div class="stat-n">{{ d.totals.online }} / {{ d.totals.extCount }}</div><div style="font-size:14px">зовнішніх складів на зв’язку</div></div>
      <div style="padding:var(--space-4)"><div class="stat-n">{{ d.totals.ops }}</div><div style="font-size:14px">переміщень у журналі</div></div>
    </div>

    @for (g of d.groups; track g.code) {
      <section style="display:flex;flex-direction:column;gap:var(--space-2)">
        <div style="display:flex;align-items:baseline;gap:var(--space-4);border-bottom:2px solid var(--color-text);padding-bottom:var(--space-2);flex-wrap:wrap">
          <span style="font-size:20px;font-weight:800;color:var(--color-accent-500)">{{ g.code }}</span>
          <span style="font-size:20px;font-weight:800;flex:1 1 auto">{{ g.name }}</span>
          <span class="tnum" style="font-size:14px">{{ summary(g) }}</span>
          @if (g.editable) {
            <button class="btn btn-secondary" (click)="add(g)">+ Додати</button>
          }
        </div>
        <div style="display:flex;flex-direction:column">
          @for (z of g.zones; track z.code) {
            <div class="row-line" style="gap:var(--space-2) var(--space-4);padding:var(--space-3) 0">
              <div style="flex:1 1 200px;min-width:0;display:flex;gap:var(--space-3);align-items:baseline"><b style="min-width:36px">{{ z.code }}</b><span>{{ z.name }}</span></div>
              <div style="flex:0 0 auto"><span [class]="'tag ' + PRI_CLASS[z.priority]">{{ PRI_LABEL[z.priority] }}</span></div>
              <div style="flex:1 1 180px;display:flex;gap:var(--space-3);align-items:center">
                <div style="flex:1;height:8px;background:var(--color-neutral-200)"><div style="height:100%;background:var(--color-accent)" [style.width.%]="pct(z)"></div></div>
                <span class="tnum" style="font-size:13px;white-space:nowrap">{{ z.used }} / {{ z.capacity }} · вільн. <b>{{ free(z) }}</b></span>
              </div>
              <div style="flex:0 1 150px;font-size:13px" [style.color]="infoColor(z)">{{ info(z) }}</div>
              <div style="display:flex;gap:var(--space-2);margin-left:auto;flex-shrink:0">
                @if (z.editable) {
                  <button class="btn btn-ghost" style="min-height:44px;white-space:nowrap" (click)="edit(z)">Змінити</button>
                }
                <button class="btn btn-secondary" style="min-height:44px;white-space:nowrap" (click)="open(z)">Відкрити →</button>
              </div>
            </div>
          }
        </div>
      </section>
    }

    <div><button class="btn btn-ghost" (click)="resetDemo()">Скинути демо-дані</button></div>
  } @else {
    <div style="font-size:15px">Завантаження…</div>
  }
</main>

@if (dialog(); as s) {
  <app-zone-dialog [state]="s" (saved)="onSaved()" (closed)="dialog.set(null)" />
}
```

- [ ] **Step 3: Build and check in the browser**

Run: `cd frontend && npx ng build`. Expected: success.

With both apps running, open `/zones` and check against `docs/design/Логистика.dc.html` (open it in a browser next to the app):
1. The totals strip shows 4 numbers, followed by 8 groups 3.1–3.8 with progress bars.
2. P07 shows «● немає зв’язку · 47 хв тому» in grey. Other external zones show mint «● онлайн · …» and refresh within 10 s.
3. «+ Додати» in group 3.2 → name «Поле за терміналом», area 1000 → «Місткість: 40 місць» → Зберегти. Zone T3 appears.
4. «Змінити» on T1 with area 25 shows «На майданчику N авто, місткість не може бути меншою.», and Зберегти is disabled.
5. «Видалити зону» shows only on the empty T3, and it removes the zone.
6. «Відкрити →» goes to `/move/S1` (still the stub).

- [ ] **Step 4: Commit**

```bash
git add frontend
git commit -m "feat(web): zones overview page and zone create/edit dialog" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 9: «Переміщення» page: zone map (aerial and scheme), route, external stock

**Files:**
- Create: `frontend/src/app/features/move/view-models.ts`
- Create: `frontend/src/app/features/move/zone-map-aerial.ts`, `zone-map-aerial.html`
- Create: `frontend/src/app/features/move/zone-map-scheme.ts` (inline template)
- Modify (replace stub): `frontend/src/app/features/move/move-page.ts`
- Create: `frontend/src/app/features/move/move-page.html`

**Interfaces:**
- Consumes:
  - `Api.zone/zones`, `MoveStore`, `SearchStore`, `SettingsStore`, `Viewport`, `Clock`, `Notify`, `pollEvery`.
  - From `format.ts`: `CAR_COLOR, ago, locLabel, vinTail`.
  - From `zone-layout.ts`: `canvasSize, geom, pathBetween, pathFromGate, pathLength, spotBox, ROAD, Pt`.
- Produces:
  - `SpotVm`, `RouteVm` and `DestChip` (in `view-models.ts`).
  - `ZoneMapAerial`: inputs `rows, perRow, strip, spots, route, focusSpot, mob`; output `spotClick`.
  - `ZoneMapScheme`: inputs `perRow, spots`; output `spotClick`.
  - `MovePage` keeps its `zone` input and adds the protected members `detail, overview, mapped, moving, spots, route, focusSpot, hint, setDest(code)` that Task 10 uses.

- [ ] **Step 1: View models**

`frontend/src/app/features/move/view-models.ts`:
```ts
import { Car } from '../../core/models';

/** One parking spot as drawn on both map views (prototype lines 779–794). */
export interface SpotVm {
  code: string;
  car: Car | null;
  reserved: boolean;
  carBg: string;
  spotBg: string;
  ring: string;
  tag: string;
  fg: string;
  sBg: string;
  sFg: string;
  sRing: string;
  sLabel: string;
  clickable: boolean;
  title: string;
}

/** Route card + polyline (prototype lines 798–823). Coordinates are % of the map. */
export interface RouteVm {
  title: string;
  meta: string;
  canDraw: boolean;
  draw: boolean;
  points: string;
  sx: number;
  sy: number;
  ex: number;
  ey: number;
  startLabel: string;
  gmaps: string;
}

export interface DestChip { code: string; name: string; free: number; on: boolean; }
```

- [ ] **Step 2: Aerial and scheme map components**

`frontend/src/app/features/move/zone-map-aerial.ts`:
```ts
import { Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';
import { ROAD, canvasSize, geom, spotBox } from '../../shared/zone-layout';
import { RouteVm, SpotVm } from './view-models';

/** "Вид згори": aisles, gate strip, cars as rectangles, dashed route. Prototype lines 123–161. */
@Component({ selector: 'app-zone-map-aerial', templateUrl: './zone-map-aerial.html' })
export class ZoneMapAerial {
  readonly rows = input.required<number>();
  readonly perRow = input.required<number>();
  readonly strip = input.required<string>();
  readonly spots = input.required<SpotVm[]>();
  readonly route = input<RouteVm | null>(null);
  readonly focusSpot = input<string | null>(null);
  readonly mob = input(false);
  readonly spotClick = output<SpotVm>();

  private readonly box = viewChild<ElementRef<HTMLDivElement>>('box');
  private readonly size = computed(() => canvasSize(this.rows(), this.perRow()));

  protected readonly map = computed(() => {
    const { W, H } = this.size(), pairs = this.rows() / 2, per = this.perRow();
    return {
      minW: Math.round(W * (this.mob() ? 44 : 22)),
      aspect: `${W} / ${H}`,
      viewBox: `0 0 ${W} ${H}`,
      stripH: (2.2 / H) * 100,
      gateW: (1.6 / W) * 100,
      stripL: (1.6 / W) * 100 + 1,
      topDash: (ROAD / H) * 100,
      botDash: ((H - 1) / H) * 100,
      arrowT: ((4 + per / 2 - 0.6) / H) * 100,
      spotW: (2 / W) * 100,
      spotH: 100 / H,
      aisles: Array.from({ length: pairs + 1 }, (_, p) => ({ l: ((p * 5.6) / W) * 100, arrow: p % 2 ? '↑' : '↓' })),
    };
  });

  protected readonly placed = computed(() => {
    const { W, H } = this.size();
    return this.spots().map(s => {
      const b = spotBox(s.code);
      return { ...s, l: (b.x / W) * 100, t: (b.y / H) * 100, dir: b.flipped ? 'row-reverse' : 'row' };
    });
  });

  constructor() {
    // Scroll wide maps so the selected / target spot is centred (prototype focusMap()).
    effect(() => {
      const el = this.box()?.nativeElement, sid = this.focusSpot(), { W } = this.size();
      if (!el || !sid) return;
      requestAnimationFrame(() => { el.scrollLeft = Math.max(0, (geom(sid).cx / W) * el.scrollWidth - el.clientWidth / 2); });
    });
  }
}
```

`frontend/src/app/features/move/zone-map-aerial.html`:
```html
<div #box style="overflow-x:auto;border:2px solid var(--color-text)">
  <div style="position:relative;width:100%;background:var(--color-neutral-800);overflow:hidden" [style.min-width.px]="map().minW" [style.aspect-ratio]="map().aspect">
    <div style="position:absolute;left:0;top:0;right:0;background:var(--color-neutral-300);border-bottom:2px solid var(--color-neutral-100)" [style.height.%]="map().stripH"></div>
    <div style="position:absolute;left:0;top:0;background:var(--color-neutral-800);display:flex;align-items:center;justify-content:center;text-align:center;color:var(--color-neutral-100);font-size:8px;font-weight:800;line-height:1.2" [style.width.%]="map().gateW" [style.height.%]="map().stripH">В’ЇЗД ↓</div>
    <div style="position:absolute;top:0;display:flex;align-items:center;font-size:11px;font-weight:800;letter-spacing:0.12em;color:var(--color-neutral-800)" [style.left.%]="map().stripL" [style.height.%]="map().stripH">{{ strip() }}</div>
    <div style="position:absolute;border-top:2px dashed var(--color-neutral-500)" [style.left.%]="map().gateW" [style.right.%]="map().gateW" [style.top.%]="map().topDash"></div>
    <div style="position:absolute;border-top:2px dashed var(--color-neutral-500)" [style.left.%]="map().gateW" [style.right.%]="map().gateW" [style.top.%]="map().botDash"></div>
    @for (a of map().aisles; track $index) {
      <div style="position:absolute;text-align:center;color:var(--color-neutral-500);font-size:16px;font-weight:800" [style.left.%]="a.l" [style.top.%]="map().arrowT" [style.width.%]="map().gateW">{{ a.arrow }}</div>
    }
    @for (s of placed(); track s.code) {
      <button type="button" [title]="s.title" (click)="spotClick.emit(s)"
        style="all:unset;position:absolute;box-sizing:border-box;border-top:1px solid var(--color-neutral-300);border-bottom:1px solid var(--color-neutral-300);display:flex;align-items:center;justify-content:space-between;padding:0 3px"
        [style.left.%]="s.l" [style.top.%]="s.t" [style.width.%]="map().spotW" [style.height.%]="map().spotH"
        [style.background]="s.spotBg" [style.box-shadow]="s.ring" [style.cursor]="s.clickable ? 'pointer' : 'default'" [style.flex-direction]="s.dir">
        @if (s.car) {
          <div style="width:64%;flex-shrink:0;height:68%;outline:1px solid var(--color-neutral-900);display:flex;align-items:stretch" [style.background]="s.carBg" [style.flex-direction]="s.dir">
            <div style="flex:0 0 18%"></div>
            <div style="flex:0 0 14%;background:var(--color-neutral-900);opacity:0.7;margin:8% 0"></div>
            <div style="flex:1 1 auto"></div>
            <div style="flex:0 0 9%;background:var(--color-neutral-900);opacity:0.55;margin:12% 0"></div>
            <div style="flex:0 0 10%"></div>
          </div>
        } @else {
          <div style="width:64%;flex-shrink:0;font-size:8px;font-weight:800;letter-spacing:0.04em" [style.color]="s.fg">{{ s.tag }}</div>
        }
        <span style="font-size:8px;font-weight:700;color:var(--color-neutral-300)">{{ s.code }}</span>
      </button>
    }
    @if (route(); as r) {
      @if (r.draw) {
        <svg [attr.viewBox]="map().viewBox" preserveAspectRatio="none" style="position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none">
          <polyline [attr.points]="r.points" style="fill:none;stroke:var(--color-accent-2);stroke-width:4px;stroke-dasharray:10 6;stroke-linejoin:round;vector-effect:non-scaling-stroke"></polyline>
        </svg>
        <div style="position:absolute;transform:translate(-50%,-50%);width:12px;height:12px;background:var(--color-accent-2);border:2px solid var(--color-accent);pointer-events:none" [style.left.%]="r.sx" [style.top.%]="r.sy"></div>
        <div style="position:absolute;transform:translate(10px,-50%);background:var(--color-accent);color:#fff;font-size:10px;font-weight:800;padding:2px 6px;white-space:nowrap;pointer-events:none" [style.left.%]="r.sx" [style.top.%]="r.sy">{{ r.startLabel }}</div>
        <div style="position:absolute;transform:translate(-50%,-50%);width:18px;height:18px;border:3px solid var(--color-accent-2);pointer-events:none" [style.left.%]="r.ex" [style.top.%]="r.ey"></div>
      }
    }
  </div>
</div>
```

`frontend/src/app/features/move/zone-map-scheme.ts`:
```ts
import { Component, input, output } from '@angular/core';
import { SpotVm } from './view-models';

/** "Схема": plain grid of spots. Prototype lines 163–174. */
@Component({
  selector: 'app-zone-map-scheme',
  template: `
    <div style="overflow-x:auto">
      <div class="cells" [style.grid-template-columns]="'repeat(' + perRow() + ',minmax(52px,1fr))'">
        @for (c of spots(); track c.code) {
          <button type="button" class="cell" style="aspect-ratio:1/0.95;padding:5px" [title]="c.title" (click)="spotClick.emit(c)"
            [style.background]="c.sBg" [style.color]="c.sFg" [style.box-shadow]="c.sRing" [style.cursor]="c.clickable ? 'pointer' : 'default'">
            <span class="cell-n">{{ c.code }}</span>
            <span class="cell-l">{{ c.sLabel }}</span>
          </button>
        }
      </div>
    </div>`,
})
export class ZoneMapScheme {
  readonly perRow = input.required<number>();
  readonly spots = input.required<SpotVm[]>();
  readonly spotClick = output<SpotVm>();
}
```
(The scheme is small enough to keep inline. Do not create `zone-map-scheme.html`.)

- [ ] **Step 3: Move page (map side)**

`frontend/src/app/features/move/move-page.ts`:
```ts
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Clock } from '../../core/clock';
import { errorCode } from '../../core/errors';
import { Car, ZoneDetail, ZoneRow, ZonesOverview } from '../../core/models';
import { MoveStore } from '../../core/move-store';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { SearchStore } from '../../core/search';
import { SettingsStore } from '../../core/settings';
import { Viewport } from '../../core/viewport';
import { CAR_COLOR, ago, locLabel, vinTail } from '../../shared/format';
import { Pt, canvasSize, geom, pathBetween, pathFromGate, pathLength } from '../../shared/zone-layout';
import { RouteVm, SpotVm } from './view-models';
import { ZoneMapAerial } from './zone-map-aerial';
import { ZoneMapScheme } from './zone-map-scheme';

type View = 'aerial' | 'scheme';

/** Prototype lines 91–336 (markup) and 769–844 (values). */
@Component({ selector: 'app-move-page', imports: [ZoneMapAerial, ZoneMapScheme], templateUrl: './move-page.html' })
export class MovePage {
  readonly zone = input.required<string>();

  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notify = inject(Notify);
  protected readonly settings = inject(SettingsStore);
  protected readonly clock = inject(Clock);
  protected readonly store = inject(MoveStore);
  protected readonly search = inject(SearchStore);
  protected readonly vp = inject(Viewport);

  protected readonly detail = signal<ZoneDetail | null>(null);
  protected readonly overview = signal<ZonesOverview | null>(null);
  protected readonly view = signal<View>(localStorage.getItem('eurocar.view') === 'scheme' ? 'scheme' : 'aerial');
  protected readonly routeOn = signal(true);
  protected readonly locLabel = locLabel;

  constructor() {
    effect(() => {
      const code = this.zone();
      this.store.version(); // reload after every move action
      localStorage.setItem('eurocar.lastZone', code);
      untracked(() => {
        if (this.detail()?.code !== code) this.detail.set(null);
        this.load(code);
      });
    });
    pollEvery(10_000, () => this.load(this.zone()));
  }

  private async load(code: string) {
    try {
      const [d, o] = await Promise.all([this.api.zone(code), this.api.zones()]);
      if (code !== this.zone()) return;
      this.detail.set(d);
      this.overview.set(o);
    } catch (e) {
      if (errorCode(e) === 'NotFound') {
        this.notify.error(e);
        this.router.navigate(['/zones']);
      }
    }
  }

  protected readonly mapped = computed(() => this.detail()?.kind === 'Mapped');
  protected readonly moving = computed(() => this.store.phase() === 'moving');
  protected readonly zonesByCode = computed(
    () => new Map<string, ZoneRow>((this.overview()?.groups ?? []).flatMap(g => g.zones).map(z => [z.code, z])));
  private readonly size = computed(() => {
    const d = this.detail();
    return d?.rows && d.perRow ? canvasSize(d.rows, d.perRow) : { W: 1, H: 1 };
  });

  protected readonly spots = computed<SpotVm[]>(() => {
    const d = this.detail();
    if (!d || d.kind !== 'Mapped') return [];
    const phase = this.store.phase(), moving = phase === 'moving';
    const from = this.store.from(), to = this.store.to(), sel = this.store.car();
    const labelByModel = this.settings.value().cellLabelMode === 'Model';
    return d.spots.map(sp => {
      const c = sp.car;
      const isOrigin = moving && from?.zoneCode === d.code && from.spotCode === sp.code;
      const isTarget = moving && to?.zoneCode === d.code && to.spotCode === sp.code;
      const isSel = !!c && sel?.id === c.id && (phase === 'selected' || phase === 'done');
      const reserved = sp.reserved && !isTarget;
      const vm: SpotVm = {
        code: sp.code, car: c, reserved,
        carBg: c ? CAR_COLOR[c.color] ?? 'var(--color-neutral-400)' : '', spotBg: 'transparent', ring: 'none',
        tag: reserved ? 'РЕЗЕРВ' : '', fg: 'var(--color-neutral-400)',
        sBg: c ? 'var(--color-surface)' : 'var(--color-bg)', sFg: c ? 'var(--color-text)' : 'var(--color-neutral-500)', sRing: 'none',
        sLabel: c ? (labelByModel ? c.model : vinTail(c.vin)) : reserved ? 'резерв' : 'вільно',
        clickable: moving ? (!c && !reserved) || isOrigin : !!c,
        title: c ? `Škoda ${c.model} · VIN ${c.vin}` : `${d.code} · ${sp.code} — ${reserved ? 'зарезервовано' : 'вільно'}`,
      };
      if (isSel) Object.assign(vm, { carBg: 'var(--color-accent-2)', ring: 'inset 0 0 0 2px var(--color-accent-2)', sBg: 'var(--color-accent)', sFg: '#fff' });
      if (isOrigin) Object.assign(vm, { ring: 'inset 0 0 0 2px var(--color-accent-2-400)', tag: 'У ДОРОЗІ', fg: 'var(--color-accent-2-300)', sFg: 'var(--color-accent-2-800)', sRing: 'inset 0 0 0 2px var(--color-accent-2-600)', sLabel: 'у дорозі' });
      if (isTarget) Object.assign(vm, { spotBg: 'var(--color-accent-2-800)', ring: 'inset 0 0 0 2px var(--color-accent-2)', tag: 'СЮДИ', fg: 'var(--color-accent-2)', sBg: 'var(--color-accent-2-200)', sFg: 'var(--color-accent-2-900)', sRing: 'inset 0 0 0 2px var(--color-accent-2-700)', sLabel: '→ сюди' });
      if (c && this.search.matches(c.vin)) Object.assign(vm, { ring: 'inset 0 0 0 3px var(--color-neutral-100)', sRing: 'inset 0 0 0 3px var(--color-text)' });
      return vm;
    });
  });

  protected readonly route = computed<RouteVm | null>(() => {
    const phase = this.store.phase();
    if (phase !== 'selected' && phase !== 'moving') return null;
    const d = this.detail(), from = this.store.from();
    if (!d || !from) return null;
    const to = this.store.to(), walk = phase === 'selected', mapped = d.kind === 'Mapped';
    const fromZ = this.zonesByCode().get(from.zoneCode), toZ = to ? this.zonesByCode().get(to.zoneCode) : undefined;
    let pts: Pt[] | null = null, title: string, startLabel = '';
    if (walk) {
      title = 'КПП → ' + locLabel(from);
      if (mapped && d.code === from.zoneCode && from.spotCode) { pts = pathFromGate(geom(from.spotCode)); startLabel = 'Ви тут · КПП'; }
      else if (fromZ?.kind === 'External') title = 'Маршрут до: ' + fromZ.name;
    } else {
      title = to ? `${locLabel(from)} → ${locLabel(to)}` : `З ${locLabel(from)}: оберіть місце`;
      if (mapped && d.code === from.zoneCode && from.spotCode) {
        const a = geom(from.spotCode);
        startLabel = 'Авто';
        if (to && to.zoneCode === d.code && to.spotCode) pts = pathBetween(a, geom(to.spotCode));
        else if (to) pts = pathFromGate(a).reverse();
      } else if (mapped && to && to.zoneCode === d.code && to.spotCode) {
        pts = pathFromGate(geom(to.spotCode));
        startLabel = 'В’їзд';
      }
    }
    const meters = pts ? Math.round(pathLength(pts) * 2.5) : 0;
    const dz = !walk && toZ ? toZ : fromZ;
    const dest = dz?.kind === 'External' ? dz.name.split(' · ').slice(1).join(', ') + ', Україна' : 'Соломоново, Закарпатська область, Україна';
    const { W, H } = this.size();
    const st = pts ? pts[0] : [0, 0], en = pts ? pts[pts.length - 1] : [0, 0];
    return {
      title, startLabel, canDraw: !!pts, draw: !!pts && this.routeOn() && this.view() === 'aerial',
      meta: pts
        ? `${meters} м · приблизно ${Math.max(1, Math.round(meters / (walk ? 1.3 : 2.8) / 60))} хв ${walk ? 'пішки' : 'на авто майданчиком'}`
        : dz?.kind === 'External' ? 'Дорога до зовнішнього складу: відкрийте Google Карти.' : 'Відкрийте зону на карті, щоб побачити маршрут.',
      points: pts ? pts.map(p => p[0].toFixed(2) + ',' + p[1].toFixed(2)).join(' ') : '',
      sx: (st[0] / W) * 100, sy: (st[1] / H) * 100, ex: (en[0] / W) * 100, ey: (en[1] / H) * 100,
      gmaps: 'https://www.google.com/maps/dir/?api=1&destination=' + encodeURIComponent(dest) + '&travelmode=driving',
    };
  });

  protected readonly focusSpot = computed(() => {
    const d = this.detail(), to = this.store.to(), from = this.store.from();
    if (!d) return null;
    if (to?.zoneCode === d.code && to.spotCode) return to.spotCode;
    if (this.store.phase() !== 'idle' && from?.zoneCode === d.code) return from.spotCode;
    return null;
  });

  protected readonly hint = computed(() => {
    const p = this.store.phase(), mapped = this.mapped();
    if (p === 'idle') return mapped ? 'Натисніть на авто на карті або знайдіть його за VIN у шапці.' : 'Оберіть авто з онлайн-залишків складу.';
    if (p === 'selected') return 'Авто вибрано. Маршрут від КПП показано на карті. Почніть операцію в панелі праворуч.';
    if (p === 'moving') return mapped ? 'Натисніть на вільне місце, куди поставили авто. Іншу зону оберіть праворуч.' : 'Натисніть «Відправити авто на цей склад», щоб передати авто.';
    return 'Операцію завершено. Оберіть наступне авто.';
  });

  protected readonly extInfo = computed(() => {
    const d = this.detail();
    if (!d || d.kind !== 'External') return '';
    const when = d.lastSyncedAt ? ago(this.clock.now() - Date.parse(d.lastSyncedAt)) : '—';
    return d.isOnline ? 'Онлайн · оновлено ' + when : 'Немає зв’язку · останнє оновлення ' + when;
  });
  protected readonly canSendHere = computed(() => this.moving() && this.detail()?.kind === 'External' && !!this.detail()?.isOnline);
  protected readonly routeToggleLabel = computed(() => (this.routeOn() && this.view() === 'aerial' ? 'Сховати маршрут' : 'Побудувати маршрут'));

  protected setView(v: View) {
    this.view.set(v);
    localStorage.setItem('eurocar.view', v);
  }
  protected toggleRoute() {
    if (this.routeOn() && this.view() === 'aerial') this.routeOn.set(false);
    else { this.routeOn.set(true); this.setView('aerial'); }
  }
  protected toZones() { this.router.navigate(['/zones']); }
  protected max0(n: number) { return Math.max(0, n); }

  protected async clickSpot(vm: SpotVm) {
    const d = this.detail();
    if (!d || !vm.clickable) return;
    if (this.moving()) {
      const from = this.store.from();
      const isOrigin = from?.zoneCode === d.code && from.spotCode === vm.code;
      await this.store.setTarget(isOrigin ? null : d.code, isOrigin ? null : vm.code);
      return;
    }
    if (vm.car) this.store.select(vm.car);
  }

  protected pickExternal(c: Car) { this.store.select(c); }

  /** Destination chip or «Відправити авто на цей склад» (prototype setDest). */
  async setDest(code: string) {
    const kind = this.zonesByCode().get(code)?.kind ?? (this.detail()?.code === code ? this.detail()?.kind : undefined);
    if (kind === 'External') await this.store.setTarget(code);
    else if (this.store.to()) await this.store.setTarget(null);
    if (code !== this.zone()) this.router.navigate(['/move', code]);
  }
}
```

`frontend/src/app/features/move/move-page.html`:
```html
<main class="split">
  <section>
    <div style="display:flex;gap:var(--space-4);align-items:flex-end;flex-wrap:wrap">
      <div style="flex:1 1 auto;display:flex;flex-direction:column;gap:4px;align-items:flex-start">
        <button class="btn btn-ghost" (click)="toZones()">← Усі зони</button>
        @if (detail(); as d) {
          <div class="kicker">{{ d.groupCode }} · {{ d.groupName }}</div>
          <div class="title-lg">{{ d.code }} · {{ d.name }}</div>
          <div style="font-size:15px">{{ d.used }} зайнято · {{ max0(d.capacity - d.used) }} вільно · {{ d.capacity }} місць</div>
        }
      </div>
      @if (mapped()) {
        <div class="seg">
          <label class="seg-opt" style="white-space:nowrap"><input type="radio" name="view" [checked]="view() === 'aerial'" (change)="setView('aerial')">Вид згори</label>
          <label class="seg-opt"><input type="radio" name="view" [checked]="view() === 'scheme'" (change)="setView('scheme')">Схема</label>
        </div>
      }
    </div>

    @if (route(); as r) {
      <div style="display:flex;align-items:center;gap:var(--space-3);flex-wrap:wrap;border:2px solid var(--color-accent);padding:var(--space-3) var(--space-4)">
        <div style="flex:1 1 220px">
          <div class="kicker">Маршрут</div>
          <div style="font-size:18px;font-weight:800">{{ r.title }}</div>
          <div style="font-size:14px">{{ r.meta }}</div>
        </div>
        @if (r.canDraw) {
          <button class="btn btn-secondary" (click)="toggleRoute()">{{ routeToggleLabel() }}</button>
        }
        <a class="btn btn-primary" [href]="r.gmaps" target="_blank" rel="noopener" style="color:#fff">Google Карти ↗</a>
      </div>
    }

    <div style="font-size:15px;padding:var(--space-3) var(--space-4);background:var(--color-surface)">{{ hint() }}</div>

    @if (detail(); as d) {
      @if (mapped()) {
        @if (view() === 'aerial') {
          <app-zone-map-aerial [rows]="d.rows!" [perRow]="d.perRow!" [strip]="d.stripLabel" [spots]="spots()" [route]="route()"
            [focusSpot]="focusSpot()" [mob]="vp.mob()" (spotClick)="clickSpot($event)" />
        } @else {
          <app-zone-map-scheme [perRow]="d.perRow!" [spots]="spots()" (spotClick)="clickSpot($event)" />
        }
        <div class="legend">
          <div><span class="sw" style="background:var(--color-surface);border:1px solid var(--color-neutral-400)"></span>Зайнято</div>
          <div><span class="sw" style="background:var(--color-bg);border:1px solid var(--color-neutral-400)"></span>Вільно</div>
          <div><span class="sw" style="background:var(--color-accent-2);border:1px solid var(--color-accent)"></span>Вибрано</div>
          <div><span class="sw" style="background:var(--color-accent-2-200);box-shadow:inset 0 0 0 2px var(--color-accent-2-700)"></span>Нове місце</div>
          <div><span style="width:18px;border-top:3px dashed var(--color-accent-2-700)"></span>Маршрут</div>
        </div>
      } @else {
        <div style="display:flex;align-items:center;gap:var(--space-3);border-top:2px solid var(--color-text);border-bottom:2px solid var(--color-text);padding:var(--space-3) 0;flex-wrap:wrap">
          <span style="width:10px;height:10px" [style.background]="d.isOnline ? 'var(--color-accent-2-600)' : 'var(--color-neutral-500)'"></span>
          <span style="font-weight:700">{{ extInfo() }}</span>
          <span style="font-size:13px;color:var(--color-neutral-600)">Залишки надходять зі складу автоматично</span>
          <div style="flex:1"></div>
          @if (canSendHere()) {
            <button class="btn btn-primary" (click)="setDest(d.code)">Відправити авто на цей склад</button>
          }
        </div>
        <div style="display:flex;flex-direction:column">
          @for (c of d.cars; track c.id) {
            <div class="row-line" style="gap:var(--space-1) var(--space-4);padding:var(--space-2)" [style.background]="store.car()?.id === c.id ? 'var(--color-accent-2-100)' : 'transparent'">
              <div style="flex:1 1 200px;min-width:0">
                <b>Škoda {{ c.model }}</b> · {{ c.color }}
                <div class="tnum" style="font-size:13px;letter-spacing:0.04em">{{ c.vin }}</div>
              </div>
              @if (!moving()) {
                <button class="btn btn-secondary" style="min-height:44px" (click)="pickExternal(c)">Вибрати</button>
              }
            </div>
          }
        </div>
      }
    } @else {
      <div style="font-size:15px">Завантаження…</div>
    }
  </section>
  <aside></aside>
</main>
```
(`<aside>` is filled in Task 10.)

- [ ] **Step 4: Build and check in the browser**

Run: `cd frontend && npx ng build`. Expected: success.

With both apps running, open `/move/S1` and compare with the prototype's «Переміщення» tab:
1. The aerial view shows a dark lot with the grey «В’ЇЗД · КПП» strip, dashed lanes, aisle arrows and car rectangles in grey shades. It scrolls horizontally.
2. Clicking a car highlights it in mint. The «Маршрут» card shows «КПП → S1 · X…» with metres and minutes, and a dashed mint polyline runs from the gate to the car. «Сховати маршрут» hides the polyline.
3. «Схема» shows a grid of cells with the last 6 VIN characters, or «вільно».
4. Searching `tmb` in the header gives a thick ring on matching cars. Picking a result opens its zone with the car selected.
5. `/move/P01` shows the online status line and the car list, and «Вибрати» highlights a row. `/move/P07` shows «Немає зв’язку · останнє оновлення 47 хв тому».

- [ ] **Step 5: Commit**

```bash
git add frontend
git commit -m "feat(web): zone map with aerial and scheme views, routes and external stock" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 10: Move panel: 5 steps, start, target, photo, finish, cancel, result + mobile bar

**Files:**
- Create: `frontend/src/app/features/move/move-panel.ts`, `move-panel.html`
- Modify: `frontend/src/app/features/move/move-page.ts`, `move-page.html`

**Interfaces:**
- Consumes:
  - `MoveStore` (all actions), `OperatorStore`, `SettingsStore`, `Clock`, `Lightbox`, `MobileBarStore`, `DestChip`.
  - `MovePage` members `detail, zonesByCode, store, settings, clock, vp, route, setDest` (Task 9).
  - From `format.ts`: `dur, hms, keyLabel, locLabel, moveDuration, pad, val, vinTail`.
  - From `zone-layout.ts`: `GATE, geom, nearestFree`.
- Produces:
  - `MovePanel`: inputs `zone`, `nearFree`, `destChips`; output `destSelected`.
  - `MovePage.nearFree`, `MovePage.destChips` and `MovePage.mobBar`, which feeds `MobileBarStore`.

- [ ] **Step 1: Move panel component**

`frontend/src/app/features/move/move-panel.ts`:
```ts
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Clock } from '../../core/clock';
import { Lightbox } from '../../core/lightbox';
import { Move, ZoneDetail } from '../../core/models';
import { MoveStore } from '../../core/move-store';
import { OperatorStore } from '../../core/operator';
import { SettingsStore } from '../../core/settings';
import { dur, hms, keyLabel, locLabel, moveDuration, pad, val, vinTail } from '../../shared/format';
import { DestChip } from './view-models';

const STEPS = ['Вибір', 'Початок', 'У дорозі', 'Фото', 'Підсумок'];

/** Right-hand panel of «Переміщення». Prototype lines 205–334 and 833–843. */
@Component({
  selector: 'app-move-panel',
  templateUrl: './move-panel.html',
  host: { style: 'display:flex;flex-direction:column;flex:1' },
})
export class MovePanel {
  readonly zone = input<ZoneDetail | null>(null);
  readonly nearFree = input<string[]>([]);
  readonly destChips = input<DestChip[]>([]);
  readonly destSelected = output<string>();

  protected readonly store = inject(MoveStore);
  protected readonly ops = inject(OperatorStore);
  private readonly clock = inject(Clock);
  private readonly settings = inject(SettingsStore);
  private readonly router = inject(Router);
  private readonly lightbox = inject(Lightbox);

  protected readonly spotInput = signal('');
  protected readonly spotErr = signal('');
  protected readonly cancelSpot = signal('');
  protected readonly cancelErr = signal('');
  protected readonly hms = hms;
  protected readonly locLabel = locLabel;
  protected readonly moveDuration = moveDuration;
  protected readonly vinTail = vinTail;
  protected readonly val = val;

  protected readonly steps = computed(() => {
    const p = this.store.phase();
    const idx = p === 'idle' ? 0 : p === 'selected' ? 1 : p === 'moving' ? (this.store.to() ? 3 : 2) : 4;
    return STEPS.map((label, i) => ({
      n: pad(i + 1), label,
      bg: i === idx ? 'var(--color-accent)' : 'transparent',
      fg: i === idx ? '#fff' : i < idx ? 'var(--color-text)' : 'var(--color-neutral-500)',
    }));
  });
  protected readonly requirePhoto = computed(() => this.settings.value().requirePhoto);
  protected readonly showCar = computed(() => this.store.phase() === 'selected' || this.store.phase() === 'moving');
  protected readonly elapsed = computed(() => {
    const a = this.store.active();
    return a ? dur(this.clock.now() - Date.parse(a.startedAt)) : '00:00';
  });
  protected readonly keyText = computed(() => {
    const c = this.store.car();
    if (!c) return '';
    if (c.keyCabinet) return keyLabel(c.keyCabinet, c.keyNumber) + (this.store.phase() === 'moving' ? ' · видано' : '');
    return c.status === 'AtExternal' ? 'у складу-отримувача' : 'немає в ключниці';
  });
  protected readonly toName = computed(() => this.store.to()?.zoneName ?? (this.store.phase() === 'moving' ? 'не обрано' : ''));
  protected readonly movingHint = computed(() => {
    const to = this.store.to(), a = this.store.active();
    if (!to) return 'Оберіть зону й вільне місце на карті.';
    if (this.requirePhoto() && !a?.photoUrl) return `Нове місце: ${locLabel(to)}. Сфотографуйте, як поставили авто, щоб завершити операцію.`;
    return to.spotCode ? `Нове місце: ${locLabel(to)}. Перевірте й завершіть операцію.` : `Авто буде передано: ${to.zoneName}. Місце призначить склад.`;
  });
  protected readonly cannotFinish = computed(
    () => !this.store.to() || (this.requirePhoto() && !this.store.active()?.photoUrl) || this.store.busy());
  protected readonly zoneMapped = computed(() => this.zone()?.kind === 'Mapped');

  protected async changeOperator(id: number) {
    this.ops.set(id);
    this.store.reset();
    await this.store.resume();
  }

  protected async applySpot() {
    const z = this.zone();
    if (!z || z.kind !== 'Mapped') return;
    const spot = this.spotInput().trim().toUpperCase().replace(/\s/g, '');
    if (!spot) { this.spotErr.set('Вкажіть номер місця.'); return; }
    const err = await this.store.setTarget(z.code, spot, true);
    this.spotErr.set(err ?? '');
    if (!err) this.spotInput.set('');
  }

  protected async pickNear(spot: string) {
    this.spotErr.set('');
    const z = this.zone();
    if (z) await this.store.setTarget(z.code, spot);
  }

  protected async onPhoto(e: Event) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) await this.store.photo(file);
  }

  protected async finish() {
    await this.store.finish();
    const r = this.store.result();
    if (r?.to) this.router.navigate(['/move', r.to.zoneCode]);
  }

  protected async cancel(fallback: string | null = null) {
    const from = this.store.from();
    const err = await this.store.cancel(fallback);
    if (err) { if (fallback) this.cancelErr.set(err); return; }
    this.cancelSpot.set('');
    this.cancelErr.set('');
    if (from) this.router.navigate(['/move', from.zoneCode]);
  }

  protected toZones() { this.router.navigate(['/zones']); }
  protected openPhoto(src: string, caption: string) { this.lightbox.open(src, caption); }
  protected photoCaption(m: Move) {
    return 'Фото постановки · ' + locLabel(m.to) + (m.photoTakenAt ? ' · ' + hms(m.photoTakenAt) : '');
  }
  protected resultCaption(m: Move) {
    return `Операція № ${m.no} · ${locLabel(m.from)} → ${locLabel(m.to)}` + (m.completedAt ? ' · ' + hms(m.completedAt) : '');
  }
}
```

`frontend/src/app/features/move/move-panel.html`:
```html
<div style="display:grid;grid-template-columns:repeat(5,minmax(0,1fr));border-bottom:2px solid var(--color-text)">
  @for (s of steps(); track s.n) {
    <div style="padding:var(--space-3);border-right:1px solid var(--color-divider)" [style.background]="s.bg" [style.color]="s.fg">
      <div style="font-size:18px;font-weight:800">{{ s.n }}</div>
      <div style="font-size:12px;font-weight:600">{{ s.label }}</div>
    </div>
  }
</div>

<div style="padding:var(--space-6);display:flex;flex-direction:column;gap:var(--space-6);flex:1">
  <div class="field" style="margin:0">
    <label>Оператор</label>
    <select class="input input-lg" style="width:100%" [disabled]="store.phase() === 'moving'" (change)="changeOperator(+val($event))">
      @for (o of ops.list(); track o.id) {
        <option [value]="o.id" [selected]="o.id === ops.id()">{{ o.displayName }}</option>
      }
    </select>
  </div>

  @if (store.phase() === 'idle') {
    <div style="display:flex;flex-direction:column;gap:var(--space-3)">
      <div style="font-size:32px;font-weight:800;line-height:1.05;letter-spacing:-0.02em">Оберіть автомобіль</div>
      <div style="font-size:15px;color:var(--color-neutral-700)">На карті зони, у списку складу або через пошук за VIN.</div>
    </div>
  }

  @if (showCar()) {
    @if (store.car(); as car) {
      <div style="border-top:2px solid var(--color-text);padding-top:var(--space-4);display:grid;grid-template-columns:1fr 1fr;gap:var(--space-4)">
        <div style="grid-column:1/-1">
          <div class="kicker">Автомобіль</div>
          <div style="font-size:32px;font-weight:800;letter-spacing:-0.01em">Škoda {{ car.model }}</div>
          <div style="font-size:14px;letter-spacing:0.04em">VIN {{ car.vin }} · {{ car.color }}</div>
          <div style="margin-top:var(--space-2);display:flex;gap:var(--space-2);align-items:baseline;font-size:14px"><span class="tag tag-accent-2">Ключ</span><b>{{ keyText() }}</b></div>
        </div>
        <div>
          <div class="kicker muted">Звідки</div>
          <div style="font-size:28px;font-weight:800">{{ locLabel(store.from()) }}</div>
          <div style="font-size:13px">{{ store.from()?.zoneName }}</div>
        </div>
        <div>
          <div class="kicker muted">Куди</div>
          <div style="font-size:28px;font-weight:800;color:var(--color-accent-2-700)">{{ locLabel(store.to()) }}</div>
          <div style="font-size:13px">{{ toName() }}</div>
        </div>
      </div>
    }
  }

  @if (store.phase() === 'selected') {
    <div style="display:flex;flex-direction:column;gap:var(--space-2)">
      <button class="btn btn-primary btn-block" [disabled]="store.busy()" (click)="store.start()">Почати переміщення</button>
      <button class="btn btn-ghost btn-block" (click)="store.reset()">Скасувати вибір</button>
    </div>
  }

  @if (store.active(); as a) {
    <div style="display:flex;flex-direction:column;gap:var(--space-4)">
      <div style="border:2px solid var(--color-accent-2-600);padding:var(--space-4)">
        <div style="font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;color:var(--color-accent-2-800)">У дорозі · початок {{ hms(a.startedAt) }}</div>
        <div class="tnum" style="font-size:56px;font-weight:800;line-height:1">{{ elapsed() }}</div>
      </div>

      <div class="field" style="margin:0">
        <label>Зона призначення</label>
        <div class="cells" style="grid-template-columns:repeat(auto-fill,minmax(64px,1fr))">
          @for (d of destChips(); track d.code) {
            <div role="button" tabindex="0" style="cursor:pointer;padding:6px 8px" [title]="d.name"
              [style.background]="d.on ? 'var(--color-accent)' : 'var(--color-bg)'" [style.color]="d.on ? '#fff' : 'var(--color-text)'"
              (click)="destSelected.emit(d.code)" (keydown.enter)="destSelected.emit(d.code)">
              <div style="font-size:15px;font-weight:800">{{ d.code }}</div>
              <div style="font-size:11px">вільн. {{ d.free }}</div>
            </div>
          }
        </div>
        <button class="btn btn-ghost" style="margin-top:var(--space-2)" (click)="toZones()">Зовнішній склад або дилер →</button>
      </div>

      @if (zoneMapped()) {
        <div class="field" style="margin:0">
          <label>Номер місця в зоні {{ zone()?.code }}</label>
          <div style="display:flex;gap:var(--space-2)">
            <input class="input input-lg" placeholder="напр. D7" style="flex:1;min-width:0;text-transform:uppercase"
              [value]="spotInput()" (input)="spotInput.set(val($event).toUpperCase()); spotErr.set('')" (keydown.enter)="applySpot()">
            <button class="btn btn-secondary" style="min-height:44px" (click)="applySpot()">Вказати</button>
          </div>
          @if (spotErr()) {
            <div class="err" style="margin-top:4px">{{ spotErr() }}</div>
          }
          <div style="font-size:12px;margin-top:var(--space-3);color:var(--color-neutral-700)">Найближчі вільні</div>
          <div style="display:flex;flex-wrap:wrap;gap:var(--space-2);margin-top:4px">
            @for (f of nearFree(); track f) {
              <button class="btn btn-secondary" style="min-height:44px;min-width:56px" (click)="pickNear(f)">{{ f }}</button>
            }
          </div>
        </div>
      }

      <div style="font-size:15px">{{ movingHint() }}</div>

      @if (store.to()) {
        <div style="display:flex;flex-direction:column;gap:var(--space-2);border-top:2px solid var(--color-text);padding-top:var(--space-4)">
          <div class="kicker">Фото постановки</div>
          @if (a.photoUrl) {
            <img [src]="a.photoUrl" alt="Фото постановки авто" title="Натисніть, щоб збільшити" (click)="openPhoto(a.photoUrl, photoCaption(a))"
              style="width:100%;height:220px;object-fit:cover;display:block;border:2px solid var(--color-text);box-sizing:border-box;cursor:zoom-in">
            <div style="font-size:13px;color:var(--color-neutral-700)">Знято о {{ hms(a.photoTakenAt!) }}</div>
          } @else {
            <div style="font-size:14px;color:var(--color-neutral-700)">Сфотографуйте авто на новому місці так, щоб було видно розмітку й номер місця.</div>
          }
          <label class="btn btn-secondary btn-block" style="position:relative;margin-top:0;cursor:pointer">
            {{ a.photoUrl ? 'Перезняти фото' : 'Зробити фото' }}
            <input class="file-hidden" type="file" accept="image/*" capture="environment" (change)="onPhoto($event)">
          </label>
        </div>
      }

      <button class="btn btn-primary btn-block" (click)="finish()" [disabled]="cannotFinish()">Завершити операцію</button>

      @if (store.originTaken()) {
        <div style="display:flex;flex-direction:column;gap:var(--space-2);border:2px solid var(--color-accent-2-600);padding:var(--space-3)">
          <div style="font-size:14px">Початкове місце {{ locLabel(store.from()) }} вже зайняте. Вкажіть інше місце в зоні {{ store.from()?.zoneCode }} для повернення.</div>
          <div style="display:flex;gap:var(--space-2)">
            <input class="input input-lg" placeholder="напр. D7" style="flex:1;min-width:0;text-transform:uppercase"
              [value]="cancelSpot()" (input)="cancelSpot.set(val($event).toUpperCase()); cancelErr.set('')">
            <button class="btn btn-secondary" style="min-height:44px" (click)="cancel(cancelSpot())">Повернути</button>
          </div>
          @if (cancelErr()) {
            <div class="err">{{ cancelErr() }}</div>
          }
        </div>
      } @else {
        <button class="btn btn-ghost btn-block" (click)="cancel()">Скасувати: повернути на {{ locLabel(store.from()) }}</button>
      }
    </div>
  }

  @if (store.phase() === 'done') {
    @if (store.result(); as r) {
      <div style="display:flex;flex-direction:column;gap:var(--space-4)">
        <div style="background:var(--color-accent);color:#fff;padding:var(--space-4)">
          <div style="font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;color:var(--color-accent-2)">Результат · операція № {{ r.no }}</div>
          <div style="font-size:30px;font-weight:800;line-height:1.1">{{ locLabel(r.from) }} → {{ locLabel(r.to) }}</div>
          <div style="font-size:14px;margin-top:4px">Škoda {{ r.car.model }} · VIN …{{ vinTail(r.car.vin) }}</div>
        </div>
        @if (r.photoUrl) {
          <img [src]="r.photoUrl" alt="Фото постановки авто" title="Натисніть, щоб збільшити" (click)="openPhoto(r.photoUrl, resultCaption(r))"
            style="width:100%;height:220px;object-fit:cover;display:block;border:2px solid var(--color-text);box-sizing:border-box;cursor:zoom-in">
        }
        <div class="stats" style="grid-template-columns:1fr 1fr 1fr">
          <div><div style="font-size:12px;color:var(--color-neutral-600)">Початок</div><div style="font-size:20px;font-weight:700">{{ hms(r.startedAt) }}</div></div>
          <div><div style="font-size:12px;color:var(--color-neutral-600)">Кінець</div><div style="font-size:20px;font-weight:700">{{ hms(r.completedAt!) }}</div></div>
          <div><div style="font-size:12px;color:var(--color-neutral-600)">Час</div><div style="font-size:20px;font-weight:700">{{ moveDuration(r) }}</div></div>
        </div>
        <div style="font-size:14px;color:var(--color-neutral-700);display:flex;flex-direction:column;gap:4px">
          <div>{{ r.from.zoneName }} → {{ r.to?.zoneName }}</div>
          <div>Ключ: {{ r.keyLabel }}</div>
          <div>Оператор: {{ r.operatorName }}</div>
        </div>
        <button class="btn btn-primary btn-block" (click)="store.reset()">Нова операція</button>
      </div>
    }
  }
</div>
```

- [ ] **Step 2: Wire the panel, nearest-free spots, destination chips and mobile bar into MovePage**

In `frontend/src/app/features/move/move-page.ts`:
- Change the Angular core import to `import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';`.
- Add these imports:
```ts
import { MobileBar, MobileBarStore } from '../../core/mobile-bar';
import { dur, moveDuration } from '../../shared/format';
import { GATE, nearestFree } from '../../shared/zone-layout';
import { DestChip } from './view-models';
import { MovePanel } from './move-panel';
```
  Merge the `format` and `zone-layout` imports into the existing import lines.
- Change `imports: [ZoneMapAerial, ZoneMapScheme]` to `imports: [ZoneMapAerial, ZoneMapScheme, MovePanel]`.
- Add a field `private readonly mobileBar = inject(MobileBarStore);` next to the other injections.
- At the end of the constructor, add:
```ts
    effect(() => this.mobileBar.bar.set(this.mobBar()));
    inject(DestroyRef).onDestroy(() => this.mobileBar.bar.set(null));
```
- Add these members to the class:
```ts
  protected readonly nearFree = computed(() => {
    const d = this.detail();
    if (!this.moving() || !d || d.kind !== 'Mapped') return [];
    const from = this.store.from();
    const fromHere = from?.zoneCode === d.code ? from.spotCode : null;
    const taken = new Set(d.spots.filter(s => s.car || s.reserved).map(s => s.code));
    return nearestFree(d.spots.map(s => s.code), s => !taken.has(s) && s !== fromHere, fromHere ? geom(fromHere) : GATE);
  });

  protected readonly destChips = computed<DestChip[]>(() => {
    const selected = this.store.to()?.zoneCode ?? this.zone();
    return [...this.zonesByCode().values()]
      .filter(z => z.kind === 'Mapped')
      .map(z => ({ code: z.code, name: z.name, free: Math.max(0, z.capacity - z.used), on: z.code === selected }));
  });

  /** Prototype lines 863–870. */
  protected readonly mobBar = computed<MobileBar | null>(() => {
    const phase = this.store.phase(), car = this.store.car();
    if (!this.vp.mob() || phase === 'idle' || !car) return null;
    const a = this.store.active(), r = this.store.result(), to = this.store.to();
    const elapsed = a ? dur(this.clock.now() - Date.parse(a.startedAt)) : '';
    if (phase === 'selected') {
      const route = this.route();
      return { kicker: `Škoda ${car.model} · ${locLabel(this.store.from())}`, title: route?.canDraw ? route.meta : 'Готово до початку', kind: 'btn', label: 'Почати', action: () => this.store.start() };
    }
    if (phase === 'moving' && !to) return { kicker: 'У дорозі · ' + elapsed, title: 'Вкажіть нове місце', kind: 'text' };
    if (phase === 'moving' && this.settings.value().requirePhoto && !a?.photoUrl)
      return { kicker: `Місце ${locLabel(to)} · ${elapsed}`, title: 'Сфотографуйте авто', kind: 'photo', onFile: f => this.store.photo(f) };
    if (phase === 'moving') return { kicker: `Місце ${locLabel(to)} · ${elapsed}`, title: 'Можна завершувати', kind: 'btn', label: 'Завершити', action: () => this.store.finish() };
    if (r) return { kicker: `Операція № ${r.no} · ${moveDuration(r)}`, title: `${locLabel(r.from)} → ${locLabel(r.to)}`, kind: 'btn', label: 'Нова', action: () => this.store.reset() };
    return null;
  });
```

In `frontend/src/app/features/move/move-page.html`, replace `<aside></aside>` with:
```html
  <aside>
    <app-move-panel [zone]="detail()" [nearFree]="nearFree()" [destChips]="destChips()" (destSelected)="setDest($event)" />
  </aside>
```

- [ ] **Step 3: Build**

Run: `cd frontend && npx ng build`
Expected: success.

- [ ] **Step 4: Walk the full move flow in the browser**

With both apps running, open `/move/S1`:
1. Click a car. Step «02 Початок» is highlighted, the panel shows «Škoda …», VIN, «Ключ Шафа …», «Звідки S1 · X», and the route is drawn.
2. «Почати переміщення» starts the timer at «00:0x». The car's old spot shows «У ДОРОЗІ», and the key text ends with «· видано».
3. Type `zz9` in «Номер місця» and press «Вказати». It shows «У зоні S1 немає місця ZZ9.» Type the origin spot: it shows «Це початкове місце авто.» Type an occupied spot: it shows «Місце … зайняте.»
4. Click a «Найближчі вільні» chip. The spot shows «СЮДИ» on the map, step «04 Фото» is highlighted, and «Завершити операцію» stays disabled.
5. «Зробити фото», then pick any image. The thumbnail and «Знято о …» appear. Clicking the thumbnail opens the lightbox, and Esc closes it.
6. «Завершити операцію» shows the dark result card «Результат · операція № 4», the photo, start/end/time, key and operator. «Нова операція» returns the panel to idle.
7. Start another move, pick the destination chip «T1», click a free spot there, take a photo and finish. The page navigates to `/move/T1` with the car on its new spot.
8. Start a move, then «Скасувати: повернути на …». The car is back on its old spot.
9. Open `/move/P01` with a move in progress. «Відправити авто на цей склад» sets «Куди P01». After the photo and finish, the key reads «передано з авто».

- [ ] **Step 5: Check the mobile layout**

In devtools device mode at 390×844: the bottom nav is visible. Selecting a car shows the action bar «Почати». While moving it shows «Вкажіть нове місце», then «Сфотографуйте авто» with the «Зробити фото» button, then «Завершити».

- [ ] **Step 6: Review Focus #6, page reload mid-move**

Start a move, set a target and add a photo, then press F5. Expected: the panel comes back in «У дорозі». The timer continues from the original start, and the target, photo and «Завершити операцію» are all restored.

- [ ] **Step 7: Commit**

```bash
git add frontend
git commit -m "feat(web): move panel with full 5-step workflow, photo, cancel and mobile action bar" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 11: «Ключі» page

**Files:**
- Modify (replace stub): `frontend/src/app/features/keys/keys-page.ts`
- Create: `frontend/src/app/features/keys/keys-page.html`

**Interfaces:**
- Consumes:
  - `Api.keysSummary/cabinet`, `MoveStore`, `SearchStore`, `Viewport`, `MobileBarStore`, `Notify`, `pollEvery`.
  - From `format.ts`: `pad, keyLabel, locLabel, vinTail`.
- Produces: `KeysPage` (route `/keys/:cabinet?slot=n`).

- [ ] **Step 1: Keys page**

`frontend/src/app/features/keys/keys-page.ts`:
```ts
import { Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { MobileBar, MobileBarStore } from '../../core/mobile-bar';
import { Car, KeyCell, KeysSummary } from '../../core/models';
import { MoveStore } from '../../core/move-store';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { SearchStore } from '../../core/search';
import { Viewport } from '../../core/viewport';
import { keyLabel, locLabel, pad, vinTail } from '../../shared/format';

const CABINETS = 12;

/** Shared key box: 12 cabinets × 100 cells. Prototype lines 338–405 and 845–861. */
@Component({ selector: 'app-keys-page', templateUrl: './keys-page.html' })
export class KeysPage {
  readonly cabinet = input.required<string>();
  readonly slot = input<string>();

  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notify = inject(Notify);
  private readonly mobileBar = inject(MobileBarStore);
  private readonly store = inject(MoveStore);
  private readonly search = inject(SearchStore);
  protected readonly vp = inject(Viewport);

  protected readonly summary = signal<KeysSummary | null>(null);
  protected readonly cells = signal<KeyCell[]>([]);
  protected readonly pad = pad;
  protected readonly keyLabel = keyLabel;
  protected readonly locLabel = locLabel;

  protected readonly cab = computed(() => Math.min(CABINETS, Math.max(1, Math.trunc(+this.cabinet()) || 1)));
  protected readonly sel = computed(() => Math.trunc(+(this.slot() ?? 0)) || null);
  protected readonly selCell = computed(() => this.cells().find(k => k.number === this.sel()) ?? null);
  protected readonly selCar = computed(() => this.selCell()?.car ?? null);

  constructor() {
    effect(() => {
      const c = this.cab();
      this.store.version();
      untracked(() => this.load(c));
    });
    pollEvery(10_000, () => this.load(this.cab()));
    effect(() => this.mobileBar.bar.set(this.mobBar()));
    inject(DestroyRef).onDestroy(() => this.mobileBar.bar.set(null));
  }

  private async load(c: number) {
    try {
      const [summary, cells] = await Promise.all([this.api.keysSummary(), this.api.cabinet(c)]);
      if (c !== this.cab()) return;
      this.summary.set(summary);
      this.cells.set(cells);
    } catch (e) {
      if (!this.cells().length) this.notify.error(e);
    }
  }

  protected readonly cabs = computed(() => {
    const counts = new Map((this.summary()?.cabinets ?? []).map(x => [x.cabinet, x.count]));
    const hits = new Set(this.search.results().map(r => r.keyCabinet).filter(Boolean));
    return Array.from({ length: CABINETS }, (_, i) => {
      const cb = i + 1, on = cb === this.cab();
      return {
        cb, name: 'Шафа ' + pad(cb), count: `${counts.get(cb) ?? 0} / 100`,
        bg: on ? 'var(--color-accent)' : 'var(--color-bg)', fg: on ? '#fff' : 'var(--color-text)',
        ring: hits.has(cb) ? 'inset 0 0 0 3px var(--color-accent-2-600)' : 'none',
      };
    });
  });

  protected readonly kcells = computed(() => this.cells().map(k => {
    const c = k.car, out = k.status === 'Issued';
    let bg = c ? (out ? 'var(--color-accent-2-300)' : 'var(--color-surface)') : 'var(--color-bg)';
    let fg = c ? 'var(--color-text)' : 'var(--color-neutral-400)';
    if (k.number === this.sel()) { bg = 'var(--color-accent)'; fg = '#fff'; }
    return {
      number: k.number, n: pad(k.number), label: c ? (out ? 'видано' : vinTail(c.vin)) : '—', bg, fg,
      ring: c && this.search.matches(c.vin) ? 'inset 0 0 0 3px var(--color-text)' : 'none',
      title: c ? `Škoda ${c.model} · VIN ${c.vin}` : 'Порожня комірка',
    };
  }));

  protected readonly mobBar = computed<MobileBar | null>(() => {
    const car = this.selCar();
    if (!this.vp.mob() || !car) return null;
    return {
      kicker: keyLabel(this.cab(), this.sel()),
      title: `Škoda ${car.model} · ${car.zoneCode ? locLabel(car) : 'у дорозі'}`,
      kind: car.zoneCode ? 'btn' : 'text', label: 'На карті', action: () => this.showOnMap(car),
    };
  });

  protected selectCab(cb: number) { this.router.navigate(['/keys', cb]); }
  protected selectCell(n: number) { this.router.navigate(['/keys', this.cab()], { queryParams: { slot: n } }); }
  protected showOnMap(car: Car) {
    if (car.zoneCode && this.store.select(car)) this.router.navigate(['/move', car.zoneCode]);
  }
}
```

`frontend/src/app/features/keys/keys-page.html`:
```html
<main class="split">
  <section>
    <div>
      <div class="kicker">Спільна ключниця · 12 шаф по 100 комірок</div>
      <div style="font-size:15px;margin-top:4px">Оберіть шафу й комірку. Пошук за VIN у шапці одразу відкриє потрібну комірку.</div>
    </div>
    <div class="stats" style="grid-template-columns:repeat(3,minmax(0,1fr))">
      <div><div style="font-size:28px;font-weight:800">{{ summary()?.inBox ?? '—' }}</div><div style="font-size:13px">ключів на місці</div></div>
      <div><div style="font-size:28px;font-weight:800">{{ summary()?.issued ?? '—' }}</div><div style="font-size:13px">видано</div></div>
      <div><div style="font-size:28px;font-weight:800">{{ summary()?.empty ?? '—' }}</div><div style="font-size:13px">порожніх комірок</div></div>
    </div>
    <div class="cells" style="grid-template-columns:repeat(auto-fill,minmax(96px,1fr))">
      @for (cb of cabs(); track cb.cb) {
        <div role="button" tabindex="0" style="cursor:pointer;padding:var(--space-2) var(--space-3)"
          [style.background]="cb.bg" [style.color]="cb.fg" [style.box-shadow]="cb.ring" (click)="selectCab(cb.cb)" (keydown.enter)="selectCab(cb.cb)">
          <div style="font-size:14px;font-weight:800">{{ cb.name }}</div>
          <div class="tnum" style="font-size:12px">{{ cb.count }}</div>
        </div>
      }
    </div>
    <div style="border:2px solid var(--color-text)">
      <div style="padding:var(--space-2) var(--space-3);border-bottom:2px solid var(--color-text);background:var(--color-accent);color:#fff;font-weight:800">Шафа {{ pad(cab()) }}</div>
      <div style="display:grid;gap:2px;background:var(--color-neutral-300)" [style.grid-template-columns]="'repeat(' + (vp.mob() ? 5 : 10) + ',minmax(0,1fr))'">
        @for (k of kcells(); track k.number) {
          <button type="button" class="cell" style="aspect-ratio:1/0.8;padding:4px" [title]="k.title"
            [style.background]="k.bg" [style.color]="k.fg" [style.box-shadow]="k.ring" (click)="selectCell(k.number)">
            <span class="cell-n">{{ k.n }}</span>
            <span class="cell-l">{{ k.label }}</span>
          </button>
        }
      </div>
    </div>
    <div class="legend">
      <div><span class="sw" style="background:var(--color-surface);border:1px solid var(--color-neutral-400)"></span>Ключ на місці</div>
      <div><span class="sw" style="background:var(--color-accent-2-300)"></span>Видано</div>
      <div><span class="sw" style="background:var(--color-bg);border:1px solid var(--color-neutral-400)"></span>Порожньо</div>
    </div>
  </section>

  <aside style="flex:1 1 340px;padding:var(--space-6);gap:var(--space-6)">
    @if (!sel()) {
      <div style="font-size:32px;font-weight:800;line-height:1.05;letter-spacing:-0.02em">Оберіть комірку в ключниці</div>
    } @else {
      @if (selCar(); as car) {
        <div style="display:flex;flex-direction:column;gap:var(--space-4)">
          <div>
            <div class="kicker">Комірка</div>
            <div style="font-size:28px;font-weight:800">{{ keyLabel(cab(), sel()) }}</div>
          </div>
          <div style="border-top:2px solid var(--color-text);padding-top:var(--space-4)">
            <div class="kicker">Автомобіль</div>
            <div style="font-size:32px;font-weight:800">Škoda {{ car.model }}</div>
            <div style="font-size:14px;letter-spacing:0.04em">VIN {{ car.vin }} · {{ car.color }}</div>
          </div>
          <div class="stats" style="grid-template-columns:1fr 1fr">
            <div>
              <div style="font-size:12px;color:var(--color-neutral-600)">Де стоїть</div>
              <div style="font-size:22px;font-weight:800">{{ car.zoneCode ? locLabel(car) : 'у дорозі' }}</div>
              <div style="font-size:12px">{{ car.zoneName }}</div>
            </div>
            <div>
              <div style="font-size:12px;color:var(--color-neutral-600)">Статус ключа</div>
              <div style="font-size:18px;font-weight:700">{{ selCell()?.status === 'Issued' ? 'Видано' : 'На місці' }}</div>
            </div>
          </div>
          @if (car.zoneCode) {
            <button class="btn btn-primary btn-block" (click)="showOnMap(car)">Показати авто на карті</button>
          }
        </div>
      } @else {
        <div style="display:flex;flex-direction:column;gap:var(--space-2)">
          <div class="kicker">Комірка</div>
          <div style="font-size:28px;font-weight:800">{{ keyLabel(cab(), sel()) }}</div>
          <div style="font-size:15px">Комірка порожня.</div>
        </div>
      }
    }
  </aside>
</main>
```

- [ ] **Step 2: Build and check in the browser**

Run: `cd frontend && npx ng build`. Expected: success.

Open `/keys` (it redirects to `/keys/1`) and check:
1. The 3 counters are shown, followed by 12 cabinet tiles («Шафа 01 · N / 100») and a 10-column grid.
2. Clicking a filled cell shows the car, «Де стоїть S1 · X», «Статус ключа На місці» and «Показати авто на карті». Clicking that button opens `/move/S1` with the car selected.
3. With a move in progress, that car's cell is mint with «видано», and its status is «Видано».
4. On the keys page, searching a VIN and picking the result opens its cabinet with the cell selected.
5. At 390 px the grid has 5 columns, and selecting a cell shows the bottom bar «На карті».

- [ ] **Step 3: Commit**

```bash
git add frontend
git commit -m "feat(web): shared key box page" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 12: «Журнал» page, README, final verification

**Files:**
- Modify (replace stub): `frontend/src/app/features/log/log-page.ts`
- Create: `frontend/src/app/features/log/log-page.html`, `README.md`

**Interfaces:**
- Consumes:
  - `Api.journal`, `Lightbox`, `Notify`, `pollEvery`.
  - From `format.ts`: `hms, locLabel, moveDuration, vinTail`.
- Produces: `LogPage`, `README.md`.

- [ ] **Step 1: Log page**

`frontend/src/app/features/log/log-page.ts`:
```ts
import { Component, inject, signal } from '@angular/core';
import { Api } from '../../core/api';
import { Lightbox } from '../../core/lightbox';
import { Move } from '../../core/models';
import { Notify } from '../../core/notify';
import { pollEvery } from '../../core/poll';
import { hms, locLabel, moveDuration, vinTail } from '../../shared/format';

/** Operations journal (last 200 completed moves). Prototype lines 408–426. */
@Component({ selector: 'app-log-page', templateUrl: './log-page.html' })
export class LogPage {
  private readonly api = inject(Api);
  private readonly lightbox = inject(Lightbox);
  private readonly notify = inject(Notify);

  protected readonly rows = signal<Move[]>([]);
  private loaded = false;
  protected readonly hms = hms;
  protected readonly locLabel = locLabel;
  protected readonly moveDuration = moveDuration;
  protected readonly vinTail = vinTail;

  constructor() {
    this.load();
    pollEvery(10_000, () => this.load());
  }

  private async load() {
    try {
      this.rows.set(await this.api.journal());
      this.loaded = true;
    } catch (e) {
      if (!this.loaded) this.notify.error(e);
    }
  }

  protected open(r: Move) {
    if (!r.photoUrl) return;
    this.lightbox.open(r.photoUrl,
      `Операція № ${r.no} · Škoda ${r.car.model} · ${locLabel(r.from)} → ${locLabel(r.to)}` +
      (r.completedAt ? ' · ' + hms(r.completedAt) : '') + ' · ' + r.operatorName);
  }
}
```

`frontend/src/app/features/log/log-page.html`:
```html
<main style="padding:var(--space-6);display:flex;flex-direction:column;gap:var(--space-4)">
  <div style="display:flex;align-items:baseline;gap:var(--space-4)">
    <div style="font-size:40px;font-weight:800;letter-spacing:-0.02em">Журнал операцій</div>
    <div style="font-size:14px;color:var(--color-neutral-600)">{{ rows().length }} записів</div>
  </div>
  <div style="display:flex;flex-direction:column;border-top:2px solid var(--color-text)">
    @for (r of rows(); track r.id) {
      <div class="row-line" style="gap:var(--space-2) var(--space-6);padding:var(--space-3) 0">
        <div style="flex:0 0 56px;height:42px;background:var(--color-surface)">
          @if (r.photoUrl) {
            <img [src]="r.photoUrl" alt="Фото постановки авто" title="Натисніть, щоб збільшити" (click)="open(r)"
              style="width:56px;height:42px;object-fit:cover;display:block;border:1px solid var(--color-divider);cursor:zoom-in">
          }
        </div>
        <div style="flex:1 1 220px;min-width:0">
          <div style="font-weight:800">№ {{ r.no }} · Škoda {{ r.car.model }} <span style="font-weight:400">…{{ vinTail(r.car.vin) }}</span></div>
          <div style="font-size:15px">{{ locLabel(r.from) }} → {{ locLabel(r.to) }}</div>
        </div>
        <div style="flex:0 1 170px;font-size:13px">{{ hms(r.startedAt) }} – {{ hms(r.completedAt!) }} · <b>{{ moveDuration(r) }}</b></div>
        <div style="flex:0 1 170px;font-size:13px">Ключ: {{ r.keyLabel }}</div>
        <div style="flex:0 1 120px;font-size:13px">{{ r.operatorName }}</div>
      </div>
    }
  </div>
</main>
```

- [ ] **Step 2: README**

`README.md`:
````markdown
# Євро Лоджистікс — демо обліку майданчика

Демо: Angular-клієнт 1:1 з макетом `docs/design/Логистика.dc.html` + C# API з мок-даними.

- Специфікація: `docs/superpowers/specs/2026-10-09-logistics-yard-design.md`
- План: `docs/superpowers/plans/2026-10-09-logistics-yard-demo.md`

## Запуск

Потрібно: .NET 10 SDK, Node.js 20+.

```bash
# 1. API → http://localhost:5080 (база data/eurocar.db створюється з демо-даними)
cd backend/Eurocar.Api
dotnet run

# 2. Клієнт → http://localhost:4200
cd frontend
npm install
npm start
```

Телефон або планшет у тій самій мережі: `npm start -- --host 0.0.0.0` і відкрити `http://<IP-комп’ютера>:4200`.

Скинути демо-дані: кнопка «Скинути демо-дані» внизу сторінки «Зони» або `curl -X POST http://localhost:5080/api/v1/demo/reset`.

## Тести

```bash
dotnet test backend
cd frontend && npx ng test --watch=false
```

## Що в демо є моком

- Залишки зовнішніх складів і дилерів генерує симулятор (`backend/Eurocar.Api/Integration/ExternalStock.cs`; вимкнути: `ExternalFeed:Enabled=false`).
- Оператор обирається зі списку, входу немає (далі — Microsoft Entra ID).
- «Google Карти» — просте посилання.
- Інтеграція з IT-Enterprise — наступний етап (інтерфейси вже є: `IExternalStockProvider`, `IPhotoStorage`, `ICurrentOperator`).
````

- [ ] **Step 3: Final verification**

Run (from the repo root):
```bash
dotnet test backend
cd frontend && npx ng test --watch=false && npx ng build && cd ..
```
Expected: 41 backend tests pass, the frontend specs pass, and the production build succeeds.

Then reset the demo and walk the whole design with both apps running:
1. `/zones` matches the prototype's «Зони» tab.
2. Complete one full move with a photo (Task 10, Step 4, items 1–6).
3. `/log` shows «№ 4» on top with a thumbnail. Clicking it opens the lightbox with the caption. Records 1–3 from the seed are below.
4. `/keys` shows the moved car's cell as «На місці».
5. Open two browser windows with different operators (select box in the panel) and start moves in both. Target the same free spot in both: the second gets «Місце … зайняте.» (Review Focus #2).
6. Stop the API: the offline banner appears and buttons dim. Start it again: the banner disappears within 10 s.
7. Repeat items 1–3 at 390 px width.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat(web): operations journal page and README" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
