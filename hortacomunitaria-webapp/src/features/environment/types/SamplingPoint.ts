import type { CatalogState } from "../../../types/records"
export interface SamplingPoint extends CatalogState {
  gardenId: string;
  code: string;
  description: string;
  location: string | null;
  bedId: string | null;
  sectorId: string | null;
}
export interface SamplingPointValues {
  code?: string;
  description: string;
  location: string | null;
  bedId: string | null;
  sectorId: string | null;
}
