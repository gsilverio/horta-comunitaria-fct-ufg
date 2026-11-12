import { confirmAction } from './confirmation'
import type { ConfirmOptions } from './confirmation'

/** `true` libera, `false` bloqueia sem perguntar (ex.: gravação em andamento) e um objeto pede confirmação. */
export type NavigationDecision = boolean | ConfirmOptions
let guard: (() => NavigationDecision) | undefined

export function registerNavigationGuard(value: () => NavigationDecision) { guard = value; return () => { if (guard === value) guard = undefined } }
export function releaseNavigationGuard() { guard = undefined }

/**
 * Retorna `true` quando a navegação pode seguir imediatamente. Se houver rascunho, abre o diálogo da
 * aplicação e chama `proceed` somente depois da confirmação, liberando a proteção antes de navegar.
 */
export function authorizeNavigation(proceed?: () => void): boolean {
  const decision = guard ? guard() : true
  if (decision === true) { guard = undefined; return true }
  if (decision === false || !proceed) return false
  void confirmAction({ confirmLabel: 'Descartar e sair', cancelLabel: 'Continuar editando', tone: 'danger', ...decision }).then(confirmed => {
    if (!confirmed) return
    guard = undefined
    proceed()
  })
  return false
}

/** Texto padrão para rascunhos de formulário. */
export const discardDraft = (message = 'As alterações deste formulário ainda não foram salvas e serão perdidas.'): ConfirmOptions =>
  ({ title: 'Descartar rascunho?', message, confirmLabel: 'Descartar e sair', cancelLabel: 'Continuar editando', tone: 'danger' })
