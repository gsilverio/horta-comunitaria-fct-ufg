import { useCallback } from 'react'
import { RecordDetail } from '../../../components/RecordDetail'
import { useRecord } from '../../../hooks/useRecord'
import { useRemote } from '../../../hooks/useRemote'
import { goTo } from '../../../routes/navigation'
import { linkTo } from '../../../routes/routes'
import type { AppRoute } from '../../../routes/routes'
import type { Garden } from '../types/Garden'
import { gardenApi } from '../services/GardenApi'
import { GardenForm, GardenReview } from '../components/GardenForm'
import { CatalogStatusFilter } from '../components/CatalogStatusFilter'
import { CreateLink, OrganizationHeading, OrganizationTrail } from '../components/OrganizationLayout'
import { useOrganizationList } from '../hooks/useOrganizationList'
import { GardenDataGrid } from '../components/GardenDataGrid'
import { SectorCollection } from './SectorCollection'

/** /hortas e /hortas/nova: catálogo e cadastro de hortas. */
export function GardenPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
  if (route.id === 'nova') return <>
    <OrganizationTrail current="Nova horta" withGarden={false} />
    <GardenForm garden="" saved={record => { saved('Horta cadastrada.'); goTo(record.id, 'horta/mapa') }} cancel={() => goTo('', 'hortas')} />
  </>
  return <GardenList route={route} />
}

function GardenList({ route }: { route: AppRoute }) {
  const { page, pageSize, isActive, status, revision, retry, changePage, changePageSize, changeStatus } = useOrganizationList(route)
  const load = useCallback((signal: AbortSignal) => gardenApi.list({ page, pageSize, isActive }, signal), [page, pageSize, isActive])
  const result = useRemote(page + '/' + pageSize + '/' + status + '/' + revision, load)
  return <>
    <OrganizationHeading title="Hortas" description="Gerencie os dados, a situação e a configuração das suas hortas." action={<CreateLink href={linkTo('', 'hortas', 'nova')}>Nova horta</CreateLink>} />
    <GardenDataGrid {...result} page={page} pageSize={pageSize} changePage={changePage} changePageSize={changePageSize} retry={retry} filter={<CatalogStatusFilter value={status} change={changeStatus} />} />
  </>
}

/** /hortas/{horta}/horta/dados[/editar|/inativar]: dados, histórico e setores da horta. */
export function GardenDataPage({ route, saved }: { route: AppRoute; saved: (message: string) => void }) {
  const get = useCallback((signal: AbortSignal) => gardenApi.get(route.garden, signal), [route.garden])
  const audit = useCallback((page: number, signal: AbortSignal) => gardenApi.history(route.garden, page, signal), [route.garden])
  const state = useRecord<Garden>(route.garden, get, audit, (version, reason) => gardenApi.deactivate(route.garden, version, reason), () => saved('Horta atualizada.'))
  const record = state.detail.data
  const openDetail = () => goTo(route.garden, 'horta/dados')
  if (route.action === 'editar' && record) return <>
    <OrganizationTrail current="Editar horta" />
    <GardenForm garden={route.garden} original={record} saved={() => { saved('Horta atualizada.'); openDetail() }} cancel={openDetail} />
  </>
  return <>
    <OrganizationTrail current="Dados da horta" />
    <RecordDetail compact requireActiveGarden={false} state={state} title={record?.name ?? 'Horta'} version={record?.version ?? 0} active={record?.isActive === true} catalog
      deactivationInitiallyOpen={route.action === 'inativar'} hideBack eyebrow="Horta"
      actions={record?.isActive ? <><a className="action-link secondary" href={linkTo(record.id, 'horta/mapa')}>Abrir mapa</a><CreateLink href={linkTo(record.id, 'horta/setores', 'novo')}>Novo setor</CreateLink></> : undefined}
      back={() => goTo('', 'hortas')}
      edit={() => goTo(route.garden, 'horta/dados', '', undefined, 'editar')}
      items={record ? [{ label: 'Dados da horta', value: <GardenReview record={record} /> }] : []}
      sections={record ? [{ id: 'setores', label: 'Setores', content: <SectorCollection route={route} /> }] : []} />
  </>
}
