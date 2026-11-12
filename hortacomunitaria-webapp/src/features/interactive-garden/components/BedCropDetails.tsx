import { CropPhoto } from '../../cultivation/components/CropCatalogEntry'
import { useSpeciesImage } from '../hooks/useSpeciesCatalog'
import { formatDate } from '../../../services/dates'
import { cropAsset, cropTimeline, phaseNames, pluralDays } from '../cropVisuals'
import type { GardenCrop } from '../cropVisuals'

/** Cultivos vigentes do canteiro na data da cena: foto da espécie, fase e prazos ilustrativos contados do plantio. */
export function BedCropDetails({ crops, on }: { crops: readonly GardenCrop[] | undefined; on: string }) {
  if (crops === undefined) return <p className="mt-3 text-sm">Dados de cultivo indisponíveis.</p>
  if (crops.length === 0) return <p className="mt-3 text-sm">Nenhum cultivo vigente neste canteiro.</p>
  return <ul className="m-0 mt-1 list-none p-0">{crops.map(crop => <CropItem key={crop.cycleId} crop={crop} on={on} />)}</ul>
}

function CropItem({ crop, on }: { crop: GardenCrop; on: string }) {
  const image = useSpeciesImage(crop.speciesId, Boolean(crop.speciesImageUrl))
  const asset = cropAsset(crop)
  const { elapsed, harvestDays, harvestOn, harvestIn, progress } = cropTimeline(crop, on)
  const name = crop.cultivarName ? `${crop.speciesName} · ${crop.cultivarName}` : crop.speciesName
  return <li className="garden-scene-crop">
    <div className="flex items-center gap-3">
      <CropPhoto name={crop.speciesName} image={image} speciesPhoto />
      <div className="min-w-0 flex-1">
        <strong className="block break-words text-sm text-white">{name}</strong>
        {crop.scientificName && <em className="block text-xs text-[#a9bea0]">{crop.scientificName}</em>}
      </div>
      <img src={asset.url} alt="" width={36} height={36} className="size-9 shrink-0" style={{ imageRendering: 'pixelated' }} title="Ilustração da fase na cena" />
    </div>
    <p className="garden-scene-phase"><span>{phaseNames[crop.stage]}</span>{elapsed !== undefined && elapsed >= 0 && <strong>{elapsed === 0 ? 'Plantado hoje' : `${pluralDays(elapsed)} de plantio`}</strong>}</p>
    {progress !== undefined && harvestDays !== undefined && harvestOn && <>
      <meter className="garden-scene-progress" aria-label="Progresso até a colheita prevista" min={0} max={harvestDays} value={progress} />
      <p className="my-1 text-xs">{harvestIn !== undefined && harvestIn > 0 ? `Colheita prevista em ${pluralDays(harvestIn)} (${formatDate(harvestOn)})` : `Prazo de colheita atingido em ${formatDate(harvestOn)}`}</p>
    </>}
    <dl className="garden-scene-facts">
      <div><dt>Plantio</dt><dd>{crop.firstPlantedOn ? formatDate(crop.firstPlantedOn) : 'Não registrado'}</dd></div>
      <div><dt>Plantas iniciais</dt><dd>{crop.initialQuantity === null ? 'Não informado' : crop.initialQuantity.toLocaleString('pt-BR')}</dd></div>
      <div><dt>Área ocupada</dt><dd>{crop.occupiedAreaM2.toLocaleString('pt-BR', { maximumFractionDigits: 2 })} m²</dd></div>
      <div><dt>Última colheita</dt><dd>{crop.lastHarvestedOn ? formatDate(crop.lastHarvestedOn) : 'Nenhuma'}</dd></div>
    </dl>
    {crop.hasReplacements && <p className="my-1 mt-2 text-xs">Houve reposição de mudas neste cultivo.</p>}
    {crop.stage === 'Mixed' && <p className="my-1 mt-2 text-xs">Plantios em datas diferentes: a contagem de dias parte do primeiro plantio inicial.</p>}
    {crop.visualHarvestDays === null && crop.firstPlantedOn && <p className="my-1 mt-2 text-xs">Sem prazo de colheita configurado para a espécie.</p>}
  </li>
}
