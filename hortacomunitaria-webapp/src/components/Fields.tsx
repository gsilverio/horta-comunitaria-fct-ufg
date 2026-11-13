import { createContext, useContext, useId } from 'react'
import { HelpButton } from './HelpButton'
import type { ReactNode } from 'react'
import type { ApiFailure } from '../types/api'
import type { Choice, DetailItem } from '../types/records'

const FilterMode = createContext(false)
/** Em filtros, campo vazio significa "todos" e a marca de opcional seria ruído visual. */
export function FilterFields({ children }: { children: ReactNode }) { return <FilterMode.Provider value={true}>{children}</FilterMode.Provider> }

/** Marca visual de obrigatório; o atributo `required` do controle informa leitores de tela. */
export function RequiredMark() { return <span className="required-mark" aria-hidden="true">*</span> }

export function FieldLabel({ label, required }: { label: string; required: boolean }) {
  const filter = useContext(FilterMode)
  return <span className="field-label">{label}{required && !filter && <RequiredMark />}</span>
}

export function Field({ name, label, value, change, error, required = false, type = 'text', hint, maxLength = 1000, suggestions, unit, wide = false, placeholder }: { name: string; label: string; value: string; change: (value: string) => void; error?: ApiFailure; required?: boolean; type?: string; hint?: string; maxLength?: number; /** Valores frequentes oferecidos sem impedir texto livre. */ suggestions?: string[]; /** Unidade exibida dentro do campo (ex.: m²); o valor enviado continua só o número. */ unit?: string; /** Ocupa duas colunas da seção quando houver espaço. */ wide?: boolean; placeholder?: string }) {
  const id = useId(), messages = error?.fields[name]
  const props = { id, name, value, required, maxLength, placeholder, 'aria-invalid': !!messages, 'aria-describedby': `${id}-hint ${id}-error`, onChange: (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => change(event.target.value) }
  const input = type === 'textarea' ? <textarea {...props} rows={3} /> : <input {...props} type={type === 'decimal' || type === 'integer' ? 'text' : type} inputMode={type === 'decimal' ? 'decimal' : type === 'integer' ? 'numeric' : undefined} step={type === 'datetime-local' ? '0.001' : undefined} list={suggestions?.length ? `${id}-suggestions` : undefined} />
  return <label htmlFor={id} className={wide ? 'field-wide' : undefined}><FieldLabel label={label} required={required} />{unit && type !== 'textarea' ? <span className="input-with-unit">{input}<span className="input-unit" aria-hidden="true">{unit}</span></span> : input}{suggestions?.length ? <datalist id={`${id}-suggestions`}>{suggestions.map(item => <option key={item} value={item} />)}</datalist> : null}<small id={`${id}-hint`} className="hint">{hint || (type === 'datetime-local' ? 'Horário de São Paulo (America/Sao_Paulo).' : '')}</small><span className="field-error" id={`${id}-error`}>{messages?.join(' ')}</span></label>
}
export function SelectField({ name, label, value, change, options, error, required = false, hint }: { name: string; label: string; value: string; change: (value: string) => void; options: Choice[]; error?: ApiFailure; required?: boolean; hint?: string }) {
  const id = useId(), messages = error?.fields[name], filter = useContext(FilterMode)
  return <label htmlFor={id}><FieldLabel label={label} required={required} /><select id={id} name={name} value={value} required={required} onChange={event => change(event.target.value)} aria-invalid={!!messages} aria-describedby={`${id}-hint ${id}-error`}><option value="">{required ? 'Selecione…' : filter ? 'Todos' : 'Não informado / geral'}</option>{value && !options.some(x => x.id === value) && <option value={value}>Referência histórica indisponível — revise</option>}{options.map(item => <option key={item.id} value={item.id} disabled={item.isActive === false || item.disabled}>{item.label}{item.isActive === false ? ' (inativo)' : ''}</option>)}</select><small id={`${id}-hint`} className="hint">{hint}</small><span className="field-error" id={`${id}-error`}>{messages?.join(' ')}</span></label>
}
export function Toggle({ label, checked, change }: { label: string; checked: boolean; change: (value: boolean) => void }) { return <label className="flex min-h-11 flex-row items-center gap-3"><input className="!w-5 !min-h-5 accent-horta-forest" type="checkbox" checked={checked} onChange={event => change(event.target.checked)} />{label}</label> }
export function Facts({ items }: { items: DetailItem[] }) { return <dl className="facts detail-facts">{items.map((item, i) => <div key={`${item.label}/${i}`}><dt>{item.label}</dt><dd className="break-words">{item.value ?? 'Não informado'}</dd></div>)}</dl> }
/** Seção de formulário por assunto. Em largura de desktop, o assunto fica à esquerda e os campos à direita. */
/** `help` vira um ícone "?" ao lado do título que abre a explicação num diálogo; `hint` fica visível. */
export function Group({ title, hint, help, children }: { title: string; hint?: string; help?: ReactNode; children: ReactNode }) { return <fieldset className="form-section"><legend className="sr-only">{hint ? `${title}. ${hint}` : title}</legend><div className="form-section-heading"><span className="form-section-title"><strong aria-hidden="true">{title}</strong>{help && <HelpButton title={title}>{help}</HelpButton>}</span>{hint && <span aria-hidden="true">{hint}</span>}</div><div className="form-section-fields">{children}</div></fieldset> }
