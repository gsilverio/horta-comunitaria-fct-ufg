import type { CultivationStatus } from "../../../types/enums"
import type { FactState } from "../../../types/records"
export interface CultivationCycle extends FactState {
  bedId: string;
  speciesId: string;
  cultivarId: string | null;
  startedOn: string;
  occupiedAreaM2: number;
  endedOn: string | null;
  responsibleParticipantId: string | null;
  bedConfigurationId: string;
  referenceAreaM2: number;
  status: CultivationStatus;
}
export interface CultivationCycleValues {
  bedId: string;
  speciesId: string;
  cultivarId: string | null;
  startedOn: string;
  occupiedAreaM2: number;
  endedOn: string | null;
  responsibleParticipantId: string | null;
}
