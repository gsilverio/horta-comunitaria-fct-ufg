import type { CatalogState } from "../../../types/records"
export interface Participant extends CatalogState {
  gardenId: string;
  displayName: string;
  category: string | null;
}
export interface ParticipantValues {
  displayName: string;
  category: string | null;
}
