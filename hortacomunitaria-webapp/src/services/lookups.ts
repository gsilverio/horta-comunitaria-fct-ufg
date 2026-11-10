import { enumLabels } from './labels'
import { allPages } from "./pagination"
import type { Choice } from "../types/records"
import { formatInstant, formatNumber } from "../features/irrigation/formValues"
import type { AgriculturalInput } from "../features/activities/types/AgriculturalInput"
import type { Bed } from "../features/organization/types/Bed"
import type { Cultivar } from "../features/cultivation/types/Cultivar"
import type { CultivationCycle } from "../features/cultivation/types/CultivationCycle"
import type { DryingLot } from "../features/production/types/DryingLot"
import type { EnvironmentalVariable } from "../features/environment/types/EnvironmentalVariable"
import type { Harvest } from "../features/production/types/Harvest"
import type { Institution } from "../features/activities/types/Institution"
import type { IrrigationSector } from "../features/organization/types/IrrigationSector"
import type { Occurrence } from "../features/occurrences/types/Occurrence"
import type { Participant } from "../features/activities/types/Participant"
import type { Planting } from "../features/cultivation/types/Planting"
import type { PropagationLot } from "../features/cultivation/types/PropagationLot"
import type { SamplingPoint } from "../features/environment/types/SamplingPoint"
import type { Species } from "../features/cultivation/types/Species"
import type { IrrigationSummary } from "../features/irrigation/types/irrigation"
export interface LookupChoice extends Choice { speciesId?: string; cultivarId?: string | null; cycleId?: string | null; bedId?: string | null; referenceUnit?: string; usableMassKg?: number | null }
export type LookupKey = 'agriculturalInput' | 'bed' | 'cultivar' | 'cultivationCycle' | 'dryingLot' | 'environmentalVariable' | 'harvest' | 'institution' | 'irrigation' | 'irrigationSector' | 'occurrence' | 'participant' | 'planting' | 'propagationLot' | 'samplingPoint' | 'species'
export function choiceLabel(choices: Choice[] | undefined, id: string | null | undefined) { return id ? choices?.find(choice => choice.id === id)?.label : undefined }
export const lookupApi = { async load(garden: string, key: string, signal: AbortSignal): Promise<Partial<Record<LookupKey, LookupChoice[]>>> {
const result: Partial<Record<LookupKey, LookupChoice[]>> = {}
await Promise.all(key.split("|").map(async item => { switch(item) {
case 'agriculturalInput': result.agriculturalInput = (await allPages<AgriculturalInput>('/inputs', signal, {})).map(x => ({ ...x, label: x.name })); break
case 'bed': result.bed = (await allPages<Bed>(`/gardens/${garden}/beds/catalog`, signal, {})).map(x => ({ ...x, label: x.name })); break
case "cultivar": { const parents = await allPages<Species>("/species", signal); const lists = await Promise.all(parents.map(parent => allPages<Cultivar>(`/species/${parent.id}/cultivars`, signal))); result.cultivar = lists.flat().map(x => ({ ...x, label: x.name })); break }
case 'cultivationCycle': {
  const [cycles, beds, species] = await Promise.all([
    allPages<CultivationCycle>(`/gardens/${garden}/cultivation-cycles`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' }),
    allPages<Bed>(`/gardens/${garden}/beds/catalog`, signal), allPages<Species>('/species', signal),
  ])
  const bedNames = new Map(beds.map(item => [item.id, item.name]))
  const speciesNames = new Map(species.map(item => [item.id, item.commonName]))
  result.cultivationCycle = cycles.map(item => ({ ...item, label: `${speciesNames.get(item.speciesId) ?? 'Espécie histórica'} · ${bedNames.get(item.bedId) ?? 'Canteiro histórico'} · ${item.startedOn.split('-').reverse().join('/')} · ${enumLabels[item.status]}` }))
  break
}
case 'dryingLot': result.dryingLot = (await allPages<DryingLot>(`/gardens/${garden}/drying-lots`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' })).map(x => ({ ...x, label: (x.method ?? "Secagem") + " · " + x.startedOn })); break
case 'environmentalVariable': result.environmentalVariable = (await allPages<EnvironmentalVariable>('/environmental-variables', signal, {})).map(x => ({ ...x, label: x.name })); break
case 'harvest': result.harvest = (await allPages<Harvest>(`/gardens/${garden}/harvests`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' })).map(x => ({ ...x, label: x.harvestedOn + " · " + formatNumber(x.freshMassKg) + " kg · aproveitável: " + (x.usableMassKg === null ? "não medida" : formatNumber(x.usableMassKg) + " kg") + " · " + x.id.slice(-6) })); break
case 'institution': result.institution = (await allPages<Institution>('/institutions', signal, {})).map(x => ({ ...x, label: x.name })); break
case 'irrigation': result.irrigation = (await allPages<IrrigationSummary>(`/gardens/${garden}/irrigations`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' })).map(x => ({ ...x, label: x.sectorName + " · " + formatInstant(x.startedAt) })); break
case 'irrigationSector': result.irrigationSector = (await allPages<IrrigationSector>(`/gardens/${garden}/sectors/catalog`, signal, {})).map(x => ({ ...x, label: x.name })); break
case 'occurrence': result.occurrence = (await allPages<Occurrence>(`/gardens/${garden}/occurrences`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' })).map(x => ({ ...x, label: x.category + " · " + formatInstant(x.openedAt) + " · " + enumLabels[x.status] })); break
case 'participant': result.participant = (await allPages<Participant>(`/gardens/${garden}/participants/catalog`, signal, {})).map(x => ({ ...x, label: x.displayName })); break
case 'planting': result.planting = (await allPages<Planting>(`/gardens/${garden}/plantings`, signal, { from: '0001-01-01', to: '9998-12-31', validity: 'Valid' })).map(x => ({ ...x, label: x.plantedOn + " · " + enumLabels[x.kind] + " · " + x.plantedQuantity + " plantas · " + x.id.slice(-6) })); break
case 'propagationLot': result.propagationLot = (await allPages<PropagationLot>(`/gardens/${garden}/propagation-lots`, signal, {})).map(x => ({ ...x, label: (x.origin ?? "Origem não informada") + " · " + enumLabels[x.materialType] + " · " + (x.receivedOn ?? "Data não informada") })); break
case 'samplingPoint': result.samplingPoint = (await allPages<SamplingPoint>(`/gardens/${garden}/sampling-points`, signal, {})).map(x => ({ ...x, label: x.description })); break
case 'species': result.species = (await allPages<Species>('/species', signal, {})).map(x => ({ ...x, label: x.commonName })); break
} })); signal.throwIfAborted(); return result } }

