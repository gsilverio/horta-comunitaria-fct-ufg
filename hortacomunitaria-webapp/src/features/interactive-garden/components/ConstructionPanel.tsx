import { useId, useState } from 'react'
import type { ReactNode } from 'react'
import { Check, Lock, PanelRightClose, PanelRightOpen } from 'lucide-react'

export type GardenSetupStage = 'terrain' | 'sectors' | 'beds'

export interface SetupStep { id: GardenSetupStage; title: string; status: string; complete: boolean; enabled: boolean; lockedReason?: string }

/** Desenho em andamento: o painel mostra só as instruções, o progresso e as ações para concluir ou cancelar. */
export interface DrawingSession { eyebrow: string; title: string; instructions: string[]; progress?: ReactNode; extra?: ReactNode; actions: ReactNode }

/**
 * Painel de construção do mapa em três etapas. Trocar de etapa só muda as ferramentas visíveis; nenhuma ação é
 * disparada. Durante um desenho, as etapas ficam bloqueadas até concluir ou cancelar.
 */
export function ConstructionPanel({ stage, steps, selectStage, session, title, description, children, footer }: {
  stage: GardenSetupStage; steps: SetupStep[]; selectStage: (stage: GardenSetupStage) => void; session?: DrawingSession
  title: string; description: string; children: ReactNode; footer?: ReactNode
}) {
  const [open, setOpen] = useState(true)
  return <aside className={`construction-panel drawing-menu order-2 flex min-w-0 flex-col self-stretch overflow-hidden border border-horta-line bg-white lg:col-start-2 lg:row-start-1 lg:h-0 lg:min-h-full ${open ? 'w-full lg:w-[22rem]' : 'w-full lg:w-12'}`} aria-label="Painel de construção do mapa">
    <div className={`hidden items-center justify-between gap-2 border-b border-horta-line px-4 py-2 lg:flex ${open ? '' : 'lg:flex-col lg:px-1'}`}>
      {open && <strong className="text-[11px] font-semibold tracking-wide text-horta-muted uppercase">Construção do mapa</strong>}
      <button type="button" className="interactive-icon-button" aria-expanded={open} aria-controls="construction-panel-content" title={open ? 'Recolher painel' : 'Abrir painel'} aria-label={open ? 'Recolher painel de construção' : 'Abrir painel de construção'} onClick={() => setOpen(current => !current)}>
        {open ? <PanelRightClose aria-hidden="true" /> : <PanelRightOpen aria-hidden="true" />}
      </button>
    </div>
    <div id="construction-panel-content" hidden={!open} className="flex min-h-0 flex-1 flex-col">
      <SetupStepper stage={stage} steps={steps} select={selectStage} locked={Boolean(session)} />
      <div id="construction-panel-scroll" className="min-h-0 max-h-[60dvh] flex-1 overflow-y-auto overscroll-contain lg:max-h-none">
        {session ? <DrawingSessionView session={session} /> : <>
          <div className="px-4 pt-4 pb-3">
            <h2 className="m-0 text-base font-semibold text-horta-forest">{title}</h2>
            <p className="mt-1 mb-0 text-xs leading-relaxed text-horta-muted">{description}</p>
          </div>
          {children}
        </>}
      </div>
      {(session?.actions ?? footer) && <div className="construction-footer">{session?.actions ?? footer}</div>}
    </div>
  </aside>
}

function SetupStepper({ stage, steps, select, locked }: { stage: GardenSetupStage; steps: SetupStep[]; select: (stage: GardenSetupStage) => void; locked: boolean }) {
  return <nav aria-label="Etapas do mapa" className="border-b border-horta-line bg-[#f7f9f3] px-3 pt-3 pb-2">
    <ol className="construction-stepper">
      {steps.map((step, index) => {
        const active = step.id === stage, disabled = !step.enabled || (locked && !active)
        const reason = locked && !active ? 'Conclua ou cancele o desenho atual' : step.lockedReason
        return <li key={step.id} className={step.complete ? 'complete' : undefined}>
          <button type="button" aria-current={active ? 'step' : undefined} disabled={disabled} onClick={() => select(step.id)} title={disabled ? reason : step.status}>
            <span aria-hidden="true" className={`construction-step-marker${active ? ' active' : step.complete ? ' complete' : ''}`}>
              {!step.enabled ? <Lock className="size-3" /> : step.complete && !active ? <Check className="size-3.5" strokeWidth={2.6} /> : index + 1}
            </span>
            <span className="min-w-0">
              <strong>{step.title}</strong>
              <small>{step.status}</small>
              {disabled && reason && <span className="sr-only">. Bloqueado: {reason}</span>}
            </span>
          </button>
        </li>
      })}
    </ol>
  </nav>
}

function DrawingSessionView({ session }: { session: DrawingSession }) {
  return <section aria-label={session.title} className="px-4 py-4">
    <span className="construction-session-badge"><span aria-hidden="true" />{session.eyebrow}</span>
    <h2 className="mt-2 mb-0 text-base font-semibold text-horta-forest">{session.title}</h2>
    {session.progress && <p className="mt-1 mb-0 text-xs font-medium text-horta-ink" role="status">{session.progress}</p>}
    {session.extra}
    <ol className="construction-instructions">{session.instructions.map(item => <li key={item}>{item}</li>)}</ol>
  </section>
}

/** Ação do painel em formato de linha: ícone, título e explicação curta. `pressed` marca a ferramenta ativa. */
export function MethodOption({ label, ariaLabel, description, icon, onClick, disabled, pressed, shortcut, badge, compact = false }: {
  label: string; ariaLabel?: string; description?: string; icon: ReactNode; onClick: () => void
  disabled?: boolean; pressed?: boolean; shortcut?: string; badge?: string; compact?: boolean
}) {
  // Nome acessível = título; a explicação entra como descrição, sem alterar o nome usado por leitores e testes.
  const descriptionId = useId()
  return <button type="button" className={`construction-option${compact ? ' compact' : ''}`} aria-label={ariaLabel ?? label} aria-describedby={description ? descriptionId : undefined} aria-pressed={pressed} aria-keyshortcuts={shortcut} disabled={disabled} onClick={onClick}
    title={shortcut ? `${ariaLabel ?? label} (${shortcut})` : ariaLabel ?? label}>
    <span aria-hidden="true" className="construction-option-icon">{icon}</span>
    <span className="min-w-0 flex-1">
      <span className="construction-option-title">{label}{badge && <span className="construction-option-badge">{badge}</span>}</span>
      {description && <span id={descriptionId} className="construction-option-description">{description}</span>}
    </span>
    {shortcut && !compact && <kbd aria-hidden="true">{shortcut}</kbd>}
  </button>
}

/** Bloco do painel com título curto. */
export function ConstructionSection({ title, children, tone = 'default', id }: { title: string; children: ReactNode; tone?: 'default' | 'danger'; id?: string }) {
  return <section id={id} className="construction-section" aria-label={title}>
    <h3 className={tone === 'danger' ? 'text-horta-danger' : undefined}>{title}</h3>
    {children}
  </section>
}
