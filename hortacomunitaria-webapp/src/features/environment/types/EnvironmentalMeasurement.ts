import type { MeasurementQuality } from "../../../types/enums"
import type { FactState } from "../../../types/records"
export interface EnvironmentalMeasurement extends FactState {
  samplingPointId: string;
  variableId: string;
  collectedAt: string;
  value: number;
  unit: string;
  method: string;
  quality: MeasurementQuality;
  responsibleParticipantId: string | null;
  notes: string | null;
  bedId: string | null;
  sectorId: string | null;
  bedSectorAssignmentId: string | null;
}
export interface EnvironmentalMeasurementValues {
  samplingPointId: string;
  variableId: string;
  collectedAt: string;
  value: number;
  unit: string;
  method: string;
  quality: MeasurementQuality;
  responsibleParticipantId: string | null;
  notes: string | null;
}
