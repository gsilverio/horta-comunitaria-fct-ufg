import type { CatalogState } from "../../../types/records"
export interface IrrigationSector extends CatalogState {
  gardenId: string;
  code: string;
  name: string;
  description: string | null;
}
export interface IrrigationSectorValues {
  code?: string;
  name: string;
  description: string | null;
}
