import type { FactState } from "../../../types/records"
export interface AgronomicObservation extends FactState {
  cycleId: string;
  observedOn: string;
  category: string;
  description: string;
  measurement: number | null;
  unit: string | null;
  method: string | null;
  responsibleParticipantId: string | null;
}
export interface AgronomicObservationValues {
  cycleId: string;
  observedOn: string;
  category: string;
  description: string;
  measurement: number | null;
  unit: string | null;
  method: string | null;
  responsibleParticipantId: string | null;
}
