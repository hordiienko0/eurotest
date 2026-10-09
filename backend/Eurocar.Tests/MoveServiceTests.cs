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

    [Fact]
    public async Task Operator_cannot_start_second_move_while_one_is_in_transit()
    {
        await _svc.StartAsync(_car1, Op1);
        Assert.Equal("ActiveMoveExists", await CodeOf(() => _svc.StartAsync(_car2, Op1)));
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
