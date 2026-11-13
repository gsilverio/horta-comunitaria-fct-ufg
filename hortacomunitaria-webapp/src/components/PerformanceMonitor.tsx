import { useEffect, useState } from 'react'

const STORAGE_KEY = 'horta-perf-monitor'
const FREEZE_MS = 200
const MAX_EVENTS = 50

interface FreezeEvent { at: string; path: string; durationMs: number; kind: 'quadro' | 'tarefa longa' }

function readEnabled() {
  try {
    const flag = new URLSearchParams(window.location.search).get('perf')
    if (flag === '1') window.sessionStorage.setItem(STORAGE_KEY, '1')
    if (flag === '0') window.sessionStorage.removeItem(STORAGE_KEY)
    return window.sessionStorage.getItem(STORAGE_KEY) === '1'
  } catch {
    return false
  }
}

/**
 * Diagnóstico opcional de travamentos, ativado com `?perf=1` (e desativado com `?perf=0`) na sessão do navegador.
 * Mede intervalos entre quadros e tarefas longas da thread principal; nada é enviado à API ou a terceiros.
 */
export function PerformanceMonitor() {
  const [enabled] = useState(readEnabled)
  const [fps, setFps] = useState(0)
  const [events, setEvents] = useState<FreezeEvent[]>([])
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (!enabled) return
    const record = (event: FreezeEvent) => setEvents(current => [event, ...current].slice(0, MAX_EVENTS))
    let frame = 0, last = performance.now(), frames = 0, windowStart = last
    const tick = (now: number) => {
      const gap = now - last
      // Aba oculta suspende os quadros; isso não é travamento.
      if (gap > FREEZE_MS && !document.hidden) record({ at: new Date().toLocaleTimeString('pt-BR'), path: window.location.pathname, durationMs: Math.round(gap), kind: 'quadro' })
      last = now; frames++
      if (now - windowStart >= 1000) { setFps(Math.round(frames * 1000 / (now - windowStart))); frames = 0; windowStart = now }
      frame = requestAnimationFrame(tick)
    }
    frame = requestAnimationFrame(tick)
    const resetClock = () => { last = performance.now() }
    document.addEventListener('visibilitychange', resetClock)
    let observer: PerformanceObserver | undefined
    try {
      observer = new PerformanceObserver(list => {
        for (const entry of list.getEntries()) if (entry.duration > FREEZE_MS)
          record({ at: new Date().toLocaleTimeString('pt-BR'), path: window.location.pathname, durationMs: Math.round(entry.duration), kind: 'tarefa longa' })
      })
      observer.observe({ type: 'longtask', buffered: false })
    } catch { observer = undefined }
    return () => { cancelAnimationFrame(frame); document.removeEventListener('visibilitychange', resetClock); observer?.disconnect() }
  }, [enabled])

  if (!enabled) return null
  const worst = events.reduce((max, event) => Math.max(max, event.durationMs), 0)
  const copy = async () => {
    const report = { geradoEm: new Date().toISOString(), navegador: navigator.userAgent, fpsAtual: fps, travamentos: events }
    try { await navigator.clipboard.writeText(JSON.stringify(report, null, 2)); setCopied(true); window.setTimeout(() => setCopied(false), 2000) } catch { setCopied(false) }
  }
  return <aside className="fixed right-3 bottom-3 z-[100] w-64 rounded-lg border border-horta-line bg-white/95 p-3 text-xs text-horta-ink shadow-xl" aria-label="Monitor de desempenho">
    <div className="flex items-center justify-between gap-2"><strong>Desempenho</strong><span className={fps > 0 && fps < 30 ? 'font-semibold text-red-700' : 'text-horta-muted'}>{fps} FPS</span></div>
    <p className="mt-1 mb-2 text-horta-muted">Travamentos acima de {FREEZE_MS} ms: <strong className="text-horta-ink">{events.length}</strong>{worst > 0 && <> · pior {worst} ms</>}</p>
    {events.length > 0 && <ol className="m-0 max-h-32 list-none overflow-y-auto p-0">{events.slice(0, 8).map((event, index) => <li key={`${event.at}-${index}`} className="border-t border-horta-line py-1"><span className="font-semibold">{event.durationMs} ms</span> · {event.kind} · {event.at}<br /><span className="text-horta-muted">{event.path}</span></li>)}</ol>}
    <div className="mt-2 flex gap-2"><button type="button" className="secondary !min-h-8 !px-2 !py-1 !text-xs" onClick={() => void copy()}>{copied ? 'Copiado' : 'Copiar relatório'}</button><button type="button" className="secondary !min-h-8 !px-2 !py-1 !text-xs" onClick={() => setEvents([])}>Limpar</button></div>
  </aside>
}
