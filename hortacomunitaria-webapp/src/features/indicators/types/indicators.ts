import type { Page } from "../../../types/api"
import type { PlantingKind, DryingStatus, MeasurementQuality, OccurrenceStatus, FinancialNature, IndicatorAvailability } from "../../../types/enums"
export interface GetSoilMoisture { from: string; to: string; timeZone: string; definition: string; normalCount: number; questionableCount: number; availability: IndicatorAvailability; origins: Page<MoistureObservation>; groups: Page<MoistureGroup> }
export interface MoistureGroup { pointId: string; unit: string; method: string; count: number; normalCount: number; questionableCount: number; lastObservation: MoistureObservation | null }
export interface MoistureObservation { id: string; pointId: string; collectedAt: string; value: number; unit: string; method: string; quality: MeasurementQuality }
export interface GetSoilMoistureFilters { from?: string; to?: string; pointId?: string; unit?: string; method?: string; page: number; pageSize: number }
export interface EstablishmentGroup { kind: PlantingKind; method: string | null; plantingCount: number; assessedCount: number; assessedPlantedQuantity: number; survivingQuantity: number; percentage: number | null }
export interface EstablishmentOrigin { plantingId: string; kind: PlantingKind; plantedQuantity: number; assessmentId: string | null; assessedOn: string | null; survivingQuantity: number | null; method: string | null; percentage: number | null }
export interface GetSeedlingEstablishment { from: string; to: string; definition: string; eligiblePlantingCount: number; ineligibleMaterialCount: number; withoutAssessmentCount: number; availability: IndicatorAvailability; origins: Page<EstablishmentOrigin>; groups: Page<EstablishmentGroup> }
export interface GetSeedlingEstablishmentFilters { from?: string; to?: string; speciesId?: string; kind?: string; method?: string; page: number; pageSize: number }
export interface CycleProductivity { cycleId: string; speciesId: string; occupiedAreaM2: number; freshMassKg: number; harvestCount: number; productivityKgPerM2: number }
export interface GetProductivity { from: string; to: string; definition: string; freshMassKg: number | null; harvestCount: number; availability: IndicatorAvailability; origins: Page<CycleProductivity> }
export interface GetProductivityFilters { from?: string; to?: string; cycleId?: string; speciesId?: string; page: number; pageSize: number }
export interface GetHarvestLosses { from: string; to: string; unit: string; knownLossKg: number | null; percentage: number | null; knownCount: number; withoutUsableMassCount: number; availability: IndicatorAvailability; origins: Page<LossOrigin> }
export interface LossOrigin { id: string; cycleId: string; harvestedOn: string; freshMassKg: number; usableMassKg: number | null; lossMassKg: number | null }
export interface GetHarvestLossesFilters { from?: string; to?: string; cycleId?: string; speciesId?: string; page: number; pageSize: number }
export interface DryingOrigin { id: string; code: string; startedOn: string; status: DryingStatus; allocatedMassKg: number; totalDryMassKg: number | null; mixedSpecies: boolean; eligible: boolean; percentage: number | null }
export interface GetDryingYield { from: string; to: string; definition: string; knownDryMassKg: number | null; eligibleAllocatedMassKg: number | null; percentage: number | null; eligibleCount: number; incompleteCount: number; mixedSpeciesCount: number; availability: IndicatorAvailability; origins: Page<DryingOrigin> }
export interface GetDryingYieldFilters { from?: string; to?: string; lotId?: string; speciesId?: string; page: number; pageSize: number }
export interface GetWorkHours { from: string; to: string; unit: string; knownHours: number | null; activityCount: number; knownParticipationCount: number; unknownParticipationCount: number; activitiesWithoutParticipants: number; availability: IndicatorAvailability; origins: Page<WorkOrigin> }
export interface WorkOrigin { activityId: string; performedOn: string; participantId: string; workHours: number | null }
export interface GetWorkHoursFilters { from?: string; to?: string; participantId?: string; category?: string; page: number; pageSize: number }
export interface GetOperationalOccurrences { from: string; to: string; timeZone: string; count: number; openCount: number; inProgressCount: number; resolvedCount: number; availability: IndicatorAvailability; origins: Page<OccurrenceOrigin> }
export interface OccurrenceOrigin { id: string; openedAt: string; category: string; status: OccurrenceStatus }
export interface GetOperationalOccurrencesFilters { from?: string; to?: string; category?: string; status?: string; page: number; pageSize: number }
export interface AudienceOrigin { activityId: string; performedOn: string; isSocial: boolean; isEducational: boolean; nominalPresences: number; knownAudience: number | null; audienceListComplete: boolean | null }
export interface GetSocialEducationalParticipation { from: string; to: string; definition: string; activityCount: number; socialCount: number; educationalCount: number; knownAudience: number | null; eventsWithoutAudienceTotal: number; nominalPresences: number; identifiedPeople: number; availability: IndicatorAvailability; origins: Page<AudienceOrigin> }
export interface GetSocialEducationalParticipationFilters { from?: string; to?: string; dimension?: string; institutionId?: string; page: number; pageSize: number }
export interface CostOrigin { id: string; entryDate: string; category: string; nature: FinancialNature; amountBrl: number; bedId: string | null; cycleId: string | null }
export interface GetBasicCosts { from: string; to: string; currency: string; plannedBrl: number | null; actualBrl: number | null; differenceBrl: number | null; plannedCount: number; actualCount: number; comparison: string; availability: IndicatorAvailability; origins: Page<CostOrigin> }
export interface GetBasicCostsFilters { from?: string; to?: string; category?: string; bedId?: string; cycleId?: string; page: number; pageSize: number }
