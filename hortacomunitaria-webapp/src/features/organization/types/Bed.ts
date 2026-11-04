import type { CatalogState } from "../../../types/records"
export interface Bed extends CatalogState {
  gardenId: string;
  code: string;
  name: string;
  location: string | null;
  on: string | null; configuration: BedConfiguration | null; sectorAssignment: BedAssignment | null;
}
export interface BedValues {
  code?: string;
  name: string;
  location: string | null;
}
export interface BedConfiguration { id: string; gardenId: string; bedId: string; lengthMeters: number | null; widthMeters: number | null; cultivableAreaM2: number; validFrom: string; validUntil: string | null }
export interface BedAssignment { id: string; gardenId: string; bedId: string; sectorId: string; validFrom: string; validUntil: string | null }
