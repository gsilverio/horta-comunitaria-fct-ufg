import { fieldLabels, enumLabels } from "../services/labels"
import type { AuditEntry } from '../types/records'
import type { Page } from '../types/api'
import { Pagination } from './Feedback'
import { formatInstant } from '../features/irrigation/formValues'
import { Facts } from './Fields'
const names: Record<string, string> = { Created: 'Criação', Corrected: 'Correção', Updated: 'Atualização', Invalidated: 'Invalidação', Inactivated: 'Inativação', Closed: 'Encerramento', Completed: 'Conclusão', StatusChanged: 'Mudança de situação', Resolved: 'Resolução' }
function snapshot(value: string | null): unknown { if (!value) return null; try { return JSON.parse(value) } catch { return 'Conteúdo histórico indisponível' } }
function Snapshot({ value }: { value: unknown }) {
  if (value === null || value === undefined) return <span>Não informado</span>
  if (typeof value === 'boolean') return <span>{value ? 'Sim' : 'Não'}</span>
  if (typeof value === 'string' || typeof value === 'number') return <span className="break-all">{typeof value === "string" ? enumLabels[value] ?? value : String(value)}</span>
  if (Array.isArray(value)) return <ol className="space-y-3">{value.map((item: unknown, index) => <li key={index}><Snapshot value={item} /></li>)}</ol>
  if (typeof value === 'object') return <Facts items={Object.entries(value).map(([key, item]) => ({ label: fieldLabels[key.charAt(0).toLowerCase() + key.slice(1)] ?? key, value: <Snapshot value={item} /> }))} />
  return <span>Não informado</span>
}
export function AuditHistory({ data, change }: { data: Page<AuditEntry>; change: (page: number) => void }) { return <section className="min-w-0"><h2 className="sr-only">Histórico de alterações</h2>{data.items.length === 0 && <p>Sem alterações registradas.</p>}<ol className="timeline">{data.items.map(item => <li key={item.id}><strong>{names[item.operation] || item.operation}</strong><p>{formatInstant(item.occurredAt)} · Autor técnico: {item.userId}</p><p>Motivo: {item.reason ?? 'Criação inicial'}</p><details><summary>Conferir antes e depois</summary><div className="grid min-w-0 gap-5 md:grid-cols-2"><section><h3>Antes</h3><Snapshot value={snapshot(item.before)} /></section><section><h3>Depois</h3><Snapshot value={snapshot(item.after)} /></section></div></details></li>)}</ol><Pagination data={data} change={change} label="Páginas do histórico" /></section> }
