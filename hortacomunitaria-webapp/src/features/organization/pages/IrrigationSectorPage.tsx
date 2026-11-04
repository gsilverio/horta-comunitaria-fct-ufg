import { useCallback } from 'react'
import { RecordDetail } from '../../../components/RecordDetail'
import { Failure } from '../../../components/Feedback'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRecord } from '../../../hooks/useRecord'
import { useRemote } from '../../../hooks/useRemote'
import { navigate, routeHref } from '../../../hooks/useRoute'
import type { RecordRoute } from '../../../types/records'
import type { IrrigationSector } from '../types/IrrigationSector'
import { irrigationSectorApi } from '../services/IrrigationSectorApi'
import { IrrigationSectorForm, IrrigationSectorReview } from '../components/IrrigationSectorForm'
import { CatalogStatusFilter } from '../components/CatalogStatusFilter'
import { CreateLink, OrganizationCollection, OrganizationHeading, OrganizationRow, OrganizationTrail } from '../components/OrganizationLayout'
import { useOrganizationList } from '../hooks/useOrganizationList'
import { SectorBeds } from './SectorBeds'

export function IrrigationSectorPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  if (route.id === 'novo') return <><OrganizationTrail current="Novo setor" /><IrrigationSectorForm garden={route.garden} saved={record => { saved('Setor cadastrado. Agora adicione os canteiros.'); navigate(route.garden, 'horta', 'setores', record.id) }} cancel={() => navigate(route.garden, 'horta', 'setores')} /></>
  if (route.id) return <SectorDetail route={route} saved={saved} />
  return <SectorList route={route} />
}
function SectorList({ route }: { route: RecordRoute }) {
  const garden = useGardenState()
  const { page, isActive, status, revision, retry, changePage, changeStatus } = useOrganizationList(route)
  const load = useCallback((signal: AbortSignal) => irrigationSectorApi.list(route.garden, { page, pageSize: 10, isActive }, signal), [route.garden, page, isActive])
  const result = useRemote(`${route.garden}/${page}/${status}/${revision}`, load)
  return <><OrganizationTrail /><OrganizationHeading eyebrow="Horta" title={garden.name} description="Organize a horta em setores. Abra um setor para ver e cadastrar seus canteiros." action={garden.isActive && <CreateLink href={routeHref(route.garden, 'horta', 'setores', 'novo')}>Novo setor</CreateLink>} />
    <OrganizationCollection title="Setores da horta" {...result} retry={retry} changePage={changePage} emptyTitle="Comece pelo primeiro setor" emptyText={status === 'active' ? 'Um setor reúne os canteiros de uma mesma área. Use um nome fácil de reconhecer, como Setor norte.' : 'Não há setores nesta situação. Altere a opção Mostrar para consultar os demais.'} filter={<CatalogStatusFilter value={status} change={changeStatus} />}>
      {result.data?.items.map(item => <OrganizationRow key={item.id} title={item.name} description={item.description || 'Abra este setor para organizar seus canteiros.'} status={!item.isActive ? 'Inativo' : undefined} href={routeHref(route.garden, 'horta', 'setores', item.id)} action="Ver canteiros" />)}
    </OrganizationCollection>
    <div className="organization-secondary-links"><a href={routeHref(route.garden, 'horta', 'hortas', route.garden)}>Dados da horta</a><a href={routeHref(route.garden, 'horta', 'canteiros')}>Todos os canteiros e cadastros pendentes</a></div>
  </>
}
function SectorDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  const get = useCallback((signal: AbortSignal) => irrigationSectorApi.get(route.garden, route.id, signal), [route.garden, route.id])
  const audit = useCallback((page: number, signal: AbortSignal) => irrigationSectorApi.history(route.garden, route.id, page, signal), [route.garden, route.id])
  const state = useRecord<IrrigationSector>(route.id, get, audit, (version, reason) => irrigationSectorApi.deactivate(route.garden, route.id, version, reason), () => saved('Setor atualizado.'))
  const record = state.detail.data
  const back = () => navigate(route.garden, 'horta', 'setores', route.id)
  if (route.search.get('edit') === '1' && record) return <><OrganizationTrail sector={record} current="Editar setor" /><IrrigationSectorForm garden={route.garden} original={record} saved={() => { saved('Setor atualizado.'); state.refresh(); back() }} cancel={back} /></>
  return <>{state.detail.loading && <p role="status">Carregando setor…</p>}{state.detail.error && <><OrganizationTrail current="Setor indisponível" /><Failure error={state.detail.error} retry={state.refresh} /></>}
    {record && <><SectorBeds route={route} sector={record} />
      <details className="organization-disclosure"><summary>Dados e manutenção do setor</summary><RecordDetail compact hideBack state={state} title={record.name} version={record.version} active={record.isActive} catalog back={() => navigate(route.garden, 'horta', 'setores')} edit={() => navigate(route.garden, 'horta', 'setores', route.id, new URLSearchParams({ edit: '1' }))} items={[{ label: 'Dados do setor', value: <IrrigationSectorReview record={record} /> }]} /></details>
    </>}
  </>
}
