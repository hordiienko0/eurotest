namespace Eurocar.Api.Domain;

public enum ZoneKind { Mapped, External }
public enum Priority { High, Normal, Low }
public enum CarStatus { Parked, InTransit, AtExternal }
public enum KeySlotStatus { Empty, InBox, Issued }
public enum MoveState { InTransit, Completed, Cancelled }
public enum CellLabelMode { Vin, Model }
