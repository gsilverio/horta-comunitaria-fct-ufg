import type { OccurrenceStatus } from "../../../types/enums"
import type { FactState } from "../../../types/records"
export interface Occurrence extends FactState {
  openedAt: string;
  category: string;
  description: string;
  status: OccurrenceStatus;
  resolvedAt: string | null;
  resolution: string | null;
  responsibleParticipantId: string | null;
  bedId: string | null;
  sectorId: string | null;
  cycleId: string | null;
  irrigationId: string | null;
}
export interface OccurrenceValues {
  openedAt: string;
  category: string;
  description: string;
  status: OccurrenceStatus;
  resolvedAt: string | null;
  resolution: string | null;
  responsibleParticipantId: string | null;
  bedId: string | null;
  sectorId: string | null;
  cycleId: string | null;
  irrigationId: string | null;
}
