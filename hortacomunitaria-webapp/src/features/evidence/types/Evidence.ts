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
export const evidenceTargets: Record<EvidenceTarget, { label: string; module: string; resource: string }> = {
  Bed: { label: 'Canteiro', module: 'horta', resource: 'canteiros' },
  CultivationCycle: { label: 'Ciclo de cultivo', module: 'cultivo', resource: 'ciclos' },
  Planting: { label: 'Plantio / reposição', module: 'cultivo', resource: 'plantios' },
  EstablishmentAssessment: { label: 'Avaliação de pegamento', module: 'cultivo', resource: 'avaliacoes' },
  AgronomicObservation: { label: 'Observação agronômica', module: 'cultivo', resource: 'observacoes' },
  Harvest: { label: 'Colheita', module: 'producao', resource: 'colheitas' },
  DryingLot: { label: 'Secagem', module: 'producao', resource: 'secagens' },
  Activity: { label: 'Atividade', module: 'atividades', resource: 'registros' },
  Occurrence: { label: 'Ocorrência', module: 'ocorrencias', resource: 'registros' },
}
