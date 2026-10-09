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
