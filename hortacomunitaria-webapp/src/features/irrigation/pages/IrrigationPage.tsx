import { useGardenState } from "../../../hooks/useGardenState"
import { useCallback, useState } from 'react'
import { Failure } from '../../../components/Feedback'
import { today } from '../formValues'
import { useRemote } from '../hooks/useRemote'
import { irrigationApi } from '../services/irrigationApi'
import { IrrigationForm } from '../components/IrrigationForm'
import { IrrigationList } from '../components/IrrigationList'
import { ConsumptionPanel } from '../components/ConsumptionPanel'
import { IrrigationDetailPage } from './IrrigationDetailPage'
import type { CatalogItem, Filters, IrrigationDetail, Validity } from '../types/irrigation'
import { navigate } from '../../../hooks/useRoute'
import type { RecordRoute } from '../../../types/records'

export function GardenFlow({ garden, modeChanged, route, onSaved }: { garden: CatalogItem; modeChanged: (busy: boolean) => void; route?: RecordRoute; onSaved?: (message: string) => void }) {
  const workspace = useGardenState()
  const [revision, setRevision] = useState(0)
  const [creating, setCreating] = useState(route?.id === 'novo'), [selected, setSelected] = useState(route?.id && route.id !== 'novo' ? route.id : ''), [notice, setNotice] = useState('')
  const [filters, setFilters] = useState<Filters>(() => ({ from: route?.search.get('from') || today(), to: route?.search.get('to') || today(), sectorId: route?.search.get('sectorId') || '', validity: route?.search.get('validity') === 'Invalid' ? 'Invalid' : 'Valid' }))
  const [draftFilters, setDraftFilters] = useState(filters), [filterError, setFilterError] = useState('')
  const [page, setPage] = useState(Math.max(1, Number(route?.search.get('page')) || 1)), [sectorPage, setSectorPage] = useState(Math.max(1, Number(route?.search.get('sectorPage')) || 1))
  const contextLoad = useCallback((signal: AbortSignal) => Promise.all([irrigationApi.sectors(garden.id, signal), irrigationApi.participants(garden.id, signal)]).then(([sectors, participants]) => ({ sectors, participants })), [garden.id])
  const context = useRemote(`${garden.id}/${revision}`, contextLoad)
  const listLoad = useCallback((signal: AbortSignal) => irrigationApi.list(garden.id, filters, page, signal), [garden.id, filters, page])
  const list = useRemote(`${JSON.stringify(filters)}/${page}/${revision}`, listLoad)
  const consumptionLoad = useCallback((signal: AbortSignal) => irrigationApi.consumption(garden.id, filters, sectorPage, signal), [garden.id, filters, sectorPage])
  const consumption = useRemote(`${JSON.stringify(filters)}/${sectorPage}/${revision}`, consumptionLoad)
  function open(id: string) { setSelected(id); modeChanged(true); if (route) navigate(garden.id, 'irrigacao', 'registros', id, route.search) }
  function saved(detail: IrrigationDetail) { setCreating(false); setRevision(value => value + 1); setNotice('Irrigação salva. Os dados abaixo são confirmados pela API.'); onSaved?.('Irrigação salva. Os dados abaixo são confirmados pela API.'); open(detail.id) }
  function back() { setSelected(''); setCreating(false); setRevision(value => value + 1); modeChanged(false); if (route) navigate(garden.id, 'irrigacao', 'registros', '', route.search) }
  const refresh = () => setRevision(value => value + 1)
  function filterRoute(values: Filters, listPage = page, consumptionPage = sectorPage) { if (route) navigate(garden.id, 'irrigacao', 'registros', '', new URLSearchParams({ ...values, page: String(listPage), sectorPage: String(consumptionPage) })) }
  if (creating) return <IrrigationForm garden={garden.id} saved={saved} cancel={back} />
  if (selected) return <>{notice && <p className="feedback success" role="status">{notice}</p>}<IrrigationDetailPage key={selected} garden={garden.id} id={selected} back={back} changed={refresh} sectors={context.data?.sectors ?? []} participants={context.data?.participants ?? []} /></>
  return <>
    <div className="page-heading"><div><span className="eyebrow">Acompanhamento da horta</span><h1 tabIndex={-1}>Irrigação</h1><p>Registros de campo e consumo conhecido, no mesmo lugar.</p></div><button disabled={!workspace.isActive} onClick={() => { setCreating(true); modeChanged(true); if (route) navigate(garden.id, 'irrigacao', 'registros', 'novo', route.search) }}>+ Nova irrigação</button></div>
    {notice && <p className="feedback success" role="status">{notice}</p>}
    <form className="panel filters" aria-label="Filtros do período" onSubmit={event => { event.preventDefault(); if (!draftFilters.from || !draftFilters.to || draftFilters.from > draftFilters.to) { setFilterError('Informe um período válido, com início até o fim.'); return } setFilterError(''); setFilters({ ...draftFilters }); setPage(1); setSectorPage(1); filterRoute(draftFilters, 1, 1) }}>
      <label htmlFor="filter-from">De<input id="filter-from" type="date" required value={draftFilters.from} onChange={event => setDraftFilters(current => ({ ...current, from: event.target.value }))} /></label>
      <label htmlFor="filter-to">Até<input id="filter-to" type="date" required value={draftFilters.to} onChange={event => setDraftFilters(current => ({ ...current, to: event.target.value }))} /></label>
      <label htmlFor="filter-sector">Setor<select id="filter-sector" value={draftFilters.sectorId} disabled={context.loading} onChange={event => setDraftFilters(current => ({ ...current, sectorId: event.target.value }))}><option value="">Todos os setores</option>{context.data?.sectors.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
      <label htmlFor="filter-validity">Registros<select id="filter-validity" value={draftFilters.validity} onChange={event => setDraftFilters(current => ({ ...current, validity: event.target.value as Validity }))}><option value="Valid">Válidos</option><option value="Invalid">Invalidados</option></select></label><button className="secondary" type="submit">Aplicar filtros</button>
      {filterError && <p className="field-error col-span-full" role="alert">{filterError}</p>}
    </form>
    {context.error && <Failure error={context.error} retry={refresh} />}
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]"><div>{list.loading ? <section className="panel" role="status">Carregando registros…</section> : list.error ? <Failure error={list.error} retry={refresh} /> : list.data && <IrrigationList data={list.data} open={open} changePage={value => { setPage(value); filterRoute(filters, value, sectorPage) }} />}</div>
      <div>{consumption.loading ? <section className="panel" role="status">Carregando consumo…</section> : consumption.error ? <Failure error={consumption.error} retry={refresh} /> : consumption.data && <ConsumptionPanel data={consumption.data} changePage={value => { setSectorPage(value); filterRoute(filters, page, value) }} />}</div></div>
    <p className="hint">O consumo considera apenas registros válidos. O filtro de invalidados altera a lista, sem incluir esses fatos no indicador.</p>
  </>
}
export function IrrigationPage() {
  const [gardenId, setGardenId] = useState(''), [revision, setRevision] = useState(0), [inFlow, setInFlow] = useState(false)
  const load = useCallback((signal: AbortSignal) => irrigationApi.gardens(signal), [])
  const gardens = useRemote(`gardens/${revision}`, load)
  const garden = gardens.data?.find(item => item.id === gardenId) ?? (gardenId ? undefined : gardens.data?.[0])
  return <><header className="app-header"><a className="brand" href="/" aria-label="Horta Comunitária, início"><svg viewBox="0 0 32 32" aria-hidden="true"><path d="M16 27V15M16 19C6 21 3 13 5 6c8 0 12 5 11 13ZM16 15C15 7 21 3 28 4c0 7-4 12-12 11Z" /></svg><span>Horta Comunitária<small>FCT / UFG · APOEMA</small></span></a>
    <label className="garden-select" htmlFor="garden-selection">Horta<select id="garden-selection" disabled={gardens.loading || inFlow} value={garden?.id ?? ''} onChange={event => setGardenId(event.target.value)}>{gardens.data?.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}</select></label></header>
    <div className="demo-strip"><span className="demo-dot" aria-hidden="true" /><strong>Ambiente de demonstração</strong><span>Dados fictícios · autoria: Operador de demonstração</span></div>
    <main id="main-content">{gardens.loading && <p role="status">Buscando hortas…</p>}{gardens.error && <Failure error={gardens.error} retry={() => setRevision(value => value + 1)} />}{gardens.data?.length === 0 && <section className="panel empty"><h1>Nenhuma horta disponível</h1><p>Ainda não há hortas cadastradas para selecionar.</p></section>}{garden && <GardenFlow key={garden.id} garden={garden} modeChanged={setInFlow} />}</main>
    <footer>Horta Comunitária FCT/UFG <span>Coleta manual · cuidado compartilhado</span></footer></>
}


