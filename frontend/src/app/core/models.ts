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
