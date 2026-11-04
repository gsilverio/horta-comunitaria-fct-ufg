import { useId } from 'react'
import type { ReactNode } from 'react'
import type { ApiFailure } from '../types/api'
import type { Choice, DetailItem } from '../types/records'

export function Field({ name, label, value, change, error, required = false, type = 'text', hint, maxLength = 1000 }: { name: string; label: string; value: string; change: (value: string) => void; error?: ApiFailure; required?: boolean; type?: string; hint?: string; maxLength?: number }) {
  const id = useId(), messages = error?.fields[name]
  const props = { id, name, value, required, maxLength, 'aria-invalid': !!messages, 'aria-describedby': `${id}-hint ${id}-error`, onChange: (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => change(event.target.value) }
  return <label htmlFor={id}>{label}{required ? ' *' : ' (opcional)'}{type === 'textarea' ? <textarea {...props} rows={3} /> : <input {...props} type={type === 'decimal' || type === 'integer' ? 'text' : type} inputMode={type === 'decimal' ? 'decimal' : type === 'integer' ? 'numeric' : undefined} step={type === 'datetime-local' ? '0.001' : undefined} />}<small id={`${id}-hint`} className="hint">{hint || (type === 'decimal' ? 'Use vírgula ou ponto. Vazio significa não informado.' : type === 'datetime-local' ? 'Horário de São Paulo (America/Sao_Paulo).' : '')}</small><span className="field-error" id={`${id}-error`}>{messages?.join(' ')}</span></label>
}
export function SelectField({ name, label, value, change, options, error, required = false }: { name: string; label: string; value: string; change: (value: string) => void; options: Choice[]; error?: ApiFailure; required?: boolean }) {
  const id = useId(), messages = error?.fields[name]
  return <label htmlFor={id}>{label}{required ? ' *' : ' (opcional)'}<select id={id} name={name} value={value} required={required} onChange={event => change(event.target.value)} aria-invalid={!!messages} aria-describedby={`${id}-error`}><option value="">{required ? 'Selecione…' : 'Não informado / geral'}</option>{value && !options.some(x => x.id === value) && <option value={value}>Referência histórica indisponível — revise</option>}{options.map(item => <option key={item.id} value={item.id} disabled={item.isActive === false || item.disabled}>{item.label}{item.isActive === false ? ' (inativo)' : ''}</option>)}</select><span className="field-error" id={`${id}-error`}>{messages?.join(' ')}</span></label>
}
export function Toggle({ label, checked, change }: { label: string; checked: boolean; change: (value: boolean) => void }) { return <label className="flex flex-row items-center gap-3"><input className="!w-5 !min-h-5" type="checkbox" checked={checked} onChange={event => change(event.target.checked)} />{label}</label> }
export function Facts({ items }: { items: DetailItem[] }) { return <dl className="facts detail-facts">{items.map((item, i) => <div key={`${item.label}/${i}`}><dt>{item.label}</dt><dd className="break-words">{item.value ?? 'Não informado'}</dd></div>)}</dl> }
export function Group({ title, children }: { title: string; children: ReactNode }) { return <fieldset className="min-w-0 rounded-2xl border border-horta-line p-4"><legend className="px-2 font-semibold">{title}</legend><div className="grid gap-4 sm:grid-cols-2">{children}</div></fieldset> }
