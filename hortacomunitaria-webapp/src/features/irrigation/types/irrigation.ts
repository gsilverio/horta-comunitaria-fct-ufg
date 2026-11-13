export interface CatalogItem {
  id: string;
  code: string;
  name: string;
}
export interface Bed extends CatalogItem {
  assignmentId: string;
}
export interface Participant {
  id: string;
  displayName: string;
}
export type Status = "Completed" | "Interrupted";
export type Validity = "Valid" | "Invalid";
export type VolumeSource = "Measured" | "Estimated";
export interface IrrigationValues {
  sectorId: string;
  bedIds: string[];
  startedAt: string;
  endedAt: string | null;
  activeDurationMinutes: number | null;
  volumeLiters: number | null;
  /** Somente no registro: preço do m³ (R$) para o servidor lançar o custo da água. */
  waterPricePerCubicMeterBrl?: number | null;
  flowRateLitersPerMinute: number | null;
  volumeSource: VolumeSource | null;
  volumeMethod: string | null;
  status: Status;
  responsibleParticipantId: string | null;
  notes: string | null;
}
export interface IrrigationSummary {
  id: string;
  sectorId: string;
  sectorName: string;
  startedAt: string;
  volumeLiters: number | null;
  status: Status;
  validity: Validity;
  version: number;
}
export interface Actor {
  id: string;
  displayName: string;
  isDemonstration: boolean;
}
export interface IrrigationDetail extends IrrigationSummary, IrrigationValues {
  recordedAt: string;
  updatedAt: string | null;
  recordedBy: Actor;
  beds: Bed[];
}
export interface Snapshot extends Omit<IrrigationValues, "bedIds"> {
  id: string;
  gardenId: string;
  validity: Validity;
  version: number;
  recordedByUserId: string;
  recordedAt: string;
  updatedAt: string | null;
  beds: { bedId: string; assignmentId: string }[];
}
export interface HistoryEntry {
  id: string;
  operation: "Created" | "Corrected" | "Invalidated";
  reason: string | null;
  occurredAt: string;
  actor: Actor;
  before: Snapshot | null;
  after: Snapshot;
}
export interface Coverage {
  knownVolumeLiters: number | null;
  eventCount: number;
  eventsWithKnownVolume: number;
  eventsWithoutVolume: number;
  availability: "NoData" | "Unavailable" | "Partial" | "Available";
  includesEstimatedVolume: boolean;
}
export interface ExecutionCoverage {
  completedCount: number; interruptedCount: number; knownActiveDurationMinutes: number | null;
  eventsWithKnownDuration: number; eventsWithoutDuration: number; durationAvailability: Coverage['availability'];
}
export interface SectorConsumption extends Coverage, ExecutionCoverage {
  sectorId: string;
  code: string;
  name: string;
}
export interface Consumption {
  from: string;
  to: string;
  timeZone: string;
  unit: string;
  sectorId: string | null;
  definition: string;
  summary: Coverage;
  executionSummary: ExecutionCoverage;
  bySector: import("../../../types/api").Page<SectorConsumption>;
}
export interface Filters {
  from: string;
  to: string;
  sectorId: string;
  validity: Validity;
}
