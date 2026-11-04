import type { CatalogState } from "../../../types/records"
export interface AgriculturalInput extends CatalogState {
  name: string;
  category: string | null;
  referenceUnit: string;
}
export interface AgriculturalInputValues {
  name: string;
  category: string | null;
  referenceUnit: string;
}
