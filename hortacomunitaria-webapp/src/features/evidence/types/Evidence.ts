import type { GardenPage } from '../../../routes/routes'
export type EvidenceTarget = 'Bed' | 'CultivationCycle' | 'Planting' | 'EstablishmentAssessment' | 'AgronomicObservation' | 'Harvest' | 'DryingLot' | 'Activity' | 'Occurrence'

/** Metadados retornados pela API; o conteúdo vem da rota de conteúdo. */
export interface Evidence {
  id: string; gardenId: string; operationId: string
  targetType: EvidenceTarget; targetId: string
  title: string; description: string | null; capturedOn: string | null
  originalFileName: string; contentType: string; sizeBytes: number
  recordedByUserId: string; recordedAt: string; updatedAt: string | null
  version: number; validity: 'Valid' | 'Invalid'
}

export interface EvidenceUpload { file: Blob; fileName: string; targetType: EvidenceTarget; targetId: string; title: string; description: string | null; capturedOn: string | null }

/** Rótulo e destino na navegação de cada tipo de registro. */
export const evidenceTargets: Record<EvidenceTarget, { label: string; page: GardenPage }> = {
  Bed: { label: 'Canteiro', page: 'horta/canteiros' },
  CultivationCycle: { label: 'Ciclo de cultivo', page: 'cultivo/ciclos' },
  Planting: { label: 'Plantio / reposição', page: 'cultivo/plantios' },
  EstablishmentAssessment: { label: 'Avaliação de pegamento', page: 'cultivo/avaliacoes' },
  AgronomicObservation: { label: 'Observação agronômica', page: 'cultivo/observacoes' },
  Harvest: { label: 'Colheita', page: 'producao/colheitas' },
  DryingLot: { label: 'Secagem', page: 'producao/secagens' },
  Activity: { label: 'Atividade', page: 'atividades/registros' },
  Occurrence: { label: 'Ocorrência', page: 'ocorrencias' },
}
