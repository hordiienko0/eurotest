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
