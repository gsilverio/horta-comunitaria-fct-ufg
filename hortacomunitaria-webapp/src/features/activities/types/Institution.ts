import type { CatalogState } from "../../../types/records"
export interface Institution extends CatalogState {
  name: string;
  acronym: string | null;
  type: string | null;
}
export interface InstitutionValues {
  name: string;
  acronym: string | null;
  type: string | null;
}
