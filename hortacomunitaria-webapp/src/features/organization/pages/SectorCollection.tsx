import type { AppRoute } from '../../../routes/routes'
import { linkTo } from '../../../routes/routes'
import { useCallback } from 'react'
import type { ReactNode } from 'react'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRemote } from '../../../hooks/useRemote'

import { CatalogStatusFilter } from '../components/CatalogStatusFilter'
import { OrganizationCollection, OrganizationRow } from '../components/OrganizationLayout'
import { useOrganizationList } from '../hooks/useOrganizationList'
import { irrigationSectorApi } from '../services/IrrigationSectorApi'

/** Setores da horta, usados na aba Setores e no detalhe da horta. */
export function SectorCollection({ route, action }: { route: AppRoute; action?: ReactNode }) {
  const garden = useGardenState()
  const { page, isActive, status, revision, retry, changePage, changeStatus } = useOrganizationList(route)
  const load = useCallback((signal: AbortSignal) => irrigationSectorApi.list(route.garden, { page, pageSize: 10, isActive }, signal), [route.garden, page, isActive])
  const result = useRemote(`${route.garden}/${page}/${status}/${revision}`, load)
  return <OrganizationCollection title="Setores da horta" {...result} retry={retry} changePage={changePage} action={action} emptyTitle="Comece pelo primeiro setor" emptyText={status === 'active' ? 'Um setor reúne os canteiros de uma mesma área. Use um nome fácil de reconhecer, como Setor norte.' : 'Não há setores nesta situação. Altere a opção Mostrar para consultar os demais.'} filter={<CatalogStatusFilter value={status} change={changeStatus} />}>
    {result.data?.items.map(item => <OrganizationRow key={item.id} title={item.name} kind="sector" description={item.description || undefined} status={!item.isActive ? 'Inativo' : undefined} statusTone="invalid" href={linkTo(garden.id || route.garden, 'horta/setores', item.id)} action="Ver canteiros" />)}
  </OrganizationCollection>
}
