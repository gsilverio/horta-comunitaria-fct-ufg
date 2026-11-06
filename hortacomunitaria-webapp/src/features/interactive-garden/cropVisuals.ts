import manifest from './crop-assets.json' with { type: 'json' }
import { visualDensity } from '../cultivation/visualDensity'

export type CropStage = 'AwaitingPlanting' | 'Planted' | 'Seedling' | 'Growing' | 'HarvestExpected' | 'HarvestRecorded' | 'Mixed'
export interface GardenCrop {
  cycleId: string
  bedId: string
  speciesName: string
  speciesCatalogKey: string | null
  scientificName: string | null
  cultivarName: string | null
  cultivarCatalogKey: string | null
  occupiedAreaM2: number
  initialQuantity: number | null
  hasReplacements: boolean
  hasMixedPlantingDates: boolean
  firstPlantedOn: string | null
  lastHarvestedOn: string | null
  visualGrowthDays: number | null
  visualHarvestDays: number | null
  stage: CropStage
}

export const stageLabels: Record<CropStage, string> = {
  AwaitingPlanting: 'Plantio ainda não registrado',
  Planted: 'Plantio registrado · evolução sem prazo configurado',
  Seedling: 'Início do plantio · fase estimada',
  Growing: 'Crescimento intermediário · fase estimada',
  HarvestExpected: 'Colheita prevista · fase estimada',
  HarvestRecorded: 'Colheita registrada · pode ser parcial',
  Mixed: 'Fase conjunta não estimada · plantios distintos ou reposições',
}

/** Faixas de desenho, sem afirmar espaçamento adequado ou número de sobreviventes. */
export function cropDensity(crop: GardenCrop): 'Baixa' | 'Média' | 'Alta' | undefined {
  return visualDensity(crop.initialQuantity, crop.occupiedAreaM2)
}

export function cropSymbolCount(crop: GardenCrop) {
  const density = cropDensity(crop)
  if (!density || crop.stage === 'AwaitingPlanting') return 0
  return Math.min(crop.initialQuantity ?? 0, density === 'Baixa' ? 2 : density === 'Média' ? 4 : 6)
}

const assets: Record<string, { seedling: string; growing: string; harvest: string }> = manifest.assets
const speciesKeys: Record<string, string> = manifest.species
const cultivarKeys: Record<string, { asset: string; sharedWithSpecies: boolean }> = manifest.cultivars

export function cropAsset(crop: GardenCrop) {
  const variant = crop.cultivarCatalogKey ? cultivarKeys[crop.cultivarCatalogKey] : undefined
  // Identificação científica permite ilustrar também cadastros manuais conhecidos,
  // sem inferir espécie a partir de nomes comuns ambíguos.
  const scientificKeys: Record<string, string> = {
    'Lactuca sativa': 'alface', 'Allium sativum': 'alho', 'Allium cepa': 'cebola',
    'Daucus carota': 'cenoura', 'Ipomoea batatas': 'batata-doce', 'Solanum melongena': 'berinjela',
    'Solanum lycopersicum': 'tomate', 'Solanum tuberosum': 'batata', 'Cucumis sativus': 'pepino',
    'Cucumis melo': 'melao', 'Pisum sativum': 'ervilha', 'Cicer arietinum': 'grao-de-bico',
    'Lens culinaris': 'lentilha', 'Sinapis alba': 'mostarda', 'Capsicum annuum': 'pimenta-annuum',
    'Capsicum chinense': 'pimenta-chinense', 'Capsicum baccatum': 'pimenta-baccatum',
    'Arracacia xanthorrhiza': 'mandioquinha', 'Cucurbita moschata': 'abobora',
  }
  const key = variant?.asset ?? (crop.speciesCatalogKey ? speciesKeys[crop.speciesCatalogKey] : undefined)
    ?? (crop.scientificName ? scientificKeys[crop.scientificName] : undefined) ?? 'generic'
  const phase = crop.stage === 'HarvestExpected' || crop.stage === 'HarvestRecorded' ? 'harvest'
    : crop.stage === 'Growing' ? 'growing' : 'seedling'
  return { url: (assets[key] ?? assets.generic)[phase], generic: key === 'generic',
    sharedWithSpecies: !variant || variant.sharedWithSpecies }
}

/** Orçamento fixo por canteiro; posições e amostragem permanecem estáveis ao aproximar a câmera. */
export function bedCropSymbols(crops: readonly GardenCrop[]) {
  const visible = crops.filter(crop => cropSymbolCount(crop) > 0).slice().sort((a, b) => a.cycleId.localeCompare(b.cycleId))
  const symbols: { crop: GardenCrop; index: number }[] = []
  for (let round = 0; round < 6 && symbols.length < 12; round++) {
    for (const crop of visible) {
      if (round < cropSymbolCount(crop) && symbols.length < 12) symbols.push({ crop, index: round })
    }
  }
  return symbols
}
