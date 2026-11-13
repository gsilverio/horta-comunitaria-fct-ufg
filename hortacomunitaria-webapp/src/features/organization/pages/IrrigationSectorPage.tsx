import type { AppRoute } from '../../../routes/routes'
import { goTo } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import { useCallback } from 'react'
import { RecordDetail } from '../../../components/RecordDetail'
import { Failure } from '../../../components/Feedback'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRecord } from '../../../hooks/useRecord'

import type { IrrigationSector } from '../types/IrrigationSector'
import { irrigationSectorApi } from '../services/IrrigationSectorApi'
import { IrrigationSectorForm, IrrigationSectorReview } from '../components/IrrigationSectorForm'
import { CreateLink, OrganizationHeading, OrganizationTrail } from '../components/OrganizationLayout'
import { SectorBeds } from './SectorBeds'
import { SectorCollection } from './SectorCollection'

export function IrrigationSectorPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
  if (route.id === 'novo') return <><OrganizationTrail current="Novo setor" /><IrrigationSectorForm garden={route.garden} saved={record => { saved('Setor cadastrado. Agora adicione os canteiros.'); goTo(route.garden, 'horta/setores', record.id) }} cancel={() => goTo(route.garden, 'horta/setores')} /></>
  if (route.id) return <SectorDetail route={route} saved={saved} />
  return <SectorList route={route} />
}
function SectorList({ route }: { route: AppRoute }) {
  const garden = useGardenState()
  return <><OrganizationTrail /><OrganizationHeading eyebrow={garden.name} title="Setores" description="Organize a horta em setores. Abra um setor para ver e cadastrar seus canteiros." action={garden.isActive && <CreateLink href={linkTo(route.garden, 'horta/setores', 'novo')}>Novo setor</CreateLink>} />
    <SectorCollection route={route} action={<a className="org-collection-link" href={linkTo(route.garden, 'horta/canteiros')}>Todos os canteiros</a>} />
  </>
}
function SectorDetail({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
  const garden = useGardenState()
  const get = useCallback((signal: AbortSignal) => irrigationSectorApi.get(route.garden, route.id, signal), [route.garden, route.id])
  const audit = useCallback((page: number, signal: AbortSignal) => irrigationSectorApi.history(route.garden, route.id, page, signal), [route.garden, route.id])
  const state = useRecord<IrrigationSector>(route.id, get, audit, (version, reason) => irrigationSectorApi.deactivate(route.garden, route.id, version, reason), () => saved('Setor atualizado.'))
  const record = state.detail.data
  const back = () => goTo(route.garden, 'horta/setores', route.id)
  if (route.action === 'editar' && record) return <><OrganizationTrail sector={record} current="Editar setor" /><IrrigationSectorForm garden={route.garden} original={record} saved={() => { saved('Setor atualizado.'); state.refresh(); back() }} cancel={back} /></>
  return <>{state.detail.loading && <p role="status">Carregando setor…</p>}{state.detail.error && <><OrganizationTrail current="Setor indisponível" /><Failure error={state.detail.error} retry={state.refresh} /></>}
    {record && <><OrganizationTrail sector={record} /><RecordDetail compact hideBack state={state} eyebrow={garden.name} title={record.name} description={record.description || 'Os canteiros deste setor ficam reunidos aqui.'} version={record.version} active={record.isActive} catalog back={() => goTo(route.garden, 'horta/setores')} edit={() => goTo(route.garden, 'horta/setores', route.id, undefined, 'editar')} actions={garden.isActive && record.isActive ? <CreateLink href={linkTo(route.garden, 'horta/canteiros', 'novo', new URLSearchParams({ sectorId: record.id }))}>Novo canteiro</CreateLink> : undefined} sections={[{ id: 'canteiros', label: 'Canteiros', content: <SectorBeds route={route} sector={record} /> }]} items={[{ label: 'Dados do setor', value: <IrrigationSectorReview record={record} /> }]} /></>}
  </>
}
