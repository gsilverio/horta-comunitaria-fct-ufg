export interface ConfirmOptions {
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  /** `danger` para ações que descartam ou removem dados. */
  tone?: 'default' | 'danger'
}

export interface PendingConfirmation extends ConfirmOptions { resolve: (value: boolean) => void }

let pending: PendingConfirmation | undefined
const listeners = new Set<() => void>()
const emit = () => listeners.forEach(listener => listener())

/**
 * Substitui `window.confirm` pelo diálogo da aplicação (`ConfirmHost`). Resolve `true` apenas quando a pessoa
 * confirma; cancelar, fechar ou Esc resolvem `false`. Um novo pedido cancela o anterior.
 */
export function confirmAction(options: ConfirmOptions): Promise<boolean> {
  pending?.resolve(false)
  return new Promise(resolve => {
    pending = { ...options, resolve: value => { pending = undefined; emit(); resolve(value) } }
    emit()
  })
}

export function subscribeConfirmation(listener: () => void) { listeners.add(listener); return () => { listeners.delete(listener) } }
export function currentConfirmation() { return pending }
