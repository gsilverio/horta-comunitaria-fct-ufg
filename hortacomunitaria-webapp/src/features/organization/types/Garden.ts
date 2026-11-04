import type { CatalogState } from "../../../types/records"
export interface Garden extends CatalogState {
  code: string;
  name: string;
  location: string | null;
  institutionalAreaM2: number | null;
  operationalAreaM2: number | null;
  experimentalAreaM2: number | null;
}
export interface GardenValues {
  code?: string;
  name: string;
  location: string | null;
  institutionalAreaM2: number | null;
  operationalAreaM2: number | null;
  experimentalAreaM2: number | null;
}
