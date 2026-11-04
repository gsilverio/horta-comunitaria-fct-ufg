import type { LayoutSnapshot } from './services/gardenLayoutApi'
import type { GardenWorkspace } from '../../hooks/useGardenState'
import { createDemoGarden, elementBounds } from './geometry'
import type { Bed } from '../organization/types/Bed'
import type { IrrigationSector } from '../organization/types/IrrigationSector'
import type { DemoElement, DemoGarden, DemoSector } from './types'

export interface InteractiveOrganization {
  savedLayout?: LayoutSnapshot
  sectors: IrrigationSector[]
  beds: Bed[]
}

export interface GardenProjection {
  garden: DemoGarden
  totalAreaM2: number | undefined
  warnings: string[]
}

const PROJECTION_VERSION = 3

function projectionKey(organization?: InteractiveOrganization) {
  if (!organization) return 'catalog:unavailable'
  const beds = organization.beds.map((bed) => ({
    id: bed.id,
    code: bed.code,
    name: bed.name,
    location: bed.location,
    active: bed.isActive,
    area: bed.configuration?.cultivableAreaM2 ?? null,
    length: bed.configuration?.lengthMeters ?? null,
    width: bed.configuration?.widthMeters ?? null,
    sector: bed.sectorAssignment?.sectorId ?? null,
  })).sort((left, right) => left.id.localeCompare(right.id))
  const sectors = organization.sectors.map((sector) => ({ id: sector.id, active: sector.isActive, name: sector.name })).sort((left, right) => left.id.localeCompare(right.id))
  return JSON.stringify({ beds, sectors })
}

/** Soma somente áreas informadas no cadastro da horta. Campos vazios não viram zero silenciosamente. */
export function registeredGardenAreaM2(garden: Pick<GardenWorkspace, 'institutionalAreaM2' | 'operationalAreaM2' | 'experimentalAreaM2'>) {
  const values = [garden.institutionalAreaM2, garden.operationalAreaM2, garden.experimentalAreaM2]
    .filter((value): value is number => value !== null && Number.isFinite(value) && value > 0)
  return values.length === 0 ? undefined : values.reduce((total, value) => total + value, 0)
}

function dimensionsForBed(bed: Bed) {
  const area = bed.configuration?.cultivableAreaM2 ?? 1
  const width = bed.configuration?.widthMeters
  const length = bed.configuration?.lengthMeters
  if (width && length) return { width, height: length, estimated: false }
  if (width) return { width, height: area / width, estimated: true }
  if (length) return { width: area / length, height: length, estimated: true }
  const side = Math.sqrt(area)
  return { width: side, height: side, estimated: true }
}

function sectorForBed(bed: Bed, sectors: readonly IrrigationSector[]) {
  const id = bed.sectorAssignment?.sectorId
  const sector = id ? sectors.find((item) => item.id === id) : undefined
  return { id: id ?? 'unassigned', name: sector?.name ?? 'Sem setor associado' }
}

function projectedBeds(input: InteractiveOrganization, width: number, height: number) {
  const padding = Math.min(0.75, Math.max(0.3, Math.min(width, height) * 0.04))
  const gap = Math.min(0.6, Math.max(0.25, padding * 0.8))
  const elements: DemoElement[] = []
  const groups = new Map<string, { id: string; name: string; beds: Bed[] }>()
  for (const bed of input.beds.filter((item) => item.isActive)) {
    const sector = sectorForBed(bed, input.sectors)
    const group = groups.get(sector.id) ?? { id: sector.id, name: sector.name, beds: [] }
    group.beds.push(bed)
    groups.set(sector.id, group)
  }

  let cursorY = padding
  const sectorBounds = new Map<string, { minX: number; minY: number; maxX: number; maxY: number; areaM2: number; bedIds: string[]; name: string }>()
  for (const group of groups.values()) {
    let cursorX = padding
    let rowHeight = 0
    let groupBounds: { minX: number; minY: number; maxX: number; maxY: number } | undefined
    for (const bed of group.beds) {
      const dimensions = dimensionsForBed(bed)
      const fitsCurrentRow = cursorX + dimensions.width <= width - padding || cursorX === padding
      if (!fitsCurrentRow) {
        cursorX = padding
        cursorY += rowHeight + gap
        rowHeight = 0
      }
      // A projeção preserva a dimensão de negócio mesmo quando os dados não
      // cabem na área da horta. Reduzir aqui esconderia uma inconsistência do
      // cadastro e faria o desenho deixar de representar os metros informados.
      const element: DemoElement = {
        id: bed.id,
        entityId: bed.id,
        sectorId: bed.sectorAssignment?.sectorId,
        sectorName: group.name,
        source: 'catalog',
        isEstimated: dimensions.estimated,
        name: bed.name,
        role: 'bed',
        shape: 'rectangle',
        x: cursorX,
        y: cursorY,
        width: dimensions.width,
        height: dimensions.height,
        rotationDeg: 0,
      }
      elements.push(element)
      const bounds = elementBounds(element)
      groupBounds = groupBounds
        ? { minX: Math.min(groupBounds.minX, bounds.minX), minY: Math.min(groupBounds.minY, bounds.minY), maxX: Math.max(groupBounds.maxX, bounds.maxX), maxY: Math.max(groupBounds.maxY, bounds.maxY) }
        : bounds
      cursorX += dimensions.width + gap
      rowHeight = Math.max(rowHeight, dimensions.height)
    }
    if (groupBounds) {
      const areaM2 = group.beds.reduce((total, bed) => total + (bed.configuration?.cultivableAreaM2 ?? 0), 0)
      sectorBounds.set(group.id, { ...groupBounds, areaM2, bedIds: group.beds.map((bed) => bed.id), name: group.name })
      cursorY += rowHeight + gap * 2
    }
  }

  const sectorPadding = Math.min(1.8, Math.max(0.8, Math.min(width, height) * 0.08))
  const sectors: DemoSector[] = [...sectorBounds].map(([id, bounds]) => ({
    id,
    name: bounds.name,
    areaM2: bounds.areaM2,
    // O setor representa uma área de cultivo, não apenas o contorno dos
    // canteiros. A margem deixa a textura própria do setor visível ao redor
    // das caixas elevadas na visão panorâmica.
    x: Math.max(0, bounds.minX - sectorPadding),
    y: Math.max(0, bounds.minY - sectorPadding),
    width: Math.min(width, bounds.maxX - bounds.minX + sectorPadding * 2),
    height: Math.min(height, bounds.maxY - bounds.minY + sectorPadding * 2),
    bedIds: bounds.bedIds,
    isEstimated: true,
  }))
  return { elements, sectors }
}

/**
 * Cria o envelope visual da horta sem confundir área cadastrada com uma geometria física.
 * A forma quadrada é uma projeção reversível; largura, comprimento e contorno da horta
 * ainda não existem no contrato do CRUD.
 */
function createCatalogProjection(garden: GardenWorkspace, organization?: InteractiveOrganization): GardenProjection {
  const totalAreaM2 = registeredGardenAreaM2(garden)
  if (!totalAreaM2) {
    const fallback = createDemoGarden()
    return {
      garden: { ...fallback, ...(organization ? projectedBeds(organization, fallback.width, fallback.height) : { elements: [] }), areaSource: 'fallback', projectionVersion: PROJECTION_VERSION, projectionKey: projectionKey(organization) },
      totalAreaM2: undefined,
      warnings: ['Medidas reais são opcionais. O quadro usa 20 × 20 unidades livres de desenho.'],
    }
  }

  const side = Math.sqrt(totalAreaM2)
  const projected = organization ? projectedBeds(organization, side, side) : { elements: [], sectors: [] }
  const warnings = ['O envelope quadrado preserva a área cadastrada, mas não representa o contorno físico da horta.']
  if (organization?.beds.some((bed) => bed.isActive && (!bed.configuration?.lengthMeters || !bed.configuration?.widthMeters))) {
    warnings.push('Alguns canteiros usam dimensão quadrada estimada a partir da área cultivável.')
  }
  const configuredBedsAreaM2 = organization?.beds.reduce((total, bed) => total + (bed.isActive ? bed.configuration?.cultivableAreaM2 ?? 0 : 0), 0) ?? 0
  if (configuredBedsAreaM2 > totalAreaM2 + 1e-6) {
    warnings.push(`A área dos canteiros cadastrados (${configuredBedsAreaM2.toLocaleString('pt-BR', { maximumFractionDigits: 2 })} m²) excede a área da horta (${totalAreaM2.toLocaleString('pt-BR', { maximumFractionDigits: 2 })} m²); a sobreposição indica uma inconsistência cadastral.`)
  }
  if (organization?.beds.some((bed) => bed.isActive && !bed.sectorAssignment)) {
    warnings.push('Há canteiros sem setor associado; eles aparecem no agrupamento “Sem setor associado”.')
  }
  return { garden: { width: side, height: side, areaM2: totalAreaM2, areaSource: 'registered', projectionVersion: PROJECTION_VERSION, projectionKey: projectionKey(organization), elements: projected.elements, sectors: projected.sectors }, totalAreaM2, warnings }
}

/** A geometria confirmada prevalece sobre envelopes estimados dos cadastros. */
export function createGardenProjection(garden: GardenWorkspace, organization?: InteractiveOrganization): GardenProjection {
  const projection = createCatalogProjection(garden, organization)
  const saved = organization?.savedLayout
  if (!saved?.layout) return projection
  return { ...projection, warnings: [], garden: { ...projection.garden, ...saved.layout, boundary: saved.layout.boundary ?? undefined, projectionKey: 'saved-layout:' + garden.id + ':' + saved.version } }
}
