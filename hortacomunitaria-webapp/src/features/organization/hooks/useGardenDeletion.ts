import { useRef, useState } from 'react'
import { apiFailure } from '../../../services/http'
import { confirmAction } from '../../../services/confirmation'
import type { ApiFailure } from '../../../types/api'
import { gardenApi } from '../services/GardenApi'

/**
 * Exclusão definitiva da horta. O servidor só a aceita sem fatos de campo; com registros responde 409
 * (`garden.has_records`) e a horta continua apenas inativável. Pede confirmação antes do envio.
 */
export function useGardenDeletion(gardenId: string, gardenName: string, deleted: () => void) {
  const [reason, setReason] = useState(''), [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>()
  const sending = useRef(false)
  async function remove(version: number) {
    if (sending.current) return
    if (!reason.trim()) { setError({ message: 'Informe o motivo.', fields: { reason: ['Motivo obrigatório.'] } }); return }
    const confirmed = await confirmAction({
      title: 'Excluir horta definitivamente?',
      message: `"${gardenName}", seus setores, canteiros, participantes e mapa serão removidos. Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir horta', tone: 'danger',
    })
    if (!confirmed) return
    sending.current = true; setBusy(true); setError(undefined)
    try { await gardenApi.remove(gardenId, version, reason.trim()); deleted() }
    catch (failure) { setError(apiFailure(failure)) }
    finally { sending.current = false; setBusy(false) }
  }
  return { reason, setReason, busy, error, remove }
}
