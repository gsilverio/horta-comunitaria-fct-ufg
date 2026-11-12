import type { ReactNode } from 'react'

/** Moldura das telas sem sessão (entrar e troca obrigatória de senha). */
export function AuthShell({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  return <main id="main-content" className="flex min-h-dvh items-center justify-center bg-horta-canvas px-4 py-10">
    <section className="w-full max-w-sm border border-horta-line bg-white p-6 sm:p-8" aria-labelledby="auth-title">
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

/** Campo de credencial com rótulo, autocomplete e erro associado. */
export function CredentialField({ id, label, type, value, change, autoComplete, error, hint, inputRef }: {
  id: string; label: string; type: 'email' | 'password' | 'text'; value: string; change: (value: string) => void
  autoComplete: string; error?: string; hint?: string; inputRef?: React.Ref<HTMLInputElement>
}) {
  return <label htmlFor={id} className="mb-4 grid gap-1">
    <span className="field-label">{label}</span>
    <input ref={inputRef} id={id} name={id} type={type} value={value} required autoComplete={autoComplete} maxLength={type === 'password' ? 128 : 256}
      aria-invalid={!!error} aria-describedby={`${id}-hint ${id}-error`} onChange={event => change(event.target.value)} />
    <small id={`${id}-hint`} className="hint">{hint}</small>
    <span id={`${id}-error`} className="field-error">{error}</span>
  </label>
}
