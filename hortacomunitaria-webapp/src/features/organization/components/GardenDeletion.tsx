import { Field } from '../../../components/Fields'
import { Failure } from '../../../components/Feedback'
import type { ApiFailure } from '../../../types/api'

/** Exclusão definitiva, separada da inativação: só vale para hortas sem registros de campo. */
export function GardenDeletion({ reason, setReason, busy, error, remove }: {
  reason: string; setReason: (value: string) => void; busy: boolean; error?: ApiFailure; remove: () => void
}) {
  return <section className="panel maintenance-section">
    <h2>Excluir horta</h2>
    <p className="text-horta-muted">Permitido apenas enquanto a horta não tem registros de campo (ciclos, lotes de material, irrigações, atividades, ocorrências, custos, secagens, medições ou fotos). A estrutura cadastrada é removida junto. Com registros, use a inativação para preservar o histórico.</p>
    {error && <Failure error={error} />}
    <Field name="reason" label="Motivo da exclusão" required type="textarea" value={reason} change={setReason} error={error} maxLength={500} />
    <div className="mt-3 flex flex-wrap gap-2">
      <button className="danger" disabled={busy || error?.status === 409} onClick={remove}>Excluir definitivamente</button>
    </div>
  </section>
}
