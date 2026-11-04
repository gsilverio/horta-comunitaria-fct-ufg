import type { CatalogState } from "../../../types/records"
import type { VisualGrowthValues } from './VisualGrowth'
export interface Cultivar extends CatalogState, VisualGrowthValues {
  speciesId: string;
  name: string;
  description: string | null;
}
export interface CultivarValues {
  name: string;
  description: string | null;
}
