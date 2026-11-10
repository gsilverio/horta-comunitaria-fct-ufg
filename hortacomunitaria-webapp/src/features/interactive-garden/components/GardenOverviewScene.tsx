import { useCallback, useEffect, useId, useMemo, useRef, useState } from 'react'
import type { KeyboardEvent as ReactKeyboardEvent, MouseEvent as ReactMouseEvent, PointerEvent as ReactPointerEvent, ReactNode } from 'react'
import { Info, Layers, Maximize2, Minimize2, Minus, Plus, Scan } from 'lucide-react'
import { routeHref } from '../../../hooks/useRoute'
import { useCanvasWheel, wheelZoomFactor } from '../hooks/useCanvasWheel'
import { elementArea, elementBounds, pointInPolygon } from '../geometry'
import type { DemoElement, DemoGarden, RectangleElement } from '../types'
import { bedCropSymbols, cropAsset, cropDensity, stageLabels } from '../cropVisuals'
import type { GardenCrop } from '../cropVisuals'

interface Point { x: number; y: number }
interface Camera {
  centerX: number
  centerY: number
  scale: number
  fitScale: number
  screenCenterY: number
}

interface SceneInteraction {
  pointerId: number
  elementId?: string
  pointerX: number
  pointerY: number
  camera: Camera
  moved: boolean
}

const VIEWBOX = { width: 960, height: 540 }
const CULTIVATED_GRASS_URL = '/assets/pixel-garden/worked-grass-tile-v2.png'
const GROUND_TEXTURE_URL = '/assets/pixel-garden/grass-tile.png'
const BED_TEXTURE_URL = '/assets/pixel-garden/bed-soil-tile.png'
const BED_BOX_TEXTURE_URL = '/assets/pixel-garden/bed-box-wood.png'
const GRASS_TUFT_URL = '/assets/pixel-garden/grass-tuft.png'
const CAMERA_MARGIN_X = 54
const CAMERA_MARGIN_Y = 70
const MIN_ZOOM = 0.8
const MAX_ZOOM = 2.6

function rectanglePoints(element: RectangleElement): Point[] {
  if (element.outline) return element.outline
  const centerX = element.x + element.width / 2
  const centerY = element.y + element.height / 2
  const angle = element.rotationDeg * Math.PI / 180
  const cos = Math.cos(angle)
  const sin = Math.sin(angle)
  return [
    { x: element.x, y: element.y },
    { x: element.x + element.width, y: element.y },
    { x: element.x + element.width, y: element.y + element.height },
    { x: element.x, y: element.y + element.height },
  ].map((point) => ({
    x: centerX + (point.x - centerX) * cos - (point.y - centerY) * sin,
    y: centerY + (point.x - centerX) * sin + (point.y - centerY) * cos,
  }))
}

function curvedPoints(element: RectangleElement, samples = 18): Point[] {
  const placement = element.mandala
  if (!placement) return rectanglePoints(element)
  const innerRadius = Math.max(0, placement.ringRadius - placement.radialLength / 2)
  const outerRadius = placement.ringRadius + placement.radialLength / 2
  const startAngle = placement.angleDeg - placement.angularWidthDeg / 2
  const points: Point[] = []
  for (let index = 0; index <= samples; index += 1) {
    const angle = (startAngle + placement.angularWidthDeg * index / samples) * Math.PI / 180
    points.push({ x: placement.centerX + Math.cos(angle) * outerRadius, y: placement.centerY + Math.sin(angle) * outerRadius })
  }
  for (let index = samples; index >= 0; index -= 1) {
    const angle = (startAngle + placement.angularWidthDeg * index / samples) * Math.PI / 180
    points.push({ x: placement.centerX + Math.cos(angle) * innerRadius, y: placement.centerY + Math.sin(angle) * innerRadius })
  }
  return points
}

function elementCenter(element: DemoElement): Point {
  return element.shape === 'circle'
    ? { x: element.centerX, y: element.centerY }
    : element.mandala?.bedStyle === 'curved'
      ? { x: element.mandala.centerX + Math.cos(element.mandala.angleDeg * Math.PI / 180) * element.mandala.ringRadius, y: element.mandala.centerY + Math.sin(element.mandala.angleDeg * Math.PI / 180) * element.mandala.ringRadius }
      : { x: element.x + element.width / 2, y: element.y + element.height / 2 }
}

function cropPoints(element: DemoElement, count: number): Point[] {
  if (element.role !== 'bed' || count === 0) return []
  if (element.shape === 'circle') return Array.from({ length: count }, (_, index) => {
    const angle = index * 2.3999632297
    const radius = element.radius * 0.72 * Math.sqrt((index + 0.5) / count)
    return { x: element.centerX + Math.cos(angle) * radius, y: element.centerY + Math.sin(angle) * radius }
  })
  if (element.mandala?.bedStyle === 'curved') {
    const { centerX, centerY, ringRadius, angleDeg, angularWidthDeg, radialLength } = element.mandala
    const columns = Math.max(1, Math.ceil(Math.sqrt(count * Math.max(0.2, ringRadius * angularWidthDeg * Math.PI / 180) / radialLength)))
    const rows = Math.ceil(count / columns)
    return Array.from({ length: count }, (_, index) => {
      const angle = (angleDeg + angularWidthDeg * ((index % columns + 0.5) / columns - 0.5) * 0.7) * Math.PI / 180
      const radius = ringRadius + radialLength * ((Math.floor(index / columns) + 0.5) / rows - 0.5) * 0.6
      return { x: centerX + Math.cos(angle) * radius, y: centerY + Math.sin(angle) * radius }
    })
  }
  const center = elementCenter(element)
  const angle = element.rotationDeg * Math.PI / 180
  const columns = Math.max(1, Math.min(count, Math.round(Math.sqrt(count * element.width / element.height))))
  const rows = Math.ceil(count / columns)
  return Array.from({ length: count }, (_, index) => {
    const x = ((index % columns + 0.5) / columns - 0.5) * element.width * 0.7
    const y = ((Math.floor(index / columns) + 0.5) / rows - 0.5) * element.height * 0.7
    return { x: center.x + x * Math.cos(angle) - y * Math.sin(angle), y: center.y + x * Math.sin(angle) + y * Math.cos(angle) }
  })
}

function sceneBounds(garden: DemoGarden) {
  const bounds = garden.elements.map(elementBounds).reduce((current, next) => ({
    minX: Math.min(current.minX, next.minX),
    minY: Math.min(current.minY, next.minY),
    maxX: Math.max(current.maxX, next.maxX),
    maxY: Math.max(current.maxY, next.maxY),
  }), { minX: 0, minY: 0, maxX: garden.width, maxY: garden.height })
  return bounds
}

function cameraFor(garden: DemoGarden): Camera {
  const bounds = sceneBounds(garden)
  const centerX = (bounds.minX + bounds.maxX) / 2
  const centerY = (bounds.minY + bounds.maxY) / 2
  // A visão geral precisa enquadrar também os elementos que vieram de um
  // cadastro antigo ou de uma estimativa maior que o terreno.
  const projectedWidthAtUnitScale = Math.max(1, bounds.maxX - bounds.minX + bounds.maxY - bounds.minY)
  const projectedHeightAtUnitScale = projectedWidthAtUnitScale * 0.56
  const fitScale = Math.min(
    (VIEWBOX.width - CAMERA_MARGIN_X * 2) / projectedWidthAtUnitScale,
    (VIEWBOX.height - CAMERA_MARGIN_Y * 2) / projectedHeightAtUnitScale,
  )
  return { centerX, centerY, scale: fitScale, fitScale, screenCenterY: VIEWBOX.height / 2 + 8 }
}

function project(point: Point, camera: Camera): Point {
  const x = point.x - camera.centerX
  const y = point.y - camera.centerY
  return { x: VIEWBOX.width / 2 + (x - y) * camera.scale, y: camera.screenCenterY + (x + y) * camera.scale * 0.56 }
}

/**
 * Mantém as texturas ancoradas no espaço da horta. Sem essa transformação,
 * o padrão fica preso à tela e parece que o usuário está percorrendo uma
 * imagem infinita ao arrastar a câmera.
 */
function textureTransform(camera: Camera, elevation = 0) {
  const translateX = VIEWBOX.width / 2 - camera.scale * camera.centerX + camera.scale * camera.centerY
  const translateY = camera.screenCenterY - camera.scale * 0.56 * (camera.centerX + camera.centerY) - elevation
  return `matrix(${camera.scale} ${camera.scale * 0.56} ${-camera.scale} ${camera.scale * 0.56} ${translateX} ${translateY})`
}

function unproject(point: Point, camera: Camera): Point {
  const projectedX = (point.x - VIEWBOX.width / 2) / camera.scale
  const projectedY = (point.y - camera.screenCenterY) / (camera.scale * 0.56)
  return { x: camera.centerX + (projectedX + projectedY) / 2, y: camera.centerY + (projectedY - projectedX) / 2 }
}

function pointsAttribute(points: Point[]) {
  return points.map((point) => `${point.x.toFixed(1)},${point.y.toFixed(1)}`).join(' ')
}

function bedWallSegments(points: Point[], depth: number) {
  const area = points.reduce((sum, point, index) => { const next = points[(index + 1) % points.length]; return sum + point.x * next.y - next.x * point.y }, 0)
  const winding = Math.sign(area)
  return points.flatMap((point, index) => {
    const next = points[(index + 1) % points.length]
    // Face visível: normal exterior voltada para a parte inferior da tela.
    if ((next.x - point.x) * winding >= -1e-7) return []
    return [{ point, next, index, wall: [point, next, { x: next.x, y: next.y + depth }, { x: point.x, y: point.y + depth }] }]
  }).sort((a, b) => (a.point.y + a.next.y) - (b.point.y + b.next.y))
}

function groundPoints(garden: DemoGarden, camera: Camera) {
  if (garden.boundary) return garden.boundary.map((point) => project(point, camera))
  return [
    project({ x: 0, y: 0 }, camera),
    project({ x: garden.width, y: 0 }, camera),
    project({ x: garden.width, y: garden.height }, camera),
    project({ x: 0, y: garden.height }, camera),
  ]
}

function sectorPoints(sector: NonNullable<DemoGarden['sectors']>[number], camera: Camera) {
  return [
    project({ x: sector.x, y: sector.y }, camera),
    project({ x: sector.x + sector.width, y: sector.y }, camera),
    project({ x: sector.x + sector.width, y: sector.y + sector.height }, camera),
    project({ x: sector.x, y: sector.y + sector.height }, camera),
  ]
}

function visibleSectors(garden: DemoGarden) {
  const sectors = garden.sectors ?? []
  const padding = Math.min(1.8, Math.max(0.8, Math.min(garden.width, garden.height) * 0.08))
  return sectors.map((sector) => {
    const beds = garden.elements.filter((element) => element.role === 'bed' && (element.sectorId === sector.id || (sectors.length === 1 && !element.sectorId)))
    if (beds.length === 0) return sector
    const bounds = beds.map(elementBounds).reduce((current, next) => ({
      minX: Math.min(current.minX, next.minX),
      minY: Math.min(current.minY, next.minY),
      maxX: Math.max(current.maxX, next.maxX),
      maxY: Math.max(current.maxY, next.maxY),
    }))
    return {
      ...sector,
      x: Math.max(0, bounds.minX - padding),
      y: Math.max(0, bounds.minY - padding),
      width: Math.min(garden.width, bounds.maxX - bounds.minX + padding * 2),
      height: Math.min(garden.height, bounds.maxY - bounds.minY + padding * 2),
      bedIds: beds.map((element) => element.entityId ?? element.id),
    }
  })
}

function elementTitle(element: DemoElement) {
  return element.shape === 'circle' ? `${element.name}, círculo` : element.mandala?.bedStyle === 'curved' ? `${element.name}, canteiro curvilíneo` : `${element.name}, retângulo`
}

function zoomCamera(camera: Camera, factor: number, anchor: Point): Camera {
  const nextScale = Math.max(camera.fitScale * MIN_ZOOM, Math.min(camera.fitScale * MAX_ZOOM, camera.scale * factor))
  if (Math.abs(nextScale - camera.scale) < 0.001) return camera
  const worldAnchor = unproject(anchor, camera)
  const projectedX = (anchor.x - VIEWBOX.width / 2) / nextScale
  const projectedY = (anchor.y - camera.screenCenterY) / (nextScale * 0.56)
  return {
    ...camera,
    scale: nextScale,
    centerX: worldAnchor.x - (projectedX + projectedY) / 2,
    centerY: worldAnchor.y - (projectedY - projectedX) / 2,
  }
}

function panCamera(camera: Camera, deltaX: number, deltaY: number): Camera {
  const projectedX = -deltaX / camera.scale
  const projectedY = -deltaY / (camera.scale * 0.56)
  return { ...camera, centerX: camera.centerX + (projectedX + projectedY) / 2, centerY: camera.centerY + (projectedY - projectedX) / 2 }
}

function formatMeters(value: number) {
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(value)
}

function SceneLife({ element, camera, crops, elevation = 0 }: { element: DemoElement; camera: Camera; crops: readonly GardenCrop[]; elevation?: number }) {
  const symbols = bedCropSymbols(crops)
  const points = cropPoints(element, symbols.length)
  if (points.length === 0) return null
  const spriteSize = Math.max(10, Math.min(32, camera.scale * 1.05))
  return <g className="garden-scene-life" aria-hidden="true" data-crop-symbols={symbols.length}>
    {points.map((point, index) => ({ point, symbol: symbols[index] })).sort((a, b) => (a.point.x + a.point.y) - (b.point.x + b.point.y)).map(({ point, symbol }) => {
      const projected = project(point, camera)
      const asset = cropAsset(symbol.crop)
      return <g key={`${symbol.crop.cycleId}-${symbol.index}`} data-crop-stage={symbol.crop.stage} data-density={cropDensity(symbol.crop)}>
        <image href={asset.url} x={projected.x - spriteSize / 2} y={projected.y - elevation - spriteSize * 1.05} width={spriteSize} height={spriteSize * 1.25} preserveAspectRatio="xMidYMid meet" style={{ imageRendering: 'pixelated' }} pointerEvents="none" />
      </g>
    })}
  </g>
}

function breezePoints(garden: DemoGarden): Point[] {
  const marginX = Math.max(1, garden.width * 0.08)
  const marginY = Math.max(1, garden.height * 0.08)
  return [
    { x: marginX, y: marginY },
    { x: garden.width * 0.28, y: marginY * 0.72 },
    { x: garden.width - marginX * 0.7, y: marginY * 0.9 },
    { x: garden.width - marginX * 0.65, y: garden.height * 0.34 },
    { x: garden.width - marginX * 0.8, y: garden.height - marginY * 0.7 },
    { x: garden.width * 0.68, y: garden.height - marginY * 0.65 },
    { x: marginX * 0.75, y: garden.height - marginY * 0.7 },
    { x: marginX * 0.7, y: garden.height * 0.62 },
  ]
}

function GrassBreeze({ garden, camera, cultivated }: { garden: DemoGarden; camera: Camera; cultivated: Point[][] }) {
  const tuftSize = Math.max(15, Math.min(28, camera.scale * 1.05))
  return <g aria-hidden="true">
    {breezePoints(garden).filter((point) => (!garden.boundary || pointInPolygon(point, garden.boundary)) && !cultivated.some(polygon => pointInPolygon(point, polygon))).map((point, index) => {
      const projected = project(point, camera)
      return <image
        key={`grass-tuft-${index}`}
        className="garden-scene-breeze-tuft"
        href={GRASS_TUFT_URL}
        x={projected.x - tuftSize / 2}
        y={projected.y - tuftSize * 0.78}
        width={tuftSize}
        height={tuftSize}
        preserveAspectRatio="xMidYMid meet"
        style={{ animationDelay: `${index * 240}ms`, imageRendering: 'pixelated' }}
        pointerEvents="none"
      />
    })}
  </g>
}

interface SceneElementProps {
  element: DemoElement
  crops: readonly GardenCrop[]
  camera: Camera
  selected: boolean
  hovered: boolean
  onSelect: (id: string) => void
  onHover: (id: string | undefined) => void
}

function SceneElement({ element, crops, camera, selected, hovered, onSelect, onHover }: SceneElementProps) {
  const textureId = useId()
  const fill = element.role === 'sector' || element.role === 'reference' ? '#91ad72' : '#a97848'
  const stroke = element.role === 'sector' || element.role === 'reference' ? '#d9e8b8' : '#e8c28e'
  const center = project(elementCenter(element), camera)
  const stateClass = `garden-scene-object${selected ? ' selected' : ''}${hovered ? ' hovered' : ''}`
  const handlers = {
    'data-scene-element-id': element.id,
    onPointerEnter: () => onHover(element.id),
    onPointerLeave: () => onHover(undefined),
    onClick: (event: ReactMouseEvent<SVGGElement>) => {
      event.preventDefault()
      event.stopPropagation()
      onSelect(element.id)
    },
    onKeyDown: (event: ReactKeyboardEvent<SVGGElement>) => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault()
        onSelect(element.id)
      }
    },
  }
  if (element.role === 'sector') {
    const points = element.shape === 'circle' ? Array.from({ length: 48 }, (_, i) => project({ x: element.centerX + Math.cos(i * Math.PI / 24) * element.radius, y: element.centerY + Math.sin(i * Math.PI / 24) * element.radius }, camera)) : rectanglePoints(element).map((point) => project(point, camera))
    return <g className={stateClass + ' garden-scene-sector'} role="button" tabIndex={0} aria-pressed={selected} aria-label={elementTitle(element)} {...handlers}><polygon points={pointsAttribute(points)} fill="none" stroke={selected || hovered ? '#fff0b6' : '#e4edc5'} strokeWidth={selected || hovered ? 2.5 : 1.5} strokeDasharray="5 5" strokeLinejoin="round" pointerEvents="stroke" /><title>{element.name}</title></g>
  }
  if (element.shape === 'circle' && element.role !== 'bed') {
    const radiusX = element.radius * camera.scale
    const radiusY = element.radius * camera.scale * 0.56
    return <g className={stateClass} role="button" tabIndex={0} aria-pressed={selected} aria-label={elementTitle(element)} {...handlers}>
      <ellipse cx={center.x + 5} cy={center.y + 7} rx={radiusX} ry={radiusY} className="garden-scene-object-shadow" />
      <ellipse cx={center.x} cy={center.y} rx={radiusX} ry={radiusY} fill={fill} stroke={stroke} strokeWidth="2" />
      <SceneLife element={element} crops={crops} camera={camera} />
      <title>{elementTitle(element)}</title>
    </g>
  }
  const points = element.shape === 'circle' ? Array.from({ length: 48 }, (_, i) => ({ x: element.centerX + Math.cos(i * Math.PI / 24) * element.radius, y: element.centerY + Math.sin(i * Math.PI / 24) * element.radius })) : element.mandala?.bedStyle === 'curved' ? curvedPoints(element) : rectanglePoints(element)
  const wallDepth = camera.scale * 0.35
  const footprint = points.map((point) => project(point, camera))
  const projected = footprint.map((point) => ({ ...point, y: point.y - wallDepth }))
  const shadow = footprint.map((point) => ({ x: point.x + 3, y: point.y + 4 }))
  const isBed = element.role === 'bed'
  const walls = isBed ? bedWallSegments(projected, wallDepth) : []
  return <g className={stateClass} role="button" tabIndex={0} aria-pressed={selected} aria-label={elementTitle(element)} {...handlers}>
    <polygon points={pointsAttribute(shadow)} className="garden-scene-object-shadow" />
    {walls.map(({ wall, point, next, index }) => {
      const length = Math.hypot(points[(index + 1) % points.length].x - points[index].x, points[(index + 1) % points.length].y - points[index].y)
      const patternId = textureId + '-wall-' + index
      return <g key={index} className="garden-scene-bed-wall" data-wall-face={index}>
        <defs><pattern id={patternId} width="2.8" height="1.064" patternUnits="userSpaceOnUse" patternTransform={`matrix(${(next.x - point.x) / length} ${(next.y - point.y) / length} 0 ${camera.scale} ${point.x} ${point.y})`}>
          <svg width="2.8" height="1.064" viewBox="11 116 1753 666" preserveAspectRatio="none"><image href={BED_BOX_TEXTURE_URL} width="1774" height="887" /></svg>
        </pattern></defs>
        <polygon points={pointsAttribute(wall)} fill="#9e673b" />
        <polygon points={pointsAttribute(wall)} fill={`url(#${patternId})`} />
        <polygon points={pointsAttribute(wall)} fill="#3b2518" opacity={next.y > point.y ? '.22' : '.06'} pointerEvents="none" />
        <path d={`M ${point.x} ${point.y + wallDepth} L ${next.x} ${next.y + wallDepth}`} stroke="#573921" strokeWidth="1" />
      </g>
    })}
    <polygon points={pointsAttribute(projected)} fill={fill} stroke={isBed ? '#4a2919' : stroke} strokeWidth={isBed ? 2 : selected ? 3 : 2} strokeLinejoin="round" />
    {isBed && <polygon points={pointsAttribute(projected)} fill="url(#garden-scene-bed-texture)" opacity=".94" />}
    {isBed && <polygon points={pointsAttribute(projected)} fill="none" stroke="#cf8542" strokeWidth={selected ? 2.5 : 2} strokeLinejoin="round" />}
    {walls.map(({ point, next, index }) => <path key={`${element.id}-rim-${index}`} d={`M ${point.x} ${point.y} L ${next.x} ${next.y}`} stroke="#e2a35b" strokeWidth="3.4" strokeLinecap="round" opacity=".9" pointerEvents="none" />)}

    {!isBed && <path d={`M ${projected[0].x} ${projected[0].y} L ${projected[1].x} ${projected[1].y}`} stroke="#f5dcae" strokeWidth="2" opacity=".65" />}
    <SceneLife element={element} crops={crops} camera={camera} elevation={wallDepth} />
    <title>{elementTitle(element)}</title>
  </g>
}

function elementRole(element: Pick<DemoElement, 'role'>) {
  return element.role === 'sector' ? 'Setor' : element.role === 'bed' ? 'Canteiro' : 'Referência'
}

interface SceneLabelItem { id: string; role: DemoElement['role']; name: string; center: Point }
interface LabelBox { x: number; y: number; width: number; height: number }

const LABEL_FONT = '"Segoe UI", Inter, Arial, sans-serif'
let measureContext: CanvasRenderingContext2D | null | undefined
/** Mede o texto com a fonte real da placa; sem canvas disponível, usa uma estimativa conservadora. */
function textWidth(text: string, size: number, weight: number, tracking = 0) {
  if (measureContext === undefined) measureContext = typeof document === 'undefined' ? null : document.createElement('canvas').getContext('2d')
  if (!measureContext) return text.length * size * 0.6 + text.length * tracking
  measureContext.font = `${weight} ${size}px ${LABEL_FONT}`
  return measureContext.measureText(text).width + text.length * tracking
}

const LABEL_HEIGHT = 22
function labelLayout(kind: string, name: string) {
  const kindWidth = textWidth(kind.toLocaleUpperCase('pt-BR'), 8.5, 700, 0.8)
  const nameWidth = textWidth(name, 11.5, 600)
  return { kindX: 19, nameX: 19 + kindWidth + 6, width: Math.ceil(19 + kindWidth + 6 + nameWidth + 11) }
}

function overlaps(a: LabelBox, b: LabelBox, gap: number) {
  return !(a.x + a.width + gap <= b.x || b.x + b.width + gap <= a.x || a.y + a.height + gap <= b.y || b.y + b.height + gap <= a.y)
}

/**
 * Placas de identificação: pílula do tamanho do texto, marcador por tipo e conector curto até o canteiro.
 * Selecionado e canteiros têm prioridade; uma placa sem espaço livre é omitida em vez de cobrir outra.
 */
function SceneLabels({ items, camera, selectedId, scale }: { items: SceneLabelItem[]; camera: Camera; selectedId?: string; scale: number }) {
  const occupied: LabelBox[] = []
  const height = LABEL_HEIGHT * scale, gap = 5 * scale, margin = 6 * scale
  const priority = (item: SceneLabelItem) => item.id === selectedId ? 0 : item.role === 'bed' ? 1 : 2
  const sorted = [...items].sort((a, b) => priority(a) - priority(b))
  return <g aria-label="Informações dos elementos" pointerEvents="none">{sorted.map(item => {
    const kind = elementRole(item)
    const name = item.name.length > 28 ? item.name.slice(0, 27) + '…' : item.name
    const layout = labelLayout(kind, name)
    const width = layout.width * scale
    const isBed = item.role === 'bed'
    const center = project(item.center, camera)
    const anchor = { x: center.x, y: center.y - (isBed ? camera.scale * 0.35 : 0) }
    if (anchor.x < 0 || anchor.x > VIEWBOX.width || anchor.y < 0 || anchor.y > VIEWBOX.height) return null
    const lift = isBed ? 12 * scale : 0
    const baseY = isBed ? anchor.y - lift - height : anchor.y - height / 2
    const rows = isBed ? [0, -1, -2, 1, 2, 3] : [0, 1, -1, 2, -2]
    const shifts = [0, -0.55, 0.55, -1.05, 1.05]
    const preferredX = anchor.x - width / 2
    // Prefere a posição mais próxima do canteiro: subir ou descer antes de deslocar para o lado.
    const candidates = rows.flatMap(row => shifts.map(shift => ({
      x: Math.max(margin, Math.min(VIEWBOX.width - width - margin, preferredX + shift * width)),
      y: baseY + row * (height + gap), width, height,
    }))).sort((a, b) => Math.abs(a.x - preferredX) * 1.4 + Math.abs(a.y - baseY) - (Math.abs(b.x - preferredX) * 1.4 + Math.abs(b.y - baseY)))
    const position = candidates.find(box => box.y > margin && box.y + height < VIEWBOX.height - 58 && occupied.every(other => !overlaps(box, other, gap)))
    if (!position) return null
    occupied.push(position)
    const selected = item.id === selectedId
    const below = position.y > anchor.y
    const connector = { x: Math.max(position.x + height / 2, Math.min(position.x + width - height / 2, anchor.x)), y: below ? position.y : position.y + height }
    return <g key={item.id} className={selected ? 'garden-scene-label selected' : 'garden-scene-label'} data-role={item.role}>
      {isBed && <><line className="garden-scene-label-leader" x1={connector.x} y1={connector.y} x2={anchor.x} y2={anchor.y} /><circle className="garden-scene-label-anchor" cx={anchor.x} cy={anchor.y} r={2.2 * Math.min(scale, 1.6)} /></>}
      <g transform={`translate(${position.x}, ${position.y}) scale(${scale})`}>
        <rect width={layout.width} height={LABEL_HEIGHT} rx={LABEL_HEIGHT / 2} />
        <circle className="garden-scene-label-marker" cx="10.5" cy={LABEL_HEIGHT / 2} r="3.2" />
        <text x={layout.kindX} y="14.4" className="garden-scene-label-kind">{kind}</text>
        <text x={layout.nameX} y="15.2" className="garden-scene-label-name">{name}</text>
      </g>
      <title>{kind}: {item.name}</title>
    </g>
  })}</g>
}

function elementKind(element: DemoElement) {
  if (element.shape === 'circle') return 'Círculo'
  if (element.mandala?.bedStyle === 'curved') return 'Canteiro curvilíneo'
  if (element.mandala) return 'Canteiro radial'
  return 'Retângulo'
}

const EMPTY_CROPS: readonly GardenCrop[] = []

/**
 * Viewport da panorâmica: a cena ocupa a área inteira e os controles ficam em um HUD sobreposto.
 * O motor (projeção, câmera, assets, fases e densidade) permanece o mesmo; muda apenas a moldura e a interação ao redor.
 */
export function GardenOverviewScene({ gardenId, garden, crops, heading, actions, legendNote }: { gardenId: string; garden: DemoGarden; crops?: readonly GardenCrop[]; heading?: ReactNode; actions?: ReactNode; legendNote?: ReactNode }) {
  const wrapperRef = useRef<HTMLDivElement>(null)
  const [fullscreen, setFullscreen] = useState(false)
  const [legendOpen, setLegendOpen] = useState(false)
  const canFullscreen = typeof document !== 'undefined' && document.fullscreenEnabled
  useEffect(() => {
    const sync = () => setFullscreen(document.fullscreenElement === wrapperRef.current)
    document.addEventListener('fullscreenchange', sync)
    return () => document.removeEventListener('fullscreenchange', sync)
  }, [])
  const toggleFullscreen = () => { if (document.fullscreenElement) void document.exitFullscreen(); else void wrapperRef.current?.requestFullscreen() }
  const cropsByBed = useMemo(() => {
    const grouped = new Map<string, GardenCrop[]>()
    for (const crop of crops ?? []) grouped.set(crop.bedId, [...(grouped.get(crop.bedId) ?? []), crop])
    return grouped
  }, [crops])
  const fitCamera = useMemo(() => cameraFor(garden), [garden])
  const sceneElements = useMemo(() => [...garden.elements].sort((left, right) => {
    const layer = (element: DemoElement) => element.role === 'bed' ? 1 : 0
    const a = elementCenter(left), b = elementCenter(right)
    return layer(left) - layer(right) || a.x + a.y - b.x - b.y
  }), [garden.elements])
  const [cameraState, setCamera] = useState(fitCamera)
  const animation = useRef<number | undefined>(undefined)
  const [focusing, setFocusing] = useState(false)
  useEffect(() => () => { if (animation.current !== undefined) cancelAnimationFrame(animation.current) }, [])
  const [selectedId, setSelectedId] = useState<string>()
  const [hoveredId, setHoveredId] = useState<string>()
  const [showSectors, setShowSectors] = useState(true)
  const [showInformation, setShowInformation] = useState(false)
  const sceneRef = useRef<SVGSVGElement | null>(null)
  const [labelScale, setLabelScale] = useState(1)
  useEffect(() => {
    const svg = sceneRef.current
    if (!svg) return
    const observer = new ResizeObserver(() => {
      const rect = svg.getBoundingClientRect()
      const renderedScale = Math.min(rect.width / VIEWBOX.width, rect.height / VIEWBOX.height)
      if (renderedScale > 0) setLabelScale(Math.max(1, Math.min(3.2, 1 / renderedScale)))
    })
    observer.observe(svg)
    return () => observer.disconnect()
  }, [])
  const interaction = useRef<SceneInteraction | undefined>(undefined)
  const suppressClick = useRef(false)
  const [dragging, setDragging] = useState(false)
  // A projeção chega depois do primeiro render em alguns fluxos. Enquanto o
  // estado ainda aponta para o envelope anterior, usa o novo enquadramento
  // diretamente; assim a câmera não fica presa no fallback inicial.
  const camera = cameraState.fitScale === fitCamera.fitScale ? cameraState : fitCamera
  const selectedElement = useMemo(() => garden.elements.find((element) => element.id === selectedId), [garden.elements, selectedId])
  const zoomPercent = Math.round(camera.scale / camera.fitScale * 100)

  const stopFocus = () => {
    if (animation.current !== undefined) cancelAnimationFrame(animation.current)
    animation.current = undefined
    setFocusing(false)
  }
  const changeCamera = (next: Camera) => { stopFocus(); setCamera(next) }
  const selectElement = (id?: string) => {
    stopFocus()
    setSelectedId(id)
    const element = garden.elements.find(item => item.id === id)
    if (!element || element.role !== 'bed') return
    const center = elementCenter(element), bounds = elementBounds(element)
    const width = sceneRef.current?.getBoundingClientRect().width ?? VIEWBOX.width
    const overlay = window.matchMedia('(min-width: 640px)').matches ? Math.min(300, Math.max(0, width - 136)) / width : 0
    const projectedSize = Math.max(.1, bounds.maxX - bounds.minX + bounds.maxY - bounds.minY)
    const scale = Math.max(camera.fitScale * MIN_ZOOM, Math.min(camera.fitScale * MAX_ZOOM, Math.max(camera.scale, camera.fitScale * 1.7), (VIEWBOX.width * (1 - overlay) - 80) / projectedSize, 260 / (projectedSize * .56 + .35)))
    const centered = { ...camera, centerX: center.x, centerY: center.y, scale, screenCenterY: VIEWBOX.height * .54 + scale * .35 }
    const target = panCamera(centered, VIEWBOX.width * overlay / 2, 0)
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) { setCamera(target); return }
    const start = performance.now(), from = camera
    setFocusing(true)
    const frame = (now: number) => {
      const progress = Math.min(1, (now - start) / 320), eased = 1 - (1 - progress) ** 3
      setCamera({ ...target, centerX: from.centerX + (target.centerX - from.centerX) * eased, centerY: from.centerY + (target.centerY - from.centerY) * eased, scale: from.scale + (target.scale - from.scale) * eased, screenCenterY: from.screenCenterY + (target.screenCenterY - from.screenCenterY) * eased })
      if (progress < 1) animation.current = requestAnimationFrame(frame)
      else { animation.current = undefined; setFocusing(false) }
    }
    animation.current = requestAnimationFrame(frame)
  }

  const onWheel = (event: WheelEvent, canvas: SVGSVGElement) => {
    const rect = canvas.getBoundingClientRect()
    const scale = Math.min(rect.width / VIEWBOX.width, rect.height / VIEWBOX.height)
    const anchor = { x: (event.clientX - rect.left - (rect.width - VIEWBOX.width * scale) / 2) / scale, y: (event.clientY - rect.top - (rect.height - VIEWBOX.height * scale) / 2) / scale }
    changeCamera(zoomCamera(camera, wheelZoomFactor(event), anchor))
  }
  const wheelRef = useCanvasWheel(onWheel)
  const attachCanvas = useCallback((svg: SVGSVGElement | null) => { sceneRef.current = svg; wheelRef(svg) }, [wheelRef])

  const localPoint = (event: ReactPointerEvent<SVGSVGElement>) => {
    const matrix = event.currentTarget.getScreenCTM()
    return matrix ? new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse()) : undefined
  }

  const endPointerInteraction = (event: ReactPointerEvent<SVGSVGElement>) => {
    const current = interaction.current
    if (!current || current.pointerId !== event.pointerId) return
    interaction.current = undefined
    suppressClick.current = true
    if (event.type === 'pointerup' && !current.moved) selectElement(current.elementId)
    setDragging(false)
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId)
  }

  const onPointerDown = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (!event.isPrimary || event.button !== 0 && event.button !== 1 || interaction.current) return
    stopFocus()
    const point = localPoint(event)
    if (!point) return
    event.preventDefault()
    event.stopPropagation()
    suppressClick.current = false
    event.currentTarget.setPointerCapture(event.pointerId)
    const target = event.target instanceof Element ? event.target.closest('[data-scene-element-id]') : null
    interaction.current = { pointerId: event.pointerId, elementId: target?.getAttribute('data-scene-element-id') ?? undefined, pointerX: point.x, pointerY: point.y, camera, moved: false }
    setDragging(true)
  }

  const onPointerMove = (event: ReactPointerEvent<SVGSVGElement>) => {
    const current = interaction.current
    if (!current || current.pointerId !== event.pointerId) return
    const rect = event.currentTarget.getBoundingClientRect()
    if (event.buttons === 0 || event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) {
      endPointerInteraction(event)
      return
    }
    const point = localPoint(event)
    if (!point) return
    event.preventDefault()
    event.stopPropagation()
    const deltaX = point.x - current.pointerX
    const deltaY = point.y - current.pointerY
    current.moved ||= Math.hypot(deltaX, deltaY) > 4
    if (current.moved) setCamera(panCamera(current.camera, deltaX, deltaY))
  }

  const sceneCenter = { x: VIEWBOX.width / 2, y: VIEWBOX.height / 2 }
  const onSceneKeyDown = (event: ReactKeyboardEvent<SVGSVGElement>) => {
    if (event.altKey || event.ctrlKey || event.metaKey) return
    const step = 48
    const commands: Record<string, () => void> = {
      '+': () => changeCamera(zoomCamera(camera, 1.22, sceneCenter)),
      '=': () => changeCamera(zoomCamera(camera, 1.22, sceneCenter)),
      '-': () => changeCamera(zoomCamera(camera, 0.82, sceneCenter)),
      '0': () => changeCamera(fitCamera),
      ArrowLeft: () => changeCamera(panCamera(camera, step, 0)),
      ArrowRight: () => changeCamera(panCamera(camera, -step, 0)),
      ArrowUp: () => changeCamera(panCamera(camera, 0, step)),
      ArrowDown: () => changeCamera(panCamera(camera, 0, -step)),
      Escape: () => selectElement(undefined),
    }
    const command = commands[event.key]
    if (!command) return
    event.preventDefault()
    command()
  }
  const sectorCount = garden.elements.filter(element => element.role === 'sector').length || (garden.sectors?.length ?? 0)
  const bedCount = garden.elements.filter(element => element.role === 'bed').length
  const selectedArea = selectedElement ? elementArea(selectedElement) : 0
  const ground = groundPoints(garden, camera)
  const sceneSectors = garden.elements.some((element) => element.role === 'sector') ? [] : visibleSectors(garden)
  const plantedBeds = garden.elements.filter(element => element.role === 'bed' && element.entityId && (cropsByBed.get(element.entityId) ?? []).some(crop => Boolean(crop.firstPlantedOn) && crop.stage !== 'AwaitingPlanting'))
  const soilAreas = garden.elements.filter(element => element.role === 'sector' && plantedBeds.some(bed => bed.sectorId === element.id)).map(element => ({ id: element.id, points: element.shape === 'rectangle' ? rectanglePoints(element) : Array.from({ length: 64 }, (_, i) => ({ x: element.centerX + Math.cos(i * Math.PI / 32) * element.radius, y: element.centerY + Math.sin(i * Math.PI / 32) * element.radius })) }))
  const cultivated = [...soilAreas, ...sceneSectors.filter(sector => plantedBeds.some(bed => sector.bedIds.includes(bed.entityId ?? bed.id))).map(sector => ({ id: sector.id, points: [{ x: sector.x, y: sector.y }, { x: sector.x + sector.width, y: sector.y }, { x: sector.x + sector.width, y: sector.y + sector.height }, { x: sector.x, y: sector.y + sector.height }] }))]
  const labelItems: SceneLabelItem[] = showInformation ? [
    ...sceneElements.filter(element => showSectors || element.role !== 'sector').map(element => ({ id: element.id, role: element.role, name: element.name, center: elementCenter(element) })),
    ...(showSectors ? sceneSectors.map(sector => ({ id: `sector-${sector.id}`, role: 'sector' as const, name: sector.name, center: { x: sector.x + sector.width / 2, y: sector.y + sector.height / 2 } })) : []),
  ] : []
  return <div ref={wrapperRef} className="garden-scene-wrapper relative" data-fullscreen={fullscreen}><div className="garden-scene" aria-label="Visão panorâmica interativa da implantação da horta">
    <svg viewBox={`0 0 ${VIEWBOX.width} ${VIEWBOX.height}`} preserveAspectRatio="xMidYMid meet" role="application" tabIndex={0} aria-label="Mapa panorâmico interativo da horta" aria-describedby="garden-scene-keys" onKeyDown={onSceneKeyDown} ref={attachCanvas} data-dragging={dragging} data-camera-moving={focusing} onClickCapture={(event) => { if (suppressClick.current) { event.preventDefault(); event.stopPropagation(); suppressClick.current = false } }} onClick={(event) => { if (!(event.target instanceof Element && event.target.closest('[role="button"]'))) selectElement(undefined) }} onLostPointerCapture={endPointerInteraction} onPointerDown={onPointerDown} onPointerMove={onPointerMove} onPointerUp={endPointerInteraction} onPointerCancel={endPointerInteraction}>
      <defs>
        <radialGradient id="garden-scene-backdrop" cx="50%" cy="46%" r="75%"><stop offset="0" stopColor="#2c4637" /><stop offset=".55" stopColor="#1a2d23" /><stop offset="1" stopColor="#0f1c16" /></radialGradient>
        <pattern id="garden-scene-ground-texture" width="5" height="5" patternUnits="userSpaceOnUse" patternTransform={textureTransform(camera)}>
          <image href={GROUND_TEXTURE_URL} x="0" y="0" width="5" height="5" preserveAspectRatio="none" style={{ imageRendering: 'pixelated' }} />
        </pattern>

        <pattern id="garden-scene-bed-texture" width="3" height="3" patternUnits="userSpaceOnUse" patternTransform={textureTransform(camera, camera.scale * 0.35)}>
          <image href={BED_TEXTURE_URL} x="0" y="0" width="3" height="3" preserveAspectRatio="none" style={{ imageRendering: 'pixelated' }} />
        </pattern>

        <filter id="garden-scene-grass-blend" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation={camera.scale * 0.13} /></filter>
        {cultivated.map((area, index) => <mask key={area.id} id={`garden-scene-worked-grass-${index}`} maskUnits="userSpaceOnUse" x="0" y="0" width={VIEWBOX.width} height={VIEWBOX.height} style={{ maskType: 'luminance' }}><polygon points={pointsAttribute(area.points.map(point => project(point, camera)))} fill="white" stroke="black" strokeWidth={Math.min(.9, Math.min(Math.max(...area.points.map(p => p.x)) - Math.min(...area.points.map(p => p.x)), Math.max(...area.points.map(p => p.y)) - Math.min(...area.points.map(p => p.y))) * .18) * camera.scale} filter="url(#garden-scene-grass-blend)" /></mask>)}
        <clipPath id="garden-scene-terrain-clip"><polygon points={pointsAttribute(ground)} /></clipPath>
        <pattern id="garden-scene-cultivated-texture" width="5" height="5" patternUnits="userSpaceOnUse" patternTransform={textureTransform(camera)}><image href={CULTIVATED_GRASS_URL} width="5" height="5" preserveAspectRatio="none" style={{ imageRendering: 'pixelated' }} /></pattern>
        <filter id="garden-scene-soft-shadow" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="7" /></filter>
      </defs>
      <rect width={VIEWBOX.width} height={VIEWBOX.height} fill="url(#garden-scene-backdrop)" />
      <polygon points={pointsAttribute(ground.map(point => ({ x: point.x, y: point.y + camera.scale * .45 + 10 })))} fill="#050c08" opacity=".45" filter="url(#garden-scene-soft-shadow)" />
      <polygon points={pointsAttribute(ground.map(point => ({ x: point.x, y: point.y + camera.scale * .45 })))} fill="#4a3424" stroke="#2b1e14" strokeWidth="1" strokeLinejoin="round" />
      <polygon points={pointsAttribute(ground)} fill="#4b8246" stroke="#1f3a2a" strokeWidth="1.5" strokeLinejoin="round" />
      <polygon points={pointsAttribute(ground)} fill="url(#garden-scene-ground-texture)" opacity=".84" stroke="#a9c98a" strokeOpacity=".45" strokeWidth="1.2" strokeLinejoin="round" />
      <g className="garden-scene-soil" clipPath="url(#garden-scene-terrain-clip)" pointerEvents="none">{cultivated.map((area, index) => <polygon key={area.id} data-sector-soil={area.id} mask={`url(#garden-scene-worked-grass-${index})`} opacity=".85" points={pointsAttribute(area.points.map(point => project(point, camera)))} fill="url(#garden-scene-cultivated-texture)" />)}</g>
      <GrassBreeze garden={garden} camera={camera} cultivated={cultivated.map(area => area.points)} />
      {showSectors && sceneSectors.map((sector) => {
        const points = sectorPoints(sector, camera)
        return <g key={`overview-sector-${sector.id}`} className="garden-scene-sector" pointerEvents="none">
          <polygon points={pointsAttribute(points)} fill="none" stroke="#e4edc5" strokeWidth="1.5" strokeDasharray="5 5" />
        </g>
      })}
      {sceneElements.filter((element) => showSectors || element.role !== 'sector').map((element) => <SceneElement key={element.id} element={element} crops={element.entityId ? cropsByBed.get(element.entityId) ?? EMPTY_CROPS : EMPTY_CROPS} camera={camera} selected={selectedId === element.id} hovered={hoveredId === element.id} onSelect={selectElement} onHover={setHoveredId} />)}
      {showInformation && <SceneLabels items={labelItems} camera={camera} selectedId={selectedId} scale={labelScale} />}
    </svg>
    <p id="garden-scene-keys" className="sr-only">Arraste para mover. Use as setas para mover, mais e menos para aproximar ou afastar, zero para reenquadrar e Esc para fechar os detalhes.</p>
    <div className="scene-hud-top">
      {heading && <div className={`scene-hud-title${selectedElement ? ' max-sm:hidden sm:invisible' : ''}`}>
        {heading}
        <ul className="scene-hud-stats" aria-label="Resumo da cena">
          <li><strong>{sectorCount}</strong> {sectorCount === 1 ? 'setor' : 'setores'}</li>
          <li><strong>{bedCount}</strong> {bedCount === 1 ? 'canteiro' : 'canteiros'}</li>
          <li><strong>{plantedBeds.length}</strong> com cultivo</li>
        </ul>
      </div>}
      <div className="scene-hud-actions">
        {actions}
        {canFullscreen && <button type="button" className="scene-hud-button" aria-pressed={fullscreen} aria-label={fullscreen ? 'Sair da tela cheia' : 'Abrir panorâmica em tela cheia'} onClick={toggleFullscreen}>{fullscreen ? <Minimize2 aria-hidden="true" /> : <Maximize2 aria-hidden="true" />}<span className="max-md:sr-only">{fullscreen ? 'Sair' : 'Tela cheia'}</span></button>}
      </div>
    </div>
    <div className="garden-scene-controls scene-dock" role="group" aria-label="Controles da câmera panorâmica">
      <button type="button" aria-label="Afastar câmera" title="Afastar (−)" onClick={() => changeCamera(zoomCamera(camera, 0.82, sceneCenter))}><Minus aria-hidden="true" /></button>
      <span className="scene-dock-zoom" aria-live={focusing ? 'off' : 'polite'}>{zoomPercent}%</span>
      <button type="button" aria-label="Aproximar câmera" title="Aproximar (+)" onClick={() => changeCamera(zoomCamera(camera, 1.22, sceneCenter))}><Plus aria-hidden="true" /></button>
      <button type="button" className="garden-scene-fit" title="Reenquadrar (0)" onClick={() => changeCamera(fitCamera)}><Scan aria-hidden="true" /><span className="max-sm:sr-only">Enquadrar horta</span></button>
      <span className="scene-dock-divider" aria-hidden="true" />
      <button type="button" className="scene-dock-toggle" aria-pressed={showSectors} aria-label="Mostrar limites dos setores" onClick={() => setShowSectors((current) => !current)}><Layers aria-hidden="true" /><span className="max-sm:hidden">Setores</span></button>
      <label className="scene-dock-toggle" data-checked={showInformation}><input type="checkbox" className="scene-switch" aria-label="Mostrar informações no mapa" checked={showInformation} onChange={event => setShowInformation(event.target.checked)} /><span className="max-sm:hidden" aria-hidden="true">Rótulos</span></label>
      <button type="button" className="scene-dock-toggle" aria-expanded={legendOpen} aria-controls="garden-scene-legend" onClick={() => setLegendOpen(current => !current)}><Info aria-hidden="true" /><span className="max-sm:sr-only">Legenda</span></button>
    </div>
    {legendOpen && <section id="garden-scene-legend" className="scene-legend" aria-label="Legenda da cena">
      <h3>Fases do cultivo</h3>
      <ul>{Object.entries(stageLabels).map(([stage, label]) => <li key={stage}>{label}</li>)}</ul>
      <h3>Como ler</h3>
      <p>Norte para o topo da cena. Desenho sem escala métrica; posições e formas vêm da última configuração salva.</p>
      {legendNote}
      <h3>Navegação</h3>
      <p>Arraste para mover · roda do mouse, + e − para zoom · 0 reenquadra · setas movem a câmera · Esc fecha detalhes.</p>
    </section>}
  </div>
    {selectedElement && <aside className="garden-scene-inspector" aria-live="polite" aria-label={`Dados de ${selectedElement.name}`}>
      <div className="garden-scene-inspector-header"><div><span className="eyebrow">{elementRole(selectedElement)} selecionado</span><strong>{selectedElement.name}</strong></div><button type="button" aria-label="Fechar detalhes do elemento" onClick={() => selectElement(undefined)}>×</button></div>
      <dl className="garden-scene-facts">
        <div><dt>Formato</dt><dd>{elementKind(selectedElement)}</dd></div>
        {selectedElement.entityId && <div><dt>Origem</dt><dd>{selectedElement.role === 'sector' ? 'Setor cadastrado' : 'Canteiro cadastrado'}</dd></div>}
        {selectedElement.sectorName && <div><dt>Setor</dt><dd>{selectedElement.sectorName}</dd></div>}
        <div><dt>Área desenhada</dt><dd>{formatMeters(selectedArea)} u²</dd></div>
        {selectedElement.shape === 'circle' && <div><dt>Raio</dt><dd>{formatMeters(selectedElement.radius)} u</dd></div>}
        {selectedElement.shape === 'rectangle' && !selectedElement.mandala && <div><dt>Dimensões</dt><dd>{formatMeters(selectedElement.width)} × {formatMeters(selectedElement.height)} u</dd></div>}
        {selectedElement.shape === 'rectangle' && selectedElement.mandala && <div><dt>Posição radial</dt><dd>Raio {formatMeters(selectedElement.mandala.ringRadius)} u · {formatMeters(selectedElement.mandala.angleDeg)}°</dd></div>}
        {selectedElement.isEstimated && <div><dt>Dimensão</dt><dd>Estimativa a partir da área cadastrada</dd></div>}
        {selectedElement.role === 'bed' && !selectedElement.entityId && <div><dt>Cultivo</dt><dd>Elemento do rascunho, sem vínculo com plantio</dd></div>}
        {selectedElement.role === 'bed' && selectedElement.entityId && <div><dt>Cultivos</dt><dd>{crops === undefined ? 'Dados de cultivo indisponíveis' : cropsByBed.get(selectedElement.entityId)?.length ? 'Conforme registros na data da cena' : 'Nenhum cultivo vigente'}</dd></div>}
      </dl>
      {selectedElement.role === 'bed' && selectedElement.entityId && <a className="action-link mt-3 w-full justify-center" href={routeHref(gardenId, 'horta', 'canteiros', selectedElement.entityId, new URLSearchParams({ returnTo: 'panorama' }))}>Ver detalhes do canteiro</a>}
      {selectedElement.entityId && cropsByBed.get(selectedElement.entityId)?.map(crop => <div key={crop.cycleId} className="mt-3 border-t border-horta-line pt-3 text-sm">
        <strong>{crop.speciesName}{crop.cultivarName ? ` · ${crop.cultivarName}` : ''}</strong>
        <p className="my-1">{stageLabels[crop.stage]}</p>
        <p className="my-1">{crop.initialQuantity === null ? 'Quantidade inicial não informada' : `${crop.initialQuantity.toLocaleString('pt-BR')} plantas iniciais · densidade visual ${cropDensity(crop)?.toLocaleLowerCase('pt-BR') ?? 'indisponível'}`}</p>
        {crop.hasReplacements && <p className="my-1">Há reposições; não somadas à referência visual. Sobrevivência não inferida.</p>}
        {cropAsset(crop).generic ? <p className="my-1">Ícone genérico: espécie sem asset específico.</p> : crop.cultivarName && cropAsset(crop).sharedWithSpecies && <p className="my-1">Ilustração compartilhada com a espécie.</p>}
      </div>)}
    </aside>}
  </div>
}
