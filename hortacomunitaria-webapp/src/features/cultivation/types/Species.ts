import type { CatalogState } from "../../../types/records"
import type { VisualGrowthValues } from './VisualGrowth'
export interface Species extends CatalogState, VisualGrowthValues {
  imageUrl: string | null;
  commonName: string;
  scientificName: string | null;
  description: string | null;
}
export interface SpeciesValues {
  commonName: string;
  scientificName: string | null;
  description: string | null;
}
