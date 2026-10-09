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
