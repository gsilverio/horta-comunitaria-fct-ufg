import type { DryingStatus } from "../../../types/enums"
import type { FactState } from "../../../types/records"
export interface DryingItem { harvestId: string; allocatedMassKg: number }
export interface DryingLot extends FactState {
  code: string;
  method: string | null;
  startedOn: string;
  endedOn: string | null;
  totalDryMassKg: number | null;
  responsibleParticipantId: string | null;
  items: DryingItem[];
  status: DryingStatus;
}
export interface DryingLotValues {
  code?: string;
  method: string | null;
  startedOn: string;
  endedOn: string | null;
  totalDryMassKg: number | null;
  responsibleParticipantId: string | null;
  items: DryingItem[];
}
