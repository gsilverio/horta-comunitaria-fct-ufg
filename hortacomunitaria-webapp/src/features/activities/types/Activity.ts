import type { FactState } from "../../../types/records"
export interface ActivityParticipant { participantId: string; role: string | null; workHours: number | null; countsAsAudience: boolean }
export interface InputUsage { id: string; inputId: string; quantity: number; unit: string }
export interface ActivityInstitution { institutionId: string; role: string | null }
export interface Activity extends FactState {
  title: string;
  category: string;
  performedOn: string;
  description: string | null;
  bedId: string | null;
  sectorId: string | null;
  cycleId: string | null;
  occurrenceId: string | null;
  isOperational: boolean;
  isSocial: boolean;
  isEducational: boolean;
  objective: string | null;
  modality: string | null;
  reportedAudienceTotal: number | null;
  audienceListComplete: boolean | null;
  participants: ActivityParticipant[];
  inputUsages: Omit<InputUsage, "id">[];
  institutions: ActivityInstitution[];
}
export interface ActivityValues {
  title: string;
  category: string;
  performedOn: string;
  description: string | null;
  bedId: string | null;
  sectorId: string | null;
  cycleId: string | null;
  occurrenceId: string | null;
  isOperational: boolean;
  isSocial: boolean;
  isEducational: boolean;
  objective: string | null;
  modality: string | null;
  reportedAudienceTotal: number | null;
  audienceListComplete: boolean | null;
  participants: ActivityParticipant[];
  inputUsages: Omit<InputUsage, "id">[];
  institutions: ActivityInstitution[];
}
