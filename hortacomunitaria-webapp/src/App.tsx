import { lazy, Suspense, useCallback, useEffect, useState } from 'react'
import { navigationItems } from './components/navigationItems'
import { AppShell } from './components/AppShell'
import { FeatureContent } from './components/FeatureContent'
import { Failure } from './components/Feedback'
import { GardenFlow } from './features/irrigation/pages/IrrigationPage'
import { irrigationApi } from './features/irrigation/services/irrigationApi'
import { WorkflowPrerequisites } from './features/workflows/pages/WorkflowPrerequisites'
import { OrganizationPage } from './features/organization/pages/OrganizationPage'
import { gardenApi } from './features/organization/services/GardenApi'
import { useRemote } from './hooks/useRemote'
import { navigate, routeHref, useRoute } from './hooks/useRoute'
import { GardenState } from './hooks/useGardenState'
import { InteractiveGardenPage } from './features/interactive-garden/pages/InteractiveGardenPage'
const DashboardPage = lazy(() => import('./features/indicators/pages/DashboardPage').then(module => ({ default: module.DashboardPage })))
const defaults: Record<string, string> = { horta: 'interativa', irrigacao: 'registros', 'visao-geral': 'painel', ambiente: 'medicoes', cultivo: 'ciclos', producao: 'colheitas', atividades: 'registros', ocorrencias: 'registros', custos: 'registros' }

export default function App() {
  const route = useRoute()
  const [revision, setRevision] = useState(0), [inFlow, setInFlow] = useState(false)
  const [notice, setNotice] = useState<{ message: string; garden: string }>()
  const load = useCallback((signal: AbortSignal) => route.garden ? gardenApi.get(route.garden, signal) : Promise.resolve(undefined), [route.garden])
  const selected = useRemote(`${route.garden}/${revision}`, load)
  const gardensLoad = useCallback((signal: AbortSignal) => irrigationApi.gardens(signal), [])
  const gardens = useRemote(`gardens/${revision}`, gardensLoad)
  const garden = selected.data
  const resource = !route.garden ? 'hortas' : route.module === 'horta' && route.resource === 'preparacao' ? 'interativa' : route.resource || defaults[route.module] || ''
  const effectiveRoute = { ...route, module: !route.garden ? 'horta' : route.module, resource, id: !route.garden && (route.module !== 'horta' || route.resource !== 'hortas') ? '' : route.id }
  const saved = (message: string) => { setNotice({ message, garden: route.garden }); setRevision(value => value + 1) }
  const interactive = effectiveRoute.module === 'horta' && resource === 'interativa'
  const basic = effectiveRoute.module === 'horta' && ['hortas', 'setores', 'canteiros'].includes(resource)
  const returnBedId = route.search.get('returnBedId')
  const returnTo = route.search.get('returnTo')
  const indicatorBack = returnTo?.startsWith(`#/hortas/${encodeURIComponent(route.garden)}/visao-geral/`) ? returnTo : undefined
  const pendingArea = navigationItems.find(item => item.id === route.search.get('nextModule'))
  const catalogRoute = route.module === 'horta' && route.resource === 'hortas' && !pendingArea
  const defaultGarden = !route.garden && !catalogRoute && !gardens.loading && !gardens.error ? gardens.data?.[0]?.id : undefined
  const defaultModule = pendingArea?.id ?? route.module
  const defaultResource = pendingArea ? defaults[defaultModule] : route.resource || defaults[defaultModule]
  useEffect(() => {
    if (defaultGarden) navigate(defaultGarden, defaultModule, defaultResource)
  }, [defaultGarden, defaultModule, defaultResource])
  function selectGarden(id: string) {
    const module = route.garden ? route.module : pendingArea?.id ?? 'horta'
    if (!id) { navigate('', 'horta', 'hortas', '', module === 'horta' ? undefined : new URLSearchParams({ nextModule: module })); return }
    const target = module === 'horta' ? 'interativa' : route.garden ? resource : pendingArea?.items[0].id ?? defaults[module]
    navigate(id, module, target)
  }
  const noGardens = !gardens.loading && !gardens.error && gardens.data?.length === 0
  const showGardenSelect = (gardens.data?.length ?? 0) > 1
  return <AppShell route={effectiveRoute} gardenSelect={showGardenSelect ? <label className="min-w-0 gap-1 text-xs text-horta-muted">Horta<select aria-label="Horta" className="min-h-10 w-full min-w-0 bg-white py-2 text-sm" value={route.garden || defaultGarden || ''} disabled={gardens.loading || !!gardens.error || inFlow && route.module === 'irrigacao'} onChange={event => selectGarden(event.target.value)}>{!route.garden && !defaultGarden && <option value="" disabled>Todas as hortas</option>}{gardens.data?.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}{garden && !gardens.data?.some(item => item.id === garden.id) && <option value={garden.id}>{garden.name}{!garden.isActive ? ' (inativa)' : ''}</option>}</select></label> : gardens.loading ? <span className="text-xs text-horta-muted" role="status">Carregando hortas…</span> : noGardens && !catalogRoute ? <a className="text-xs underline" href={routeHref('', 'horta', 'hortas', 'novo')}>Cadastrar horta</a> : undefined}>
    <GardenState.Provider value={{ id: garden?.id ?? '', name: garden?.name ?? '', isActive: garden?.isActive ?? true, institutionalAreaM2: garden?.institutionalAreaM2 ?? null, operationalAreaM2: garden?.operationalAreaM2 ?? null, experimentalAreaM2: garden?.experimentalAreaM2 ?? null }}>
      {notice && notice.garden === route.garden && <div className="feedback success" role="status"><span>{notice.message}</span><button type="button" className="text-button" onClick={() => setNotice(undefined)} aria-label="Dispensar confirmação">Fechar</button></div>}
      {route.garden && selected.loading && <p role="status">Carregando horta…</p>}
      {selected.error && <><Failure error={selected.error} retry={() => setRevision(value => value + 1)} /><a className="action-link" href={routeHref('', 'horta', 'hortas')}>Voltar às hortas</a></>}
      {gardens.error && <Failure error={gardens.error} retry={() => setRevision(value => value + 1)} />}
      {garden && !garden.isActive && <p className="notice">Horta inativa. Seus cadastros continuam disponíveis para consulta.</p>}
      {returnBedId && <a className="mb-5 block font-semibold underline" href={routeHref(route.garden, 'horta', 'canteiros', returnBedId)}>← Voltar ao canteiro</a>}
      {indicatorBack && <a className="mb-5 block font-semibold underline" href={indicatorBack}>Voltar ao indicador com os mesmos filtros</a>}
      {defaultGarden && <p role="status">Abrindo horta…</p>}
      {!defaultGarden && (!route.garden || garden) && <Suspense fallback={<p role="status">Carregando área…</p>}>
        {interactive && garden ? <InteractiveGardenPage key={garden.id} route={effectiveRoute} />
          : basic ? <OrganizationPage key={`${effectiveRoute.garden}/${resource}/${route.id}`} route={effectiveRoute} saved={saved} />
          : garden && route.module === 'irrigacao' ? <WorkflowPrerequisites route={effectiveRoute}><GardenFlow key={`${garden.id}/${route.id}/${route.search}`} garden={garden} route={effectiveRoute} modeChanged={setInFlow} onSaved={message => setNotice({ message, garden: garden.id })} /></WorkflowPrerequisites>
          : route.module === 'visao-geral' ? <DashboardPage key={`${route.garden}/${resource}`} route={effectiveRoute} />
          : <FeatureContent key={`${route.garden}/${route.module}/${resource}`} route={effectiveRoute} saved={saved} />}
      </Suspense>}
    </GardenState.Provider>
  </AppShell>
}
