import type { MaterialType } from "../../../types/enums"
import type { CatalogState } from "../../../types/records"
export interface PropagationLot extends CatalogState {
  gardenId: string;
  code: string;
  speciesId: string;
  cultivarId: string | null;
  materialType: MaterialType;
  origin: string | null;
  receivedOn: string | null;
  notes: string | null;
}
export interface PropagationLotValues {
  code?: string;
  speciesId: string;
  cultivarId: string | null;
  materialType: MaterialType;
  origin: string | null;
  receivedOn: string | null;
  notes: string | null;
}
