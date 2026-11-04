import type { FactState } from "../../../types/records"
export interface EstablishmentAssessment extends FactState {
  plantingId: string;
  assessedOn: string;
  survivingQuantity: number;
  method: string;
  notes: string | null;
  responsibleParticipantId: string | null;
}
export interface EstablishmentAssessmentValues {
  plantingId: string;
  assessedOn: string;
  survivingQuantity: number;
  method: string;
  notes: string | null;
  responsibleParticipantId: string | null;
}
