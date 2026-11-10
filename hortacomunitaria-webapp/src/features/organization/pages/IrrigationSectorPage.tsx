import { useCallback } from 'react'
import { RecordDetail } from '../../../components/RecordDetail'
import { Failure } from '../../../components/Feedback'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRecord } from '../../../hooks/useRecord'
import { navigate, routeHref } from '../../../hooks/useRoute'
import type { RecordRoute } from '../../../types/records'
import type { IrrigationSector } from '../types/IrrigationSector'
import { irrigationSectorApi } from '../services/IrrigationSectorApi'
import { IrrigationSectorForm, IrrigationSectorReview } from '../components/IrrigationSectorForm'
import { CreateLink, OrganizationHeading, OrganizationTrail } from '../components/OrganizationLayout'
import { SectorBeds } from './SectorBeds'
import { SectorCollection } from './SectorCollection'

export function IrrigationSectorPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  if (route.id === 'novo') return <><OrganizationTrail current="Novo setor" /><IrrigationSectorForm garden={route.garden} saved={record => { saved('Setor cadastrado. Agora adicione os canteiros.'); navigate(route.garden, 'horta', 'setores', record.id) }} cancel={() => navigate(route.garden, 'horta', 'setores')} /></>
  if (route.id) return <SectorDetail route={route} saved={saved} />
  return <SectorList route={route} />
}
function SectorList({ route }: { route: RecordRoute }) {
  const garden = useGardenState()
  return <><OrganizationTrail /><OrganizationHeading eyebrow={garden.name} title="Setores" description="Organize a horta em setores. Abra um setor para ver e cadastrar seus canteiros." action={garden.isActive && <CreateLink href={routeHref(route.garden, 'horta', 'setores', 'novo')}>Novo setor</CreateLink>} />
    <SectorCollection route={route} />
    <div className="organization-secondary-links"><a href={routeHref(route.garden, 'horta', 'canteiros')}>Todos os canteiros e cadastros pendentes</a></div>
  </>
}
function SectorDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  const garden = useGardenState()
  const get = useCallback((signal: AbortSignal) => irrigationSectorApi.get(route.garden, route.id, signal), [route.garden, route.id])
  const audit = useCallback((page: number, signal: AbortSignal) => irrigationSectorApi.history(route.garden, route.id, page, signal), [route.garden, route.id])
  const state = useRecord<IrrigationSector>(route.id, get, audit, (version, reason) => irrigationSectorApi.deactivate(route.garden, route.id, version, reason), () => saved('Setor atualizado.'))
  const record = state.detail.data
  const back = () => navigate(route.garden, 'horta', 'setores', route.id)
  if (route.search.get('edit') === '1' && record) return <><OrganizationTrail sector={record} current="Editar setor" /><IrrigationSectorForm garden={route.garden} original={record} saved={() => { saved('Setor atualizado.'); state.refresh(); back() }} cancel={back} /></>
  return <>{state.detail.loading && <p role="status">Carregando setor…</p>}{state.detail.error && <><OrganizationTrail current="Setor indisponível" /><Failure error={state.detail.error} retry={state.refresh} /></>}
    {record && <><OrganizationTrail sector={record} /><RecordDetail compact hideBack state={state} eyebrow={garden.name} title={record.name} description={record.description || 'Os canteiros deste setor ficam reunidos aqui.'} version={record.version} active={record.isActive} catalog back={() => navigate(route.garden, 'horta', 'setores')} edit={() => navigate(route.garden, 'horta', 'setores', route.id, new URLSearchParams({ edit: '1' }))} actions={garden.isActive && record.isActive ? <CreateLink href={routeHref(route.garden, 'horta', 'canteiros', 'novo', new URLSearchParams({ sectorId: record.id }))}>Novo canteiro</CreateLink> : undefined} sections={[{ id: 'canteiros', label: 'Canteiros', content: <SectorBeds route={route} sector={record} /> }]} items={[{ label: 'Dados do setor', value: <IrrigationSectorReview record={record} /> }]} /></>}
  </>
}
