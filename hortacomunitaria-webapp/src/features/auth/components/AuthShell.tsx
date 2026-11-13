import { useState } from 'react'
import type { ReactNode } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import loginBackground from '../../../assets/login-garden-pixel.svg'

/** Moldura das telas sem sessão (entrar e troca obrigatória de senha). */
export function AuthShell({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  // Fundo decorativo em pixel art (horta com canteiros, cerca e regador); o cartão mantém a leitura sobre a cena.
  return <main id="main-content" className="flex min-h-dvh items-center justify-center bg-horta-canvas bg-cover bg-bottom bg-no-repeat px-4 py-10"
    style={{ backgroundImage: `url(${loginBackground})` }}>
    <section className="w-full max-w-sm rounded-lg border border-horta-line bg-white/95 p-6 shadow-[0_24px_48px_-20px_rgb(28_73_57/0.55)] backdrop-blur-sm sm:p-8" aria-labelledby="auth-title">
      <div className="mb-6 flex items-center gap-3 text-horta-forest">
        <svg viewBox="0 0 32 32" aria-hidden="true" className="size-9 fill-none stroke-current stroke-2">
          <path d="M16 27V15M16 19C6 21 3 13 5 6c8 0 12 5 11 13ZM16 15C15 7 21 3 28 4c0 7-4 12-12 11Z" />
        </svg>
        <p className="m-0 text-sm font-semibold leading-5 text-horta-ink">Horta Comunitária<br /><span className="font-normal text-horta-muted">FCT / UFG · APOEMA</span></p>
      </div>
      <h1 id="auth-title" tabIndex={-1} className="mb-1 text-xl">{title}</h1>
      <p className="mb-5 text-sm text-horta-muted">{description}</p>
      {children}
    </section>
  </main>
}

/**
 * Campo de credencial com rótulo, autocomplete e erro associado. Senhas têm um botão para mostrar/ocultar o texto
 * digitado; o valor não muda, só a forma de exibição.
 */
export function CredentialField({ id, label, type, value, change, autoComplete, error, hint, inputRef }: {
  id: string; label: string; type: 'email' | 'password' | 'text'; value: string; change: (value: string) => void
  autoComplete: string; error?: string; hint?: string; inputRef?: React.Ref<HTMLInputElement>
}) {
  const [visible, setVisible] = useState(false)
  const isPassword = type === 'password'
  const input = <input ref={inputRef} id={id} name={id} type={isPassword && visible ? 'text' : type} value={value} required autoComplete={autoComplete}
    maxLength={isPassword ? 128 : 256} className={isPassword ? 'pr-11' : undefined} autoCapitalize={isPassword ? 'none' : undefined} spellCheck={isPassword ? false : undefined}
    aria-invalid={!!error} aria-describedby={`${id}-hint ${id}-error`} onChange={event => change(event.target.value)} />
  return <div className="mb-4 grid gap-1">
    <label htmlFor={id} className="field-label">{label}</label>
    {isPassword ? <div className="relative">
      {input}
      <button type="button" aria-controls={id} aria-pressed={visible} aria-label={visible ? `Ocultar ${label.toLocaleLowerCase('pt-BR')}` : `Mostrar ${label.toLocaleLowerCase('pt-BR')}`}
        title={visible ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setVisible(current => !current)}
        className="absolute inset-y-0 right-0 m-0 flex w-11 min-h-0 items-center justify-center rounded-r-md border-0 bg-transparent p-0 text-horta-muted shadow-none hover:bg-transparent hover:text-horta-forest focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-horta-focus">
        {visible ? <EyeOff aria-hidden="true" className="size-4" /> : <Eye aria-hidden="true" className="size-4" />}
      </button>
    </div> : input}
    <small id={`${id}-hint`} className="hint">{hint}</small>
    <span id={`${id}-error`} className="field-error">{error}</span>
  </div>
}
