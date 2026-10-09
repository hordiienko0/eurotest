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
