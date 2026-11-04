import { BedCultivation } from '../../cultivation/pages/BedCultivation'
import { CultivationCycleForm } from '../../cultivation/components/CultivationCycleForm'
import { BedPlantingPage } from '../../cultivation/pages/BedPlantingPage'
import { useCallback } from 'react'
import { RecordDetail } from '../../../components/RecordDetail'
import { Failure } from '../../../components/Feedback'
import { useGardenState } from '../../../hooks/useGardenState'
import { useLookups } from '../../../hooks/useLookups'
import { useRecord } from '../../../hooks/useRecord'
import { useRemote } from '../../../hooks/useRemote'
import { navigate, routeHref } from '../../../hooks/useRoute'
import type { RecordRoute } from '../../../types/records'
import type { Bed } from '../types/Bed'
import { bedApi } from '../services/BedApi'
import { bedContextApi } from '../services/bedContextApi'
import { irrigationSectorApi } from '../services/IrrigationSectorApi'
import { organizationBrowseApi } from '../services/organizationBrowseApi'
import { BedForm } from '../components/BedForm'
import { BedContext } from '../components/BedContext'
import { BedSetupForm } from '../components/BedSetupForm'
import { CatalogStatusFilter } from '../components/CatalogStatusFilter'
import { OrganizationCollection, OrganizationHeading, OrganizationRow, OrganizationTrail } from '../components/OrganizationLayout'
import { useOrganizationList } from '../hooks/useOrganizationList'
import { formatDate } from '../../../services/dates'
import { formatNumber, today } from '../../irrigation/formValues'

export function BedPage({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  if (route.id === 'novo') return <NewBed route={route} saved={saved} />
  if (route.id) return <BedDetail route={route} saved={saved} />
  return <BedList route={route} />
}
function NewBed({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  const sectorId = route.search.get('sectorId') ?? ''
  const load = useCallback((signal: AbortSignal) => sectorId ? irrigationSectorApi.get(route.garden, sectorId, signal) : Promise.resolve(undefined), [route.garden, sectorId])
  const sector = useRemote(`${route.garden}/${sectorId}`, load)
  if (!sectorId) return <><OrganizationTrail current="Novo canteiro" /><section className="panel"><h1 tabIndex={-1}>Escolha o setor do canteiro</h1><p>Abra um setor e use Novo canteiro. Assim, o cadastro permanece no lugar certo.</p><a className="action-link" href={routeHref(route.garden, 'horta', 'setores')}>Escolher setor</a></section></>
  if (sector.loading) return <p role="status">Carregando setor…</p>
  if (sector.error) return <><OrganizationTrail current="Setor indisponível" /><Failure error={sector.error} /><a href={routeHref(route.garden, 'horta', 'setores')}>Voltar aos setores</a></>
  if (!sector.data?.isActive) return <p className="notice">Este setor está inativo. <a href={routeHref(route.garden, 'horta', 'setores')}>Escolher outro setor</a></p>
  return <><OrganizationTrail sector={sector.data} current="Novo canteiro" /><ol className="organization-path" aria-label="Etapas do cadastro"><li aria-current="step" className="font-semibold text-horta-forest">1. Identificação</li><li>2. Área e setor</li></ol>
    <BedForm garden={route.garden} saved={record => { saved('Identificação salva. Complete a área e o setor.'); navigate(route.garden, 'horta', 'canteiros', record.id, new URLSearchParams({ sectorId, setup: '1' })) }} cancel={() => navigate(route.garden, 'horta', 'setores', sectorId)} />
  </>
}
function BedList({ route }: { route: RecordRoute }) {
  const garden = useGardenState()
  const { page, isActive, status, revision, retry, changePage, changeStatus } = useOrganizationList(route)
  const on = today()
  const load = useCallback((signal: AbortSignal) => organizationBrowseApi.beds(route.garden, page, isActive, on, signal), [route.garden, page, isActive, on])
  const result = useRemote(`${route.garden}/${page}/${status}/${revision}`, load)
  const options = useLookups(route.garden, ['irrigationSector'])
  return <><OrganizationTrail current="Todos os canteiros" /><OrganizationHeading eyebrow={garden.name} title="Todos os canteiros" description="Consulte os canteiros da horta, inclusive os cadastros que ainda precisam de área ou setor." />
    {options.error && <Failure error={options.error} retry={options.retry} />}
    <OrganizationCollection title="Canteiros da horta" {...result} retry={retry} changePage={changePage} emptyTitle="Nenhum canteiro nesta seleção" emptyText="Para cadastrar, volte aos setores e escolha onde o canteiro ficará." filter={<CatalogStatusFilter value={status} change={changeStatus} />}>
      {result.data?.items.map(bed => { const sector = options.data?.irrigationSector?.find(item => item.id === bed.sectorAssignment?.sectorId); return <OrganizationRow key={bed.id} title={bed.name} description={`${bed.sectorAssignment ? sector?.label ?? 'Setor associado' : 'Sem setor na data atual'} · ${bed.configuration ? formatNumber(bed.configuration.cultivableAreaM2) + ' m²' : 'Área pendente'}`} status={!bed.isActive ? 'Inativo' : !bed.configuration || !bed.sectorAssignment ? 'Cadastro pendente' : undefined} href={routeHref(route.garden, 'horta', 'canteiros', bed.id)} action="Ver canteiro" /> })}
    </OrganizationCollection><div className="organization-secondary-links"><a href={routeHref(route.garden, 'horta', 'setores')}>← Voltar aos setores</a></div>
  </>
}
function BedDetail({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) {
  const on = today(), garden = useGardenState()
  const fromPanorama = route.search.get('returnTo') === 'panorama'
  const get = useCallback((signal: AbortSignal) => bedContextApi.on(route.garden, route.id, on, signal), [route.garden, route.id, on])
  const audit = useCallback((page: number, signal: AbortSignal) => bedApi.history(route.garden, route.id, page, signal), [route.garden, route.id])
  const state = useRecord<Bed>(route.id, get, audit, (version, reason) => bedApi.deactivate(route.garden, route.id, version, reason), () => saved('Canteiro atualizado.'))
  const record = state.detail.data
  const sectorId = record?.sectorAssignment?.sectorId ?? route.search.get('sectorId') ?? ''
  const loadSector = useCallback((signal: AbortSignal) => sectorId ? irrigationSectorApi.get(route.garden, sectorId, signal) : Promise.resolve(undefined), [route.garden, sectorId])
  const sector = useRemote(`${route.garden}/${sectorId}`, loadSector)
  const detail = () => navigate(route.garden, 'horta', 'canteiros', route.id, sectorId ? new URLSearchParams({ sectorId }) : undefined)
  if (route.search.get('cultivate') === '1' && record && record.isActive && garden.isActive) return <><OrganizationTrail sector={sector.data} current={record.name} /><CultivationCycleForm garden={route.garden} fixedBed={record} saved={cycle => { saved('Cultivo cadastrado. Registre o plantio agora ou volte ao canteiro para fazer isso depois.'); navigate(route.garden, 'horta', 'canteiros', route.id, new URLSearchParams({ plantingCycle: cycle.id })) }} cancel={detail} /></>
  const plantingCycle = route.search.get('plantingCycle')
  if (plantingCycle && record && record.isActive && garden.isActive) return <><OrganizationTrail sector={sector.data} current={record.name} /><BedPlantingPage key={plantingCycle} garden={route.garden} bedId={record.id} cycleId={plantingCycle} saved={() => { saved('Plantio registrado neste canteiro.'); state.refresh(); detail() }} cancel={detail} /></>
  if (route.search.get('edit') === '1' && record) return <><OrganizationTrail sector={sector.data} current="Editar canteiro" /><BedForm garden={route.garden} original={record} saved={() => { saved('Canteiro atualizado.'); state.refresh(); detail() }} cancel={detail} /></>
  if (route.search.get('setup') === '1' && record && record.isActive && garden.isActive) return <><OrganizationTrail sector={sector.data} current={record.name} /><BedSetupForm garden={route.garden} bed={record} sectorId={sectorId} saved={confirmed => { saved('Canteiro cadastrado.'); state.refresh(); navigate(route.garden, 'horta', 'canteiros', confirmed.id, new URLSearchParams({ sectorId: confirmed.sectorAssignment?.sectorId ?? sectorId })) }} cancel={detail} /></>
  return <>{fromPanorama && <a className="action-link secondary mb-4" href={routeHref(route.garden, 'visao-geral', 'painel')}>Voltar à panorâmica</a>}<OrganizationTrail sector={sector.data} current={record?.name ?? 'Canteiro'} />
    {sector.error && <Failure error={sector.error} />}
    <RecordDetail compact state={state} title={record?.name ?? 'Canteiro'} version={record?.version ?? 0} active={record?.isActive === true} catalog back={() => fromPanorama ? navigate(route.garden, 'visao-geral', 'painel') : navigate(route.garden, 'horta', sectorId ? 'setores' : 'canteiros', sectorId)} edit={() => navigate(route.garden, 'horta', 'canteiros', route.id, new URLSearchParams({ ...(sectorId ? { sectorId } : {}), edit: '1' }))} items={record ? [
      { label: 'Código do canteiro', value: record.code },
      { label: 'Setor atual', value: record.sectorAssignment ? sector.data?.name ?? 'Carregando setor…' : 'Ainda não associado' },
      { label: 'Área cultivável', value: record.configuration ? `${formatNumber(record.configuration.cultivableAreaM2)} m²` : 'Ainda não informada' },
      { label: 'Comprimento real', value: record.configuration?.lengthMeters == null ? 'Não informado' : `${formatNumber(record.configuration.lengthMeters)} m` },
      { label: 'Largura real', value: record.configuration?.widthMeters == null ? 'Não informada' : `${formatNumber(record.configuration.widthMeters)} m` },
      ...(record.configuration ? [{ label: 'Área válida desde', value: formatDate(record.configuration.validFrom) }] : []),
      ...(record.sectorAssignment ? [{ label: 'Setor associado desde', value: formatDate(record.sectorAssignment.validFrom) }] : []),
      ...(record.location ? [{ label: 'Localização', value: record.location }] : []),
    ] : []}>
      {record && record.isActive && garden.isActive && (!record.configuration || !record.sectorAssignment) && <div className="next-step"><h3>Falta completar o cadastro</h3><p>Confirme a área e o setor para deixar este canteiro organizado.</p><a className="action-link" href={routeHref(route.garden, 'horta', 'canteiros', route.id, new URLSearchParams({ ...(sectorId ? { sectorId } : {}), setup: '1' }))}>Completar cadastro</a></div>}
      {record && <BedCultivation garden={route.garden} bed={record} />}
      {record && <details className="organization-disclosure"><summary>Consultar ou alterar área e setor por data</summary><BedContext garden={route.garden} bed={record} changed={state.refresh} /></details>}
    </RecordDetail>
  </>
}
