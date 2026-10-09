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
