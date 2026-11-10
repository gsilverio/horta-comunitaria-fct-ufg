import { useGardenState } from "../../../hooks/useGardenState"
import { useState } from 'react'
import { Field, Facts } from '../../../components/Fields'
import { CommandFeedback } from '../../../components/CommandFeedback'
import { useCommand } from '../../../hooks/useCommand'
import { requiredNumber } from '../../../services/values'
import { today, FormFailure } from '../../irrigation/formValues'
import { dryingLotApi } from '../services/DryingLotApi'
import type { DryingLot } from '../types/DryingLot'
export function DryingActions({ garden, record, changed }: { garden: string; record: DryingLot; changed: () => void }) {
  const workspace = useGardenState()
  const [endedOn, setEndedOn] = useState(today()), [mass, setMass] = useState(''), [reason, setReason] = useState('')
  const command = useCommand(record, () => dryingLotApi.get(garden, record.id), changed)
  return record.status === 'InProgress' && record.validity === 'Valid' && workspace.isActive && <details className="organization-disclosure"><summary>Finalizar secagem com pesagem</summary><p>Massa seca em branco mantém o lote em andamento. Não há rateio por espécie ou origem.</p><CommandFeedback {...command} compare={() => void command.compare()} review={command.review && <Facts items={[{ label: 'Versão', value: command.review.version }, { label: 'Massa seca (kg)', value: command.review.totalDryMassKg }, { label: 'Situação', value: command.review.status }]} />} /><form onSubmit={event => { event.preventDefault(); void command.submit(version => { if (!reason.trim()) throw new FormFailure({ reason: ['Informe o motivo.'] }); return dryingLotApi.complete(garden, record.id, endedOn, requiredNumber(mass, 'totalDryMassKg'), version, reason.trim()) }) }}><fieldset className="grid gap-4 sm:grid-cols-2" disabled={command.busy}><Field name="endedOn" label="Fim da secagem" required type="date" value={endedOn} change={setEndedOn} error={command.error} /><Field name="totalDryMassKg" label="Massa seca obtida (kg)" required type="decimal" value={mass} change={setMass} error={command.error} /><Field name="reason" label="Motivo da conclusão" required type="textarea" maxLength={500} value={reason} change={setReason} error={command.error} /><button disabled={command.error?.status === 409}>Confirmar finalização</button></fieldset></form></details>
}
