import { useCallback, useMemo, useState } from 'react'
import { Plus } from 'lucide-react'
import { Failure, Pagination } from '../../../components/Feedback'
import { useRemote } from '../../../hooks/useRemote'
import { useGardenState } from '../../../hooks/useGardenState'
import { formatDate } from '../../../services/dates'
import { linkTo, withReturn } from '../../../routes/routes'
import { formatNumber, today } from '../../irrigation/formValues'
import type { Bed } from '../../organization/types/Bed'
import { gardenCropsApi } from '../../interactive-garden/services/gardenCropsApi'
import { cropTimeline, phaseNames, pluralDays } from '../../interactive-garden/cropVisuals'
import type { GardenCrop } from '../../interactive-garden/cropVisuals'
import { useSpeciesImage } from '../../interactive-garden/hooks/useSpeciesCatalog'
import { CropPhoto } from '../components/CropCatalogEntry'
import { bedCultivationApi } from '../services/bedCultivationApi'

type CycleItem = Awaited<ReturnType<typeof bedCultivationApi.list>>['items'][number]

/** Cultivos do canteiro: o que está plantado, em que fase e o próximo passo, sem cartões aninhados. */
export function BedCultivation({ garden, bed }: { garden: string; bed: Bed }) {
  const workspace = useGardenState()
  const on = today()
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => bedCultivationApi.list(garden, bed.id, page, signal), [garden, bed.id, page])
  const result = useRemote(`${garden}/${bed.id}/${page}/${revision}`, load)
  // Fase e prazos vêm da mesma consulta da panorâmica, calculados no servidor para a data de hoje.
  const loadCrops = useCallback((signal: AbortSignal) => gardenCropsApi.list(garden, on, signal), [garden, on])
  const crops = useRemote(`${garden}/${on}/crops/${revision}`, loadCrops)
  const cropByCycle = useMemo(() => new Map((crops.data ?? []).filter(crop => crop.bedId === bed.id).map(crop => [crop.cycleId, crop])), [crops.data, bed.id])
  const canWrite = workspace.isActive && bed.isActive
  const canStart = canWrite && !!bed.configuration
  const backToBed = linkTo(garden, 'horta/canteiros', bed.id)
  return <section className="bed-crops" aria-labelledby="bed-crops-title">
    <div className="section-heading">
      <div><h2 id="bed-crops-title">O que está plantado</h2><p>Fase estimada pelos prazos da espécie, contada a partir do primeiro plantio.</p></div>
      {canStart && <a className="action-link" href={linkTo(garden, 'horta/canteiros', bed.id, undefined, 'cultivar')}><Plus aria-hidden="true" className="size-4" />Adicionar cultivo</a>}
    </div>
    {!bed.configuration && canWrite && <p className="notice">Complete a área do canteiro para cadastrar o que será plantado nele.</p>}
    {result.loading && <p role="status">Carregando cultivos…</p>}
    {result.error && <Failure error={result.error} retry={() => setRevision(value => value + 1)} />}
    {crops.error && <p className="hint">Fase e prazos indisponíveis no momento; os cultivos continuam listados.</p>}
    {result.data && (result.data.items.length
      ? <ul className="bed-crop-list">{result.data.items.map(cycle => <CycleRow key={cycle.id} garden={garden} bed={bed} cycle={cycle} crop={cropByCycle.get(cycle.id)} on={on} canWrite={canWrite} backToBed={backToBed} />)}</ul>
      : <div className="empty"><h3>Nenhum cultivo cadastrado</h3><p className="mx-auto max-w-md">Use Adicionar cultivo para informar o que será plantado, a data de início e a área ocupada. A quantidade é registrada quando o plantio acontecer.</p></div>)}
    {result.data && result.data.totalPages > 1 && <Pagination data={result.data} change={setPage} label="Páginas de cultivos do canteiro" />}
  </section>
}

function CycleRow({ garden, bed, cycle, crop, on, canWrite, backToBed }: { garden: string; bed: Bed; cycle: CycleItem; crop?: GardenCrop; on: string; canWrite: boolean; backToBed: string }) {
  const image = useSpeciesImage(cycle.speciesId, Boolean(crop?.speciesImageUrl))
  const timeline = crop ? cropTimeline(crop, on) : undefined
  const closed = cycle.status === 'Closed'
  const status = closed ? 'Cultivo encerrado' : crop ? phaseNames[crop.stage] : cycle.plantings.totalCount ? 'Com plantio registrado' : 'Aguardando plantio'
  const ready = crop?.stage === 'HarvestExpected'
  const name = crop?.cultivarName ? `${cycle.speciesName} · ${crop.cultivarName}` : cycle.speciesName
  return <li className="bed-crop" data-ready={ready || undefined}>
    <CropPhoto name={cycle.speciesName} image={image} speciesPhoto />
    <div className="bed-crop-body">
    <div className="min-w-0">
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <h3>{name}</h3>
        <span className={`badge ${closed ? 'invalid' : ready ? 'ready' : ''}`}>{status}</span>
      </div>
      <p className="bed-crop-meta">
        {timeline?.elapsed !== undefined && timeline.elapsed >= 0 && <strong>{timeline.elapsed === 0 ? 'Plantado hoje' : `${pluralDays(timeline.elapsed)} de plantio`}</strong>}
        <span>{formatNumber(cycle.occupiedAreaM2)} m²</span>
        {crop?.initialQuantity != null && <span>{crop.initialQuantity.toLocaleString('pt-BR')} plantas iniciais</span>}
        <span>Início em {formatDate(cycle.startedOn)}</span>
      </p>
      {timeline?.progress !== undefined && timeline.harvestDays !== undefined && timeline.harvestOn && <div className="bed-crop-progress">
        <meter aria-label="Progresso até a colheita prevista" min={0} max={timeline.harvestDays} value={timeline.progress} />
        <span><span>{timeline.harvestIn !== undefined && timeline.harvestIn > 0 ? `Colheita prevista em ${pluralDays(timeline.harvestIn)}` : 'Prazo de colheita atingido'}</span><span>{formatDate(timeline.harvestOn)}</span></span>
      </div>}
      {crop && crop.visualHarvestDays === null && crop.firstPlantedOn && <p className="hint my-1">Sem prazo de colheita configurado para a espécie.</p>}
      {!!cycle.plantings.items.length && <p className="bed-crop-plantings"><span>Plantios:</span>{cycle.plantings.items.map(planting => <a key={planting.id} href={linkTo(garden, 'cultivo/plantios', planting.id, withReturn({}, backToBed))}>{formatNumber(planting.plantedQuantity)} plantas · {formatDate(planting.plantedOn)}{planting.kind === 'Replacement' ? ' · reposição' : ''}</a>)}
        {cycle.plantings.hasNextPage && <a href={linkTo(garden, 'cultivo/plantios', '', withReturn({ cycleId: cycle.id, bedId: bed.id, from: '0001-01-01', to: '9998-12-31' }, backToBed))}>ver todos ({cycle.plantings.totalCount})</a>}</p>}
    </div>
      <div className="bed-crop-actions">
        {canWrite && cycle.status === 'InProgress' && <a className="action-link" href={linkTo(garden, 'horta/canteiros', bed.id, new URLSearchParams({ cycleId: cycle.id }), 'plantar')}>{cycle.plantings.totalCount ? 'Registrar outro plantio' : 'Registrar plantio'}</a>}
        <a className="action-link secondary" href={linkTo(garden, 'cultivo/ciclos', cycle.id, withReturn({}, backToBed))}>Detalhes do cultivo</a>
      </div>
    </div>
  </li>
}
