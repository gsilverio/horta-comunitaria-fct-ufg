import { useCallback, useEffect, useState } from 'react'
import { AppShell } from './components/AppShell'
import { InlineSelect } from './components/InlineSelect'
import { BackLink } from './components/PageTrail'
import { BackLinkState } from './hooks/useBackLink'
import { Failure } from './components/Feedback'
import { irrigationApi } from './features/irrigation/services/irrigationApi'
import { gardenApi } from './features/organization/services/GardenApi'
import { useRemote } from './hooks/useRemote'
import { GardenState } from './hooks/useGardenState'
import { go, replaceRoute, useNavigationGuard, useRoute, useRouteFocus } from './routes/navigation'
import { backTarget, describeHref, linkTo } from './routes/routes'
import type { AppRoute } from './routes/routes'
import { NotFound, PageContent } from './routes/pages'
import { UsersPage } from './features/users/pages/UsersPage'
import { ChangePasswordPage } from './features/auth/pages/ChangePasswordPage'

export default function App() {
  const route = useRoute()
  useNavigationGuard()
  useRouteFocus(route)
  const [revision, setRevision] = useState(0), [inFlow, setInFlow] = useState(false)
  const [notice, setNotice] = useState<{ message: string; garden: string }>()
  const gardensLoad = useCallback((signal: AbortSignal) => irrigationApi.gardens(signal), [])
  const gardens = useRemote(`gardens/${revision}`, gardensLoad)
  // Catálogos e a lista de hortas não têm horta no endereço: mantêm a última horta aberta como contexto do menu.
  const [lastGarden, setLastGarden] = useState(route.garden)
  if (route.garden && route.garden !== lastGarden) setLastGarden(route.garden)
  const contextGarden = route.garden || lastGarden || gardens.data?.[0]?.id || ''
  const load = useCallback((signal: AbortSignal) => contextGarden ? gardenApi.get(contextGarden, signal) : Promise.resolve(undefined), [contextGarden])
  const selected = useRemote(`${contextGarden}/${revision}`, load)
  const garden = selected.data && selected.data.id === contextGarden ? selected.data : undefined

  // Somente o endereço vazio é encaminhado: para a primeira horta ou, sem hortas, para a lista.
  const firstGarden = gardens.data?.[0]?.id
  const home = route.scope === 'home' && !gardens.loading && !gardens.error ? (firstGarden ? linkTo(firstGarden, 'visao-geral') : linkTo('', 'hortas')) : undefined
  useEffect(() => { if (home) replaceRoute(home) }, [home])

  const pageRoute: AppRoute = route.scope === 'catalog' ? { ...route, garden: contextGarden } : route
  const saved = (message: string) => { setNotice({ message, garden: contextGarden }); setRevision(value => value + 1) }
  const backHref = backTarget(route)
  const back = backHref ? { href: backHref, label: describeHref(backHref) } : undefined
  function selectGarden(id: string) {
    if (route.scope === 'catalog') { setLastGarden(id); return }
    if (route.scope === 'garden' && route.page) { go(linkTo(id, route.page)); return }
    go(linkTo(id, 'visao-geral'))
  }
  const showGardenSelect = (gardens.data?.length ?? 0) > 1
  const noGardens = !gardens.loading && !gardens.error && gardens.data?.length === 0
  const needsGarden = route.scope === 'garden' || route.scope === 'catalog'
  return <BackLinkState.Provider value={back}><AppShell route={pageRoute} gardenName={garden?.name} gardenSelect={showGardenSelect ? <InlineSelect className="garden-picker" label="Horta" value={contextGarden} disabled={gardens.loading || !!gardens.error || (inFlow && route.page === 'irrigacao')} onChange={event => selectGarden(event.target.value)}>{gardens.data?.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}{garden && !gardens.data?.some(item => item.id === garden.id) && <option value={garden.id}>{garden.name}{!garden.isActive ? ' (inativa)' : ''}</option>}</InlineSelect> : gardens.loading ? <span className="text-xs text-horta-muted" role="status">Carregando hortas…</span> : noGardens && route.scope !== 'gardens' ? <a className="inline-flex min-h-11 items-center text-sm underline" href={linkTo('', 'hortas', 'nova')}>Cadastrar horta</a> : undefined}>
    <GardenState.Provider value={{ id: garden?.id ?? '', name: garden?.name ?? '', isActive: garden?.isActive ?? true, institutionalAreaM2: garden?.institutionalAreaM2 ?? null, operationalAreaM2: garden?.operationalAreaM2 ?? null, experimentalAreaM2: garden?.experimentalAreaM2 ?? null }}>
      {notice && notice.garden === contextGarden && <div className="feedback success" role="status"><span>{notice.message}</span><button type="button" className="text-button" onClick={() => setNotice(undefined)} aria-label="Dispensar confirmação">Fechar</button></div>}
      {needsGarden && contextGarden && selected.loading && <p role="status">Carregando horta…</p>}
      {selected.error && <><Failure error={selected.error} retry={() => setRevision(value => value + 1)} /><a className="action-link secondary" href={linkTo('', 'hortas')}>Voltar às hortas</a></>}
      {gardens.error && <Failure error={gardens.error} retry={() => setRevision(value => value + 1)} />}
      {garden && !garden.isActive && route.scope === 'garden' && <p className="notice">Horta inativa. Seus cadastros continuam disponíveis para consulta.</p>}
      {/* Exibido aqui só quando a página não mostra a barra de contexto (que já inclui o retorno). */}
      {back && <BackLink target={back} standalone />}
      {route.scope === 'home' && <p role="status">Abrindo horta…</p>}
      {route.scope === 'not-found' && <NotFound />}
      {route.scope === 'users' && <UsersPage route={route} saved={saved} />}
      {route.scope === 'account' && <ChangePasswordPage />}
      {route.scope === 'gardens' && <PageContent route={pageRoute} saved={saved} modeChanged={setInFlow} />}
      {route.scope === 'catalog' && <PageContent route={pageRoute} garden={garden} saved={saved} modeChanged={setInFlow} />}
      {route.scope === 'garden' && garden && <PageContent route={pageRoute} garden={garden} saved={saved} modeChanged={setInFlow} />}
    </GardenState.Provider>
  </AppShell></BackLinkState.Provider>
}
