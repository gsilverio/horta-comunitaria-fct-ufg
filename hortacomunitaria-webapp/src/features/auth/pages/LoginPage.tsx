import { useRef, useState } from 'react'
import { apiFailure } from '../../../services/http'
import type { ApiFailure } from '../../../types/api'
import { AuthShell, CredentialField } from '../components/AuthShell'
import { authApi } from '../services/authApi'

/** Tela de entrada. Após autenticar, o provedor carrega o usuário e a página do endereço atual é aberta. */
export function LoginPage({ signedIn, expired }: { signedIn: () => Promise<void>; expired: boolean }) {
  const [email, setEmail] = useState(''), [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false), [error, setError] = useState<ApiFailure>()
  const passwordRef = useRef<HTMLInputElement>(null)

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    if (busy) return
    setBusy(true); setError(undefined)
    try {
      await authApi.login(email.trim(), password)
      await signedIn()
    } catch (failure) {
      const result = apiFailure(failure)
      setError(result)
      if (result.status === 401 || result.status === 429) { setPassword(''); passwordRef.current?.focus() }
      setBusy(false)
    }
  }

  return <AuthShell title="Entrar" description="Use o e-mail e a senha cadastrados pela administração da horta.">
    {expired && !error && <p className="notice mb-4" role="status">Sua sessão terminou. Entre novamente para continuar.</p>}
    <form onSubmit={submit} noValidate={false}>
      <CredentialField id="email" label="E-mail" type="email" value={email} change={setEmail} autoComplete="username" error={error?.fields.email?.join(' ')} />
      <CredentialField id="password" label="Senha" type="password" value={password} change={setPassword} autoComplete="current-password" inputRef={passwordRef} />
      {error && <p className="field-error mb-4" role="alert">{error.message}</p>}
      <button type="submit" className="w-full" disabled={busy}>{busy ? 'Entrando…' : 'Entrar'}</button>
    </form>
    <p className="mt-5 mb-0 text-xs text-horta-muted">Esqueceu a senha? Procure a administração da horta.</p>
  </AuthShell>
}
