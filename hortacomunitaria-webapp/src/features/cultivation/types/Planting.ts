import type { PlantingKind } from "../../../types/enums"
import type { FactState } from "../../../types/records"
export interface Planting extends FactState {
  cycleId: string;
  propagationLotId: string | null;
  replacedPlantingId: string | null;
  plantedOn: string;
  kind: PlantingKind;
  plantedQuantity: number;
  plantSpacingCm: number | null;
  rowSpacingCm: number | null;
  responsibleParticipantId: string | null;
}
export interface PlantingValues {
  cycleId: string;
  propagationLotId: string | null;
  replacedPlantingId: string | null;
  plantedOn: string;
  kind: PlantingKind;
  plantedQuantity: number;
  plantSpacingCm: number | null;
  rowSpacingCm: number | null;
  responsibleParticipantId: string | null;
}
