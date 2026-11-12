import { useState } from 'react'
import { apiFailure } from '../../../services/http'
import type { ApiFailure } from '../../../types/api'
import { CredentialField } from './AuthShell'
import { authApi } from '../services/authApi'

/** Formulário de troca da própria senha; a confirmação é conferida antes do envio. */
export function ChangePasswordForm({ done, cancel }: { done: () => Promise<void>; cancel?: () => void }) {
  const [current, setCurrent] = useState(''), [next, setNext] = useState(''), [confirm, setConfirm] = useState('')
  const [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>()
  const mismatch = confirm.length > 0 && confirm !== next

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    if (busy || mismatch) return
    setBusy(true); setError(undefined)
    try {
      await authApi.changePassword(current, next)
      await done()
    } catch (failure) {
      setError(apiFailure(failure))
      setBusy(false)
    }
  }

  return <form onSubmit={submit}>
    <CredentialField id="currentPassword" label="Senha atual" type="password" value={current} change={setCurrent} autoComplete="current-password" error={error?.fields.currentPassword?.join(' ')} />
    <CredentialField id="newPassword" label="Nova senha" type="password" value={next} change={setNext} autoComplete="new-password"
      hint="Pelo menos 12 caracteres, sem senhas comuns nem seu nome ou e-mail." error={error?.fields.newPassword?.join(' ')} />
    <CredentialField id="confirmPassword" label="Confirmar nova senha" type="password" value={confirm} change={setConfirm} autoComplete="new-password"
      error={mismatch ? 'A confirmação não corresponde à nova senha.' : undefined} />
    {error && Object.keys(error.fields).length === 0 && <p className="field-error mb-4" role="alert">{error.message}</p>}
    <div className="flex flex-wrap gap-2">
      <button type="submit" disabled={busy || mismatch || !confirm}>{busy ? 'Salvando…' : 'Trocar senha'}</button>
      {cancel && <button type="button" className="secondary" onClick={cancel} disabled={busy}>Cancelar</button>}
    </div>
  </form>
}
