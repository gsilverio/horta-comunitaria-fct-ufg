import { useId } from 'react'
import { Icon } from './Icon'

export interface GardenOption { id: string; name: string }

/**
 * Troca de horta no cabeçalho, ao lado da conta. O select nativo cobre o controle inteiro:
 * teclado, leitor de tela e a lista do sistema continuam funcionando sem um combobox próprio.
 */
export function GardenSwitcher({ options, value, disabled, onChange }: { options: GardenOption[]; value: string; disabled?: boolean; onChange: (id: string) => void }) {
  const id = useId()
  const current = options.find(option => option.id === value)
  return <div className="garden-switcher" data-disabled={disabled || undefined} title={current?.name}>
    <span className="garden-switcher-icon" aria-hidden="true"><Icon name="horta" /></span>
    <span className="min-w-0 flex-1">
      <label htmlFor={id} className="garden-switcher-label">Horta</label>
      <span className="garden-switcher-name" aria-hidden="true">{current?.name ?? 'Selecione uma horta'}</span>
    </span>
    <Icon name="chevron-down" className="garden-switcher-chevron" />
    <select id={id} className="garden-switcher-select" value={value} disabled={disabled} onChange={event => onChange(event.target.value)}>
      {options.map(option => <option key={option.id} value={option.id}>{option.name}</option>)}
    </select>
  </div>
}
