import { useEffect, useRef, useSyncExternalStore } from 'react'
import { authorizeNavigation, releaseNavigationGuard } from '../services/navigationGuard'
import { linkTo, parseLocation, samePage } from './routes'
import type { AppRoute, PageKey, RouteAction } from './routes'

/** Evento interno disparado sempre que o endereço aceito muda (link, voltar/avançar ou troca sem histórico). */
const CHANGED = 'horta:route-changed'
const current = () => `${location.pathname}${location.search}`
const notify = () => window.dispatchEvent(new Event(CHANGED))

let cachedHref: string | undefined
let cachedRoute: AppRoute
/** Rota do endereço atual. A mesma instância é devolvida enquanto o endereço não muda. */
export function readRoute(): AppRoute {
  const href = current()
  if (cachedHref !== href) { cachedHref = href; cachedRoute = parseLocation(href) }
  return cachedRoute
}

function subscribe(changed: () => void) {
  window.addEventListener(CHANGED, changed)
  return () => window.removeEventListener(CHANGED, changed)
}
export function useRoute() { return useSyncExternalStore(subscribe, readRoute) }

/** Último endereço aceito; usado para desfazer voltar/avançar quando há rascunho. */
let accepted = current()
function push(href: string) { history.pushState(null, '', href); accepted = current(); notify() }

/**
 * Voltar/avançar do navegador: a proteção de rascunho decide antes de a tela mudar. Se a pessoa continuar
 * editando, o endereço anterior é restaurado; se descartar, o destino é aplicado.
 */
window.addEventListener('popstate', () => {
  const target = current()
  if (target === accepted) return
  if (authorizeNavigation(() => push(target))) { accepted = target; notify(); return }
  // Diálogo de descarte aberto: a tela permanece e o endereço volta a ser o do formulário.
  history.pushState(null, '', accepted)
})

/** Navega para um endereço interno respeitando a proteção de rascunho. */
export function go(href: string) {
  if (href === current()) return
  if (authorizeNavigation(() => push(href))) push(href)
}

/** Atalhos de `go` com os mesmos argumentos de `linkTo` e `samePage`. */
export function goTo(garden: string, key: PageKey, id = '', query?: URLSearchParams, action: RouteAction | '' = '') { go(linkTo(garden, key, id, query, action)) }
export function goToSame(route: AppRoute, id = '', query?: URLSearchParams, action: RouteAction | '' = '') { go(samePage(route, id, query, action)) }

/** Troca o endereço sem nova entrada no histórico (abas, normalização). Não passa pela proteção de rascunho. */
export function replaceRoute(href: string) {
  if (href === current()) return
  history.replaceState(null, '', href)
  accepted = current()
  notify()
}

/** Atualiza só a query da rota atual, sem histórico. */
export function replaceQuery(change: (query: URLSearchParams) => void) {
  const query = new URLSearchParams(location.search)
  change(query)
  replaceRoute(`${location.pathname}${query.size ? `?${query}` : ''}`)
}

/** Link que a aplicação trata sem recarregar: mesma origem, rota conhecida, clique simples, sem nova aba ou download. */
function internalHref(event: MouseEvent): { link: HTMLAnchorElement; href: string } | undefined {
  if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
  const link = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>('a[href]') : null
  if (!link || (link.target && link.target !== '_self') || link.hasAttribute('download')) return
  const href = link.getAttribute('href') ?? ''
  if (!href.startsWith('/') || href.startsWith('//') || parseLocation(href).scope === 'not-found') return
  return { link, href }
}

/**
 * Intercepta os links internos para navegar sem recarregar e com proteção de rascunho. Links com
 * `data-preserve-draft` liberam a proteção sem perguntar (o destino guarda o rascunho por conta própria).
 */
export function useNavigationGuard() {
  useEffect(() => {
    const clicked = (event: MouseEvent) => {
      const target = internalHref(event)
      if (!target) return
      event.preventDefault()
      if (target.link.dataset.preserveDraft === 'true') releaseNavigationGuard()
      go(target.href)
    }
    document.addEventListener('click', clicked)
    return () => document.removeEventListener('click', clicked)
  }, [])
}

/** Após navegar para outra página, leva o foco ao título principal (exceto em formulários). */
export function useRouteFocus(route: AppRoute) {
  const key = `${route.garden}|${route.page}|${route.id}|${route.action}`
  const form = route.id === 'novo' || route.id === 'nova' || route.action !== ''
  const first = useRef(true)
  useEffect(() => {
    if (first.current) { first.current = false; return }
    if (form) return
    const frame = requestAnimationFrame(() => document.querySelector<HTMLElement>('main h2, main h1')?.focus())
    return () => cancelAnimationFrame(frame)
  }, [key, form])
}
