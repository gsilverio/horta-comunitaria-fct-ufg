import { useRef, useState } from 'react'
import { apiFailure } from '../../../services/http'
import { releaseNavigationGuard } from '../../../services/navigationGuard'
import type { ApiFailure } from '../../../types/api'
import { FormFailure } from '../../irrigation/formValues'
import { bedContextApi } from '../../organization/services/bedContextApi'
import { cultivationCycleApi } from '../../cultivation/services/CultivationCycleApi'
import { plantingApi } from '../../cultivation/services/PlantingApi'
import type { CultivationCycle } from '../../cultivation/types/CultivationCycle'

export interface InteractivePlantingValues {
  bedId: string; speciesId: string; cultivarId: string | null; on: string
  bedAreaM2: number | null; occupiedAreaM2: number; quantity: number
  responsibleParticipantId: string | null; propagationLotId: string | null
  plantSpacingCm: number | null; rowSpacingCm: number | null
}
interface Attempt {
  values: InteractivePlantingValues
  cycleOperationId: string
  plantingOperationId: string
  cycle?: CultivationCycle
  configured: boolean
  checked: boolean
}

/** Retoma as etapas confirmadas e mantém as chaves originais se a resposta se perder. */
export function useInteractivePlanting(garden: string, saved: () => void) {
  const attempt = useRef<Attempt | undefined>(undefined), sending = useRef(false)
  const [busy, setBusy] = useState(false), [pending, setPending] = useState(false)
  const [error, setError] = useState<ApiFailure>(), [partialCycle, setPartialCycle] = useState<string>()
  const [progress, setProgress] = useState('')
  async function submit(values: () => InteractivePlantingValues) {
    if (sending.current || partialCycle) return
    sending.current = true; setBusy(true); setError(undefined)
    try {
      attempt.current ??= { values: values(), cycleOperationId: crypto.randomUUID(), plantingOperationId: crypto.randomUUID(), configured: false, checked: false }
      setPending(true)
      const current = attempt.current, v = current.values
      if (!current.checked) {
      setProgress('Conferindo área e setor na data do plantio…')
      const bed = await bedContextApi.on(garden, v.bedId, v.on, new AbortController().signal)
      if (!bed.isActive || !bed.sectorAssignment) throw new FormFailure({ on: ['O canteiro precisa estar ativo e associado a um setor nesta data.'] })
      if (!bed.configuration) {
        if (v.bedAreaM2 === null) throw new FormFailure({ cultivableAreaM2: ['Informe a área real do canteiro nesta data.'] })
        setProgress('Salvando área real do canteiro…')
        await bedContextApi.configure(garden, bed.id, { cultivableAreaM2: v.bedAreaM2, lengthMeters: null, widthMeters: null, validFrom: v.on, expectedBedVersion: bed.version, reason: null })
        current.configured = true
      } else if (v.bedAreaM2 !== null && bed.configuration.cultivableAreaM2 !== v.bedAreaM2) {
        throw new FormFailure({ cultivableAreaM2: ['A área cadastrada mudou. Recarregue o canteiro e revise os valores.'] })
      }
      current.checked = true
      }
      if (!current.cycle) {
        setProgress('Salvando espécie e área ocupada…')
        current.cycle = await cultivationCycleApi.create(garden, { bedId: v.bedId, speciesId: v.speciesId, cultivarId: v.cultivarId, startedOn: v.on, occupiedAreaM2: v.occupiedAreaM2, endedOn: null, responsibleParticipantId: v.responsibleParticipantId }, current.cycleOperationId)
      }
      setProgress('Registrando quantidade plantada…')
      await plantingApi.create(garden, { cycleId: current.cycle.id, plantedOn: v.on, kind: 'Initial', plantedQuantity: v.quantity, propagationLotId: v.propagationLotId, replacedPlantingId: null, plantSpacingCm: v.plantSpacingCm, rowSpacingCm: v.rowSpacingCm, responsibleParticipantId: v.responsibleParticipantId }, current.plantingOperationId)
      attempt.current = undefined; setPending(false); releaseNavigationGuard(); saved()
    } catch (failure) {
      const normalized: ApiFailure = failure instanceof FormFailure ? { message: failure.message, fields: failure.fields } : apiFailure(failure)
      setError(normalized)
      const definitive = failure instanceof FormFailure || normalized.status !== undefined && normalized.status >= 400 && normalized.status < 500
      if (definitive) {
        if (attempt.current?.cycle) {
          setPartialCycle(attempt.current.cycle.id); setPending(false)
          setProgress('O cultivo foi salvo. Revise o registro do plantio para concluir sem criar outro cultivo.')
        } else {
          setProgress(attempt.current?.configured ? 'A área foi salva. Revise os dados para continuar o plantio.' : '')
          attempt.current = undefined; setPending(false)
        }
      }
    } finally { sending.current = false; setBusy(false) }
  }
  return { submit, busy, pending, error, progress, partialCycle }
}
