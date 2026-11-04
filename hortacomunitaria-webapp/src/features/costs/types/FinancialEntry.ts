import type { FinancialNature } from "../../../types/enums"
import type { FactState } from "../../../types/records"
export interface FinancialEntry extends FactState {
  entryDate: string;
  description: string;
  category: string;
  amountBrl: number;
  nature: FinancialNature;
  bedId: string | null;
  cycleId: string | null;
}
export interface FinancialEntryValues {
  entryDate: string;
  description: string;
  category: string;
  amountBrl: number;
  nature: FinancialNature;
  bedId: string | null;
  cycleId: string | null;
}
