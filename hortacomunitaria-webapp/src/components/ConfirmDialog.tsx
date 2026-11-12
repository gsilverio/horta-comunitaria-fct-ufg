import { useSyncExternalStore } from 'react'
import { AlertTriangle, HelpCircle } from 'lucide-react'
import * as DialogPrimitive from '@radix-ui/react-dialog'
import { currentConfirmation, subscribeConfirmation } from '../services/confirmation'

/** Montado uma vez na aplicação; renderiza o pedido de confirmação ativo. */
export function ConfirmHost() {
  const current = useSyncExternalStore(subscribeConfirmation, currentConfirmation)
  const danger = current?.tone === 'danger'
  return <DialogPrimitive.Root open={!!current} onOpenChange={open => { if (!open) current?.resolve(false) }}>
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="fixed inset-0 z-[60] bg-[#21352a]/45 data-[state=open]:animate-in data-[state=open]:fade-in-0" />
      <DialogPrimitive.Content role="alertdialog" aria-describedby="confirm-dialog-message"
        className="fixed top-1/2 left-1/2 z-[61] grid w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 gap-4 rounded-xl border border-horta-line bg-white p-5 shadow-2xl focus:outline-none data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=open]:zoom-in-95"
        onOpenAutoFocus={event => { event.preventDefault(); (event.currentTarget as HTMLElement).querySelector<HTMLButtonElement>('[data-confirm-cancel]')?.focus() }}>
        {current && <>
          <div className="flex items-start gap-3">
            <span aria-hidden="true" className={`flex size-10 shrink-0 items-center justify-center rounded-full ${danger ? 'bg-[#fbecea] text-[#a33a2f]' : 'bg-horta-soft text-horta-forest'}`}>
              {danger ? <AlertTriangle className="size-5" /> : <HelpCircle className="size-5" />}
            </span>
            <div className="min-w-0">
              <DialogPrimitive.Title className="m-0 text-base font-semibold text-horta-ink">{current.title}</DialogPrimitive.Title>
              <DialogPrimitive.Description id="confirm-dialog-message" className="mt-1 mb-0 text-sm leading-relaxed text-horta-muted">{current.message}</DialogPrimitive.Description>
            </div>
          </div>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" className="secondary" data-confirm-cancel onClick={() => current.resolve(false)}>{current.cancelLabel ?? 'Cancelar'}</button>
            <button type="button" className={danger ? 'danger' : undefined} onClick={() => current.resolve(true)}>{current.confirmLabel ?? 'Confirmar'}</button>
          </div>
        </>}
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  </DialogPrimitive.Root>
}
