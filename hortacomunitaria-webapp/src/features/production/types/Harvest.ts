import type { FactState } from "../../../types/records"
export interface Harvest extends FactState {
  cycleId: string;
  harvestedOn: string;
  freshMassKg: number;
  usableMassKg: number | null;
  lossReason: string | null;
  responsibleParticipantId: string | null;
  allocatedMassKg: number | null; remainingUsableMassKg: number | null;
}
export interface HarvestValues {
  cycleId: string;
  harvestedOn: string;
  freshMassKg: number;
  usableMassKg: number | null;
  lossReason: string | null;
  responsibleParticipantId: string | null;
}
