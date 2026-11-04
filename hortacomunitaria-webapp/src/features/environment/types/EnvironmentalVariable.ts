import type { CatalogState } from "../../../types/records"
export interface EnvironmentalVariable extends CatalogState {
  code: string;
  isSoilMoisture: boolean;
  name: string;
  quantity: string;
  referenceUnit: string;
  description: string | null;
}
export interface EnvironmentalVariableValues {
  code?: string;
  isSoilMoisture: boolean;
  name: string;
  quantity: string;
  referenceUnit: string;
  description: string | null;
}
