import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { KeyboardEvent as ReactKeyboardEvent, PointerEvent as ReactPointerEvent } from 'react'
import type { RecordRoute } from '../../../types/records'
import { registerNavigationGuard } from '../../../services/navigationGuard'
import { routeHref } from '../../../hooks/useRoute'
import { useGardenState } from '../../../hooks/useGardenState'
import { useRemote } from '../../../hooks/useRemote'
import { Failure } from '../../../components/Feedback'
import { today } from '../../irrigation/formValues'
import { alignElement, clampElement, clampZoom, cloneElements, conflictIds, DEFAULT_MANDALA_CONFIG, elementArea, elementBounds, elementsOverlap, validBoundary, boundaryContainsElement, baseScale, generateMandalaElements, normalizeAngle, sameElements, screenToWorld, snap, snapRightAngle, updateMandalaPlacement, validateMandalaConfig, worldToScreen } from '../geometry'
import { useCanvasWheel, wheelZoomFactor } from '../hooks/useCanvasWheel'
import { useLayoutSave } from '../hooks/useLayoutSave'
import { InteractiveBedMenu } from './InteractiveBedMenu'
import { DrawingMenu } from '../components/DrawingMenu'
import { PanelSection, ToolTile } from '../components/EditorToolGroup'
import { CanvasToolbar } from '../components/CanvasToolbar'
import { Circle, Frame, PenTool, Pencil, Rainbow, RectangleHorizontal, Save, Square, SquareDashed, Sun, Trash2, X } from 'lucide-react'
import { TerrainPresets } from '../components/TerrainPresets'
import type { TerrainPreset } from '../components/TerrainPresets'
import { GardenSetupSteps } from '../components/GardenSetupSteps'
import type { GardenSetupStage } from '../components/GardenSetupSteps'
import type { MandalaConfig } from '../geometry'
import type { DemoElement, DemoGarden, ElementShape, Pan } from '../types'
import { readInteractiveDraft, writeInteractiveDraft } from '../layoutStorage'
import { createGardenProjection } from '../projection'
import type { InteractiveOrganization } from '../projection'
import { interactiveGardenApi } from '../services/interactiveGardenApi'

interface Viewport {
  width: number
  height: number
}

interface AlignmentGuides {
  x?: number
  y?: number
}

type Tool = 'sector-outline' | 'terrain' | 'select' | 'pan' | 'sector' | ElementShape
type EditorStage = GardenSetupStage
type NumericField = 'x' | 'y' | 'width' | 'height' | 'rotationDeg' | 'centerX' | 'centerY' | 'radius' | 'mandalaCenterX' | 'mandalaCenterY' | 'mandalaRadius' | 'mandalaAngle' | 'mandalaArcWidth' | 'mandalaRadialLength'
type EditableField = NumericField | 'name'

type Interaction =
  | { kind: 'outline-edge'; index: number; pointerX: number; pointerY: number; startPoints: { x: number; y: number }[] }
  | { kind: 'terrain-vertex'; index: number; pointerX: number; pointerY: number; closeOnClick: boolean }
  | { kind: 'pan'; pointerX: number; pointerY: number; startX: number; startY: number }
  | { kind: 'move'; elementId: string; elementIds: string[]; pointerX: number; pointerY: number; startElements: DemoElement[] }
  | { kind: 'rotate'; elementId: string; elementIds: string[]; centerX: number; centerY: number; pointerCenterX: number; pointerCenterY: number; startAngle: number; startRotation: number; startElements: DemoElement[] }
  | { kind: 'create'; shape: ElementShape; role: 'sector' | 'bed'; start: { x: number; y: number }; current: { x: number; y: number } }
  | { kind: 'marquee'; start: { x: number; y: number }; current: { x: number; y: number }; baseSelectedIds: string[] }

const EMPTY_VIEWPORT: Viewport = { width: 0, height: 0 }

function formatMeters(value: number) {
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(value)
}

function canvasElementLabel(element: DemoElement) {
  if (element.role === 'sector') return element.name
  if (element.shape === 'rectangle' && element.mandala) {
    const suffix = element.name.split('·').pop()?.trim()
    return suffix || element.name
  }
  return element.name
}

function parseNumber(value: string) {
  const normalized = value.trim().replace(',', '.')
  if (!normalized) return undefined
  const number = Number(normalized)
  return Number.isFinite(number) ? number : undefined
}

function angleBetween(point: { x: number; y: number }, center: { x: number; y: number }) {
  return Math.atan2(point.y - center.y, point.x - center.x) * 180 / Math.PI
}

function arcPath(centerX: number, centerY: number, radius: number, startAngle: number, endAngle: number) {
  const radians = (angle: number) => angle * Math.PI / 180
  const start = { x: centerX + radius * Math.cos(radians(startAngle)), y: centerY + radius * Math.sin(radians(startAngle)) }
  const end = { x: centerX + radius * Math.cos(radians(endAngle)), y: centerY + radius * Math.sin(radians(endAngle)) }
  const largeArcFlag = Math.abs(endAngle - startAngle) > 180 ? 1 : 0
  return `M ${start.x} ${start.y} A ${radius} ${radius} 0 ${largeArcFlag} 1 ${end.x} ${end.y}`
}

function mandalaPath(element: Extract<DemoElement, { shape: 'rectangle' }>, scale: number, centerX: number, centerY: number, styleOverride?: MandalaConfig['bedStyle']) {
  const placement = element.mandala
  if (!placement || (styleOverride ?? placement.bedStyle) !== 'curved') return ''
  const innerRadius = Math.max(0, placement.ringRadius - placement.radialLength / 2) * scale
  const outerRadius = (placement.ringRadius + placement.radialLength / 2) * scale
  const startAngle = placement.angleDeg - placement.angularWidthDeg / 2
  const endAngle = placement.angleDeg + placement.angularWidthDeg / 2
  const radians = (angle: number) => angle * Math.PI / 180
  const point = (radius: number, angle: number) => ({ x: centerX + radius * Math.cos(radians(angle)), y: centerY + radius * Math.sin(radians(angle)) })
  const outerStart = point(outerRadius, startAngle)
  const outerEnd = point(outerRadius, endAngle)
  const innerEnd = point(innerRadius, endAngle)
  const innerStart = point(innerRadius, startAngle)
  const largeArcFlag = placement.angularWidthDeg > 180 ? 1 : 0
  if (innerRadius <= 0.01) return `M ${outerStart.x} ${outerStart.y} A ${outerRadius} ${outerRadius} 0 ${largeArcFlag} 1 ${outerEnd.x} ${outerEnd.y} L ${centerX} ${centerY} Z`
  return `M ${outerStart.x} ${outerStart.y} A ${outerRadius} ${outerRadius} 0 ${largeArcFlag} 1 ${outerEnd.x} ${outerEnd.y} L ${innerEnd.x} ${innerEnd.y} A ${innerRadius} ${innerRadius} 0 ${largeArcFlag} 0 ${innerStart.x} ${innerStart.y} Z`
}

function mandalaLabelPoint(element: Extract<DemoElement, { shape: 'rectangle' }>, scale: number, centerX: number, centerY: number) {
  const placement = element.mandala
  if (!placement) return { x: centerX, y: centerY }
  const angle = placement.angleDeg * Math.PI / 180
  return { x: centerX + placement.ringRadius * scale * Math.cos(angle), y: centerY + placement.ringRadius * scale * Math.sin(angle) }
}

function fieldKey(id: string, field: EditableField) {
  return `${id}:${field}`
}

function elementField(element: DemoElement, field: NumericField) {
  if (element.shape === 'rectangle') {
    if (field === 'mandalaCenterX' && element.mandala) return element.mandala.centerX
    if (field === 'mandalaCenterY' && element.mandala) return element.mandala.centerY
    if (field === 'mandalaRadius' && element.mandala) return element.mandala.ringRadius
    if (field === 'mandalaAngle' && element.mandala) return element.mandala.angleDeg
    if (field === 'mandalaArcWidth' && element.mandala) return element.mandala.angularWidthDeg
    if (field === 'mandalaRadialLength' && element.mandala) return element.mandala.radialLength
    if (field === 'x') return element.x
    if (field === 'y') return element.y
    if (field === 'width') return element.width
    if (field === 'height') return element.height
    return element.rotationDeg
  }
  if (field === 'centerX') return element.centerX
  if (field === 'centerY') return element.centerY
  return element.radius
}

function mandalaLimits(element: Extract<DemoElement, { shape: 'rectangle' }>, garden: DemoGarden) {
  const placement = element.mandala
  if (!placement) return undefined
  const edgeDistance = Math.min(placement.centerX, garden.width - placement.centerX, placement.centerY, garden.height - placement.centerY)
  const boundaryRadius = placement.ringRadius + placement.radialLength / 2
  return {
    minCenterX: boundaryRadius,
    maxCenterX: Math.max(boundaryRadius, garden.width - boundaryRadius),
    minCenterY: boundaryRadius,
    maxCenterY: Math.max(boundaryRadius, garden.height - boundaryRadius),
    maxRingRadius: Math.max(0.4, edgeDistance - placement.radialLength / 2),
    maxRadialLength: Math.max(0.2, 2 * (edgeDistance - placement.ringRadius)),
  }
}

function elementFromPoints(shape: ElementShape, role: 'sector' | 'bed', start: { x: number; y: number }, current: { x: number; y: number }, garden: DemoGarden, id: string, name: string, sectorId?: string, sectorName?: string): DemoElement {
  if (shape === 'circle') {
    const distance = Math.hypot(current.x - start.x, current.y - start.y)
    return clampElement({ id, name, role, shape, centerX: start.x, centerY: start.y, radius: Math.max(0.2, distance || 1), sectorId, sectorName }, garden)
  }
  const width = Math.abs(current.x - start.x)
  const height = Math.abs(current.y - start.y)
  return clampElement({ id, name, role, shape, x: Math.min(start.x, current.x), y: Math.min(start.y, current.y), width: width || (role === 'sector' ? 0.2 : 1), height: height || (role === 'sector' ? 0.2 : 5), rotationDeg: 0, sectorId, sectorName }, garden)
}

function elementCenter(element: DemoElement) {
  return element.shape === 'rectangle'
    ? { x: element.x + element.width / 2, y: element.y + element.height / 2 }
    : { x: element.centerX, y: element.centerY }
}

function elementAnchor(element: DemoElement) {
  return element.shape === 'rectangle'
    ? { x: element.x, y: element.y }
    : { x: element.centerX, y: element.centerY }
}

function sectorContainsElement(sector: DemoElement, element: DemoElement) {
  if (sector.role !== 'sector' || sector.shape !== 'rectangle') return false
  return elementsOverlap(sector, element)
}

function sectorForElement(elements: DemoElement[], element: DemoElement) {
  if (element.role !== 'bed') return undefined
  const previous = elements.find((candidate) => candidate.role === 'sector' && candidate.id === element.sectorId && sectorContainsElement(candidate, element))
  if (previous) return previous
  return elements.find((candidate) => candidate.role === 'sector' && sectorContainsElement(candidate, element))
}

function normalizeVisualSectorAssignments(elements: DemoElement[]) {
  const sectors = elements.filter((element): element is Extract<DemoElement, { shape: 'rectangle' }> => element.role === 'sector' && element.shape === 'rectangle')
  if (sectors.length === 0) return elements
  return elements.map((element) => {
    if (element.role !== 'bed') return element
    const containingSector = sectorForElement(sectors, element)
    return containingSector
      ? { ...element, sectorId: containingSector.id, sectorName: containingSector.name }
      : { ...element, sectorId: undefined, sectorName: undefined }
  })
}

function layoutFitsSectors(elements: DemoElement[]) {
  return elements.every((element) => {
    if (element.role !== 'bed' || !element.sectorId) return true
    const sector = sectorForElement(elements, element)
    return Boolean(sector)
  })
}

function fitBedInSector(element: DemoElement, sector: Extract<DemoElement, { shape: 'rectangle' }>) {
  if (!sector.outline || boundaryContainsElement(sector.outline, element)) return element
  const bounds = elementBounds(element)
  const width = bounds.maxX - bounds.minX, height = bounds.maxY - bounds.minY
  for (let row = 0; row <= 24; row++) {
    for (let column = 0; column <= 24; column++) {
      const candidate = translateElement(element, sector.x + (sector.width - width) * column / 24 - bounds.minX, sector.y + (sector.height - height) * row / 24 - bounds.minY)
      if (boundaryContainsElement(sector.outline, candidate)) return candidate
    }
  }
  return undefined
}

function selectionBounds(elements: DemoElement[]) {
  const bounds = elements.map(elementBounds)
  return {
    minX: Math.min(...bounds.map((item) => item.minX)),
    minY: Math.min(...bounds.map((item) => item.minY)),
    maxX: Math.max(...bounds.map((item) => item.maxX)),
    maxY: Math.max(...bounds.map((item) => item.maxY)),
  }
}

function rotationCenterFor(elements: DemoElement[]) {
  const only = elements[0]
  if (elements.length === 1 && only?.shape === 'rectangle' && only.mandala?.bedStyle === 'curved') {
    return { x: only.mandala.centerX, y: only.mandala.centerY }
  }
  const bounds = selectionBounds(elements)
  return { x: (bounds.minX + bounds.maxX) / 2, y: (bounds.minY + bounds.maxY) / 2 }
}

function rotationVisualCenterFor(element: DemoElement, elements: DemoElement[]) {
  if (elements.length === 1 && element.shape === 'rectangle' && element.mandala?.bedStyle === 'curved') return elementCenter(element)
  const bounds = selectionBounds(elements)
  return { x: (bounds.minX + bounds.maxX) / 2, y: (bounds.minY + bounds.maxY) / 2 }
}

function translateElement(element: DemoElement, deltaX: number, deltaY: number): DemoElement {
  if (element.shape === 'rectangle') {
    return element.mandala
      ? { ...element, x: element.x + deltaX, y: element.y + deltaY, mandala: { ...element.mandala, centerX: element.mandala.centerX + deltaX, centerY: element.mandala.centerY + deltaY } }
      : { ...element, x: element.x + deltaX, y: element.y + deltaY, outline: element.outline?.map((point) => ({ x: point.x + deltaX, y: point.y + deltaY })) }
  }
  return { ...element, centerX: element.centerX + deltaX, centerY: element.centerY + deltaY }
}

function rotateElementAround(element: DemoElement, center: { x: number; y: number }, deltaDeg: number): DemoElement {
  const origin = elementCenter(element)
  const radians = deltaDeg * Math.PI / 180
  const rotatedCenter = {
    x: center.x + (origin.x - center.x) * Math.cos(radians) - (origin.y - center.y) * Math.sin(radians),
    y: center.y + (origin.x - center.x) * Math.sin(radians) + (origin.y - center.y) * Math.cos(radians),
  }
  if (element.shape === 'rectangle') {
    if (element.mandala?.bedStyle === 'curved') {
      const mandalaCenter = { x: element.mandala.centerX, y: element.mandala.centerY }
      const mandalaCenterIsFixed = Math.abs(center.x - mandalaCenter.x) < 1e-6 && Math.abs(center.y - mandalaCenter.y) < 1e-6
      const rotatedMandalaCenter = mandalaCenterIsFixed ? mandalaCenter : {
        x: center.x + (mandalaCenter.x - center.x) * Math.cos(radians) - (mandalaCenter.y - center.y) * Math.sin(radians),
        y: center.y + (mandalaCenter.x - center.x) * Math.sin(radians) + (mandalaCenter.y - center.y) * Math.cos(radians),
      }
      const mandala = { ...element.mandala, centerX: rotatedMandalaCenter.x, centerY: rotatedMandalaCenter.y, angleDeg: normalizeAngle(element.mandala.angleDeg + deltaDeg) }
      return {
        ...element,
        x: rotatedCenter.x - element.width / 2,
        y: rotatedCenter.y - element.height / 2,
        rotationDeg: normalizeAngle(mandala.angleDeg - 90),
        mandala,
      }
    }
    const mandala = element.mandala
      ? { ...element.mandala, centerX: rotatedCenter.x, centerY: rotatedCenter.y, angleDeg: normalizeAngle(element.mandala.angleDeg + deltaDeg) }
      : undefined
    return {
      ...element,
      x: rotatedCenter.x - element.width / 2,
      y: rotatedCenter.y - element.height / 2,
      rotationDeg: normalizeAngle(element.rotationDeg + deltaDeg),
      mandala,
    }
  }
  return { ...element, centerX: rotatedCenter.x, centerY: rotatedCenter.y }
}

export function InteractiveGardenPage({ route }: { route: RecordRoute }) {
  const workspace = useGardenState()
  const on = today()
  const load = useCallback((signal: AbortSignal) => interactiveGardenApi.get(route.garden, on, signal), [on, route.garden])
  const organization = useRemote(`${route.garden}/${on}`, load)
  const projection = useMemo(() => createGardenProjection(workspace, organization.data), [organization.data, workspace])
  if (organization.loading) return <section className="panel" aria-live="polite"><p role="status">Carregando a horta, setores e canteiros…</p></section>
  return <>
    {organization.error && <Failure error={organization.error} />}
    <InteractiveGardenEditor route={route} initial={projection.garden} projectionWarnings={projection.warnings} organization={organization.data} />
  </>
}

interface InteractiveGardenEditorProps {
  route: RecordRoute
  initial: DemoGarden
  projectionWarnings: string[]
  organization?: InteractiveOrganization
}

function InteractiveGardenEditor({ route, initial: projection, projectionWarnings, organization }: InteractiveGardenEditorProps) {
  const restored = useMemo(() => readInteractiveDraft(route.garden, projection), [projection, route.garden])
  // Dimensions frame the drawing surface; only a confirmed boundary defines the terrain.
  const [extent, setExtent] = useState({ width: restored.width, height: restored.height })
  const initial = useMemo(() => ({ ...projection, ...extent }), [projection, extent])
  const saving = useLayoutSave(route.garden, organization?.savedLayout)
  const [confirmedSignature, setConfirmedSignature] = useState(() => JSON.stringify({ boundary: projection.boundary ?? null, elements: projection.elements }))
  const [draftBaseKey, setDraftBaseKey] = useState(projection.projectionKey)
  const [resetBaseline, setResetBaseline] = useState(restored)
  const startingElements = useMemo(() => normalizeVisualSectorAssignments(resetBaseline.elements), [resetBaseline])
  const startingBoundary = useMemo(() => resetBaseline.boundary, [resetBaseline])
  const [boundary, setBoundary] = useState(startingBoundary)
  const [editingSectorId, setEditingSectorId] = useState<string>()
  const [draftClosed, setDraftClosed] = useState(Boolean(startingBoundary))
  const [boundaryDraft, setBoundaryDraft] = useState<{ x: number; y: number }[]>([])
  const [elements, setElements] = useState<DemoElement[]>(() => cloneElements(startingElements))
  const [history, setHistory] = useState<DemoElement[][]>(() => [cloneElements(startingElements)])
  const [historyIndex, setHistoryIndex] = useState(0)
  const [selectedId, setSelectedId] = useState(startingElements[0]?.id ?? '')
  const [selectedIds, setSelectedIds] = useState<string[]>(startingElements[0] ? [startingElements[0].id] : [])
  const [hoveredId, setHoveredId] = useState<string>()
  const [tool, setTool] = useState<Tool>(() => !startingBoundary ? 'terrain' : startingElements.some((element) => element.role === 'sector') ? 'select' : 'sector')
  const [editorStage, setEditorStage] = useState<EditorStage>(() => !startingBoundary ? 'terrain' : startingElements.some((element) => element.role === 'sector') ? 'beds' : 'sectors')
  const [workflowNotice, setWorkflowNotice] = useState<string>()
  const [viewport, setViewport] = useState<Viewport>(EMPTY_VIEWPORT)
  const [zoom, setZoom] = useState(1)
  const [pan, setPan] = useState<Pan>({ x: 0, y: 0 })
  const [showGrid, setShowGrid] = useState(true)
  const [snapEnabled, setSnapEnabled] = useState(true)
  const [rotationSnapEnabled, setRotationSnapEnabled] = useState(true)
  const [alignmentEnabled, setAlignmentEnabled] = useState(true)
  const [alignmentGuides, setAlignmentGuides] = useState<AlignmentGuides>({})
  const [mandalaOpen, setMandalaOpen] = useState(false)
  const [propertiesOpen, setPropertiesOpen] = useState(false)
  const [bedMenuId, setBedMenuId] = useState<string>()
  const [bedFormOpen, setBedFormOpen] = useState(false)
  const [canvasPosition, setCanvasPosition] = useState({ left: 0, top: 0 })
  const [mandalaConfig, setMandalaConfig] = useState<MandalaConfig>(DEFAULT_MANDALA_CONFIG)
  const [mandalaShowValidation, setMandalaShowValidation] = useState(false)
  const [fieldDraft, setFieldDraft] = useState<Record<string, string>>({})
  const [interaction, setInteraction] = useState<Interaction>()
  const [spacePressed, setSpacePressed] = useState(false)
  const canvasRef = useRef<HTMLDivElement>(null)
  const svgRef = useRef<SVGSVGElement>(null)
  const propertiesCloseRef = useRef<HTMLButtonElement>(null)
  const elementsRef = useRef(elements)
  const nextId = useRef(2)

  useEffect(() => {
    elementsRef.current = elements
  }, [elements])

  // Os atalhos leem a ferramenta/etapa atuais sem recriar o listener a cada mudança.
  const toolRef = useRef(tool)
  const stageRef = useRef({ canEditSectors: false, canPlaceBeds: false })

  useEffect(() => {
    writeInteractiveDraft(route.garden, { ...initial, projectionKey: draftBaseKey, boundary, elements: cloneElements(elements) })
  }, [boundary, draftBaseKey, elements, initial, route.garden])

  const geometryDirty = !sameElements(elements, startingElements)
  const draftDirty = Object.entries(fieldDraft).some(([key, value]) => {
    const [id, field] = key.split(':') as [string, EditableField]
    const element = elements.find((item) => item.id === id)
    return element !== undefined && (field === 'name' ? element.name !== value : String(elementField(element, field)) !== value)
  })
  const dirty = geometryDirty || draftDirty || JSON.stringify(boundary) !== JSON.stringify(startingBoundary) || boundaryDraft.length > 0
  const unsaved = JSON.stringify({ boundary: boundary ?? null, elements }) !== confirmedSignature
  const selected = elements.find((element) => element.id === selectedId)
  const bedMenuElement = elements.find(element => element.id === bedMenuId)
  const sectorElements = elements.filter((element): element is Extract<DemoElement, { shape: 'rectangle' }> => element.role === 'sector' && element.shape === 'rectangle')
  const selectedSector = selected?.role === 'sector' && selected.shape === 'rectangle'
    ? selected
    : selected?.sectorId ? sectorElements.find((sector) => sector.id === selected.sectorId)
      : undefined
  const drawingOutline = tool === 'terrain' || tool === 'sector-outline'
  const canEditSectors = Boolean(boundary) && !drawingOutline
  const canPlaceBeds = canEditSectors && sectorElements.length > 0
  useEffect(() => { toolRef.current = tool; stageRef.current = { canEditSectors, canPlaceBeds } }, [tool, canEditSectors, canPlaceBeds])
  // A área do setor envolve os canteiros por definição; ela não deve ser
  // tratada como uma forma sobreposta ao calcular conflitos visuais.
  const conflicts = conflictIds(elements.filter((element) => element.role !== 'sector'))
  const mandalaError = validateMandalaConfig(mandalaConfig, initial)
  const mandalaPreview = useMemo(() => generateMandalaElements(mandalaConfig, initial), [initial, mandalaConfig])
  const selectedMandalaLimits = selected?.shape === 'rectangle' && selected.mandala ? mandalaLimits(selected, initial) : undefined
  const propertiesPosition = (() => {
    if (!selected || viewport.width === 0) return { left: 16, top: 16 }
    const bounds = elementBounds(selected)
    const topLeft = worldToScreen({ x: bounds.minX, y: bounds.minY }, viewport, initial, zoom, pan)
    const bottomRight = worldToScreen({ x: bounds.maxX, y: bounds.maxY }, viewport, initial, zoom, pan)
    const panelWidth = 320
    const panelHeight = Math.min(560, Math.max(280, window.innerHeight - 32))
    const preferredLeft = bottomRight.x + 12
    const fallbackLeft = topLeft.x - panelWidth - 12
    const leftInCanvas = preferredLeft + panelWidth <= viewport.width - 12 ? preferredLeft : fallbackLeft
    const topInCanvas = Math.max(12, Math.min(topLeft.y, viewport.height - panelHeight - 12))
    return {
      left: Math.max(12, Math.min(window.innerWidth - panelWidth - 12, canvasPosition.left + Math.max(12, Math.min(leftInCanvas, Math.max(12, viewport.width - panelWidth - 12))))),
      top: Math.max(12, Math.min(window.innerHeight - panelHeight - 12, canvasPosition.top + topInCanvas)),
    }
  })()

  useEffect(() => {
    const element = canvasRef.current
    if (!element) return
    const updateCanvasPosition = () => {
      const rect = element.getBoundingClientRect()
      setCanvasPosition({ left: rect.left, top: rect.top })
    }
    const observer = new ResizeObserver((entries) => {
      const entry = entries[0]
      if (!entry) return
      setViewport({ width: entry.contentRect.width, height: Math.max(420, entry.contentRect.height) })
      updateCanvasPosition()
    })
    observer.observe(element)
    updateCanvasPosition()
    window.addEventListener('scroll', updateCanvasPosition, true)
    return () => {
      observer.disconnect()
      window.removeEventListener('scroll', updateCanvasPosition, true)
    }
  }, [])

  useEffect(() => {
    if (!dirty && !unsaved) return
    return registerNavigationGuard(() => window.confirm('Há alterações não salvas no mapa. A visão panorâmica mostra somente a configuração salva. Deseja sair mesmo assim?'))
  }, [dirty, unsaved, bedMenuId])

  useEffect(() => {
    if (!mandalaOpen) return
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMandalaOpen(false)
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [mandalaOpen])

  useEffect(() => {
    if (!propertiesOpen) return
    propertiesCloseRef.current?.focus()
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setPropertiesOpen(false)
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [propertiesOpen])

  const applyLiveElements = useCallback((next: DemoElement[]) => {
    if (boundary && next.some((element) => !boundaryContainsElement(boundary, element))) {
      setWorkflowNotice('Mantenha os objetos dentro do contorno do terreno.'); return
    }
    const snapshot = cloneElements(normalizeVisualSectorAssignments(next))
    elementsRef.current = snapshot
    setElements(snapshot)
  }, [boundary])

  const pushHistory = useCallback((next: DemoElement[]) => {
    if (boundary && next.some((element) => !boundaryContainsElement(boundary, element))) {
      setWorkflowNotice('Mantenha os objetos dentro do contorno do terreno.')
      return
    }
    const snapshot = cloneElements(normalizeVisualSectorAssignments(next))
    if (sameElements(snapshot, history[historyIndex] ?? [])) return
    setHistory((current) => [...current.slice(0, historyIndex + 1), snapshot])
    setHistoryIndex(historyIndex + 1)
    elementsRef.current = snapshot
    setElements(snapshot)
  }, [boundary, history, historyIndex])

  const commitCurrentHistory = useCallback(() => pushHistory(elementsRef.current), [pushHistory])

  const applyHistorySnapshot = useCallback((snapshot: DemoElement[], index: number) => {
    const next = cloneElements(snapshot)
    elementsRef.current = next
    setElements(next)
    setHistoryIndex(index)
    setFieldDraft({})
    setSelectedId((current) => next.some((element) => element.id === current) ? current : next[0]?.id ?? '')
    setSelectedIds((current) => {
      const retained = current.filter((id) => next.some((element) => element.id === id))
      return retained.length > 0 ? retained : next[0] ? [next[0].id] : []
    })
  }, [])

  const undo = useCallback(() => {
    if (historyIndex === 0) return
    applyHistorySnapshot(history[historyIndex - 1], historyIndex - 1)
  }, [applyHistorySnapshot, history, historyIndex])

  const redo = useCallback(() => {
    if (historyIndex >= history.length - 1) return
    applyHistorySnapshot(history[historyIndex + 1], historyIndex + 1)
  }, [applyHistorySnapshot, history, historyIndex])

  const reset = useCallback(() => {
    setExtent({ width: resetBaseline.width, height: resetBaseline.height })
    setBoundary(startingBoundary)
    setBoundaryDraft([])
    const snapshot = cloneElements(startingElements)
    elementsRef.current = snapshot
    setElements(snapshot)
    setHistory([snapshot])
    setHistoryIndex(0)
    setFieldDraft({})
    setSelectedId(snapshot[0]?.id ?? '')
    setSelectedIds(snapshot[0] ? [snapshot[0].id] : [])
    setTool(!startingBoundary ? 'terrain' : snapshot.some((element) => element.role === 'sector') ? 'select' : 'sector')
    setEditorStage(!startingBoundary ? 'terrain' : snapshot.some((element) => element.role === 'sector') ? 'beds' : 'sectors')
    setWorkflowNotice(undefined)
    setZoom(1)
    setPan({ x: 0, y: 0 })
  }, [resetBaseline, startingBoundary, startingElements])

  const removeSelected = useCallback((targetId?: string) => {
    const currentSelected = elementsRef.current.find((element) => element.id === (targetId ?? selectedId))
    if (!currentSelected) return
    const idsToRemove = targetId ? [targetId] : selectedIds.length > 0 ? selectedIds : [currentSelected.id]
    const message = idsToRemove.length === 1 ? `Remover “${currentSelected.name}” do rascunho demonstrativo?` : `Remover os ${idsToRemove.length} objetos selecionados do rascunho demonstrativo?`
    if (!window.confirm(message)) return
    const removedSectorIds = new Set(elementsRef.current.filter((element) => idsToRemove.includes(element.id) && element.role === 'sector').map((element) => element.id))
    const next = elementsRef.current
      .filter((element) => !idsToRemove.includes(element.id))
      .map((element) => removedSectorIds.has(element.sectorId ?? '') ? { ...element, sectorId: undefined, sectorName: undefined } : element)
    pushHistory(next)
    setFieldDraft({})
    setPropertiesOpen(false)
    setSelectedId(next[0]?.id ?? '')
    setSelectedIds(next[0] ? [next[0].id] : [])
    if (boundary && !next.some((element) => element.role === 'sector')) {
      setEditorStage('sectors')
      setTool('sector')
    }
  }, [boundary, pushHistory, selectedId, selectedIds])

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null
      const editing = target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement
      const modifier = event.ctrlKey || event.metaKey
      if (mandalaOpen || propertiesOpen || bedMenuId || saving.busy || saving.uncertain) return
      if (event.code === 'Space' && !editing) {
        event.preventDefault()
        setSpacePressed(true)
      } else if (modifier && event.key.toLowerCase() === 'z') {
        event.preventDefault()
        if (event.shiftKey) redo()
        else undo()
      } else if (modifier && event.key.toLowerCase() === 'y') {
        event.preventDefault()
        redo()
      } else if (event.key === 'Delete' && !editing) {
        event.preventDefault()
        removeSelected()
      } else if (!editing && !modifier && !event.altKey) {
        // Atalhos de ferramenta: só trocam o modo do cursor; nenhum cria ou remove objetos.
        const key = event.key.toLowerCase()
        const outlining = toolRef.current === 'terrain' || toolRef.current === 'sector-outline'
        if (key === 'escape' && !outlining && toolRef.current !== 'select') { event.preventDefault(); setTool('select') }
        else if (key === 'v' && !outlining) { event.preventDefault(); setTool('select') }
        else if (key === 'h' && !outlining) { event.preventDefault(); setTool('pan') }
        else if (key === 's' && !outlining && stageRef.current.canEditSectors) { event.preventDefault(); setEditorStage('sectors'); setTool('sector') }
        else if ((key === 'r' || key === 'c') && !outlining && stageRef.current.canPlaceBeds) { event.preventDefault(); setEditorStage('beds'); setTool(key === 'r' ? 'rectangle' : 'circle') }
        else if (key === 'f') { event.preventDefault(); setZoom(1); setPan({ x: 0, y: 0 }) }
        else if (key === '+' || key === '=') { event.preventDefault(); setZoom((current) => clampZoom(current + 0.15)) }
        else if (key === '-') { event.preventDefault(); setZoom((current) => clampZoom(current - 0.15)) }
      }
    }
    window.addEventListener('keydown', onKeyDown)
    const onKeyUp = (event: KeyboardEvent) => {
      if (event.code === 'Space') setSpacePressed(false)
    }
    window.addEventListener('keyup', onKeyUp)
    return () => {
      window.removeEventListener('keydown', onKeyDown)
      window.removeEventListener('keyup', onKeyUp)
    }
  }, [mandalaOpen, propertiesOpen, bedMenuId, redo, removeSelected, undo, saving.busy, saving.uncertain])

  const scale = viewport.width > 0 ? baseScale(viewport.width, viewport.height, initial) * zoom : 0
  const terrain = viewport.width > 0 ? worldToScreen({ x: 0, y: 0 }, viewport, initial, zoom, pan) : { x: 0, y: 0 }

  const fit = useCallback(() => {
    setZoom(1)
    setPan({ x: 0, y: 0 })
  }, [])

  const zoomAt = useCallback((nextZoom: number, pointer?: { x: number; y: number }) => {
    const next = clampZoom(nextZoom)
    if (!pointer || viewport.width === 0 || next === zoom) {
      setZoom(next)
      return
    }
    const before = screenToWorld(pointer, viewport, initial, zoom, pan)
    const afterOrigin = worldToScreen(before, viewport, initial, next, { x: 0, y: 0 })
    setPan({ x: pointer.x - afterOrigin.x, y: pointer.y - afterOrigin.y })
    setZoom(next)
  }, [initial, pan, viewport, zoom])

  const addElement = (shape: ElementShape) => {
    if (!canPlaceBeds) {
      setEditorStage('sectors')
      setTool('sector')
      setWorkflowNotice('Defina pelo menos um setor dentro da área da horta antes de adicionar canteiros.')
      return
    }
    const sector = selectedSector ?? sectorElements[0]
    const sectorWidth = sector.width
    const sectorHeight = sector.height
    const count = elementsRef.current.length + 1
    const id = `demo-${shape}-${crypto.randomUUID()}`
    nextId.current += 1
    const requested = shape === 'circle'
      ? clampElement({ id, name: `Círculo ${count}`, role: 'bed', shape, centerX: sector.x + sectorWidth / 2, centerY: sector.y + sectorHeight / 2, radius: Math.max(0.2, Math.min(1.25, sectorWidth / 4, sectorHeight / 4)), sectorId: sector.id, sectorName: sector.name }, initial)
      : clampElement({ id, name: `Retângulo ${count}`, role: 'bed', shape, x: sector.x + Math.max(0, (sectorWidth - Math.min(2, sectorWidth)) / 2), y: sector.y + Math.max(0, (sectorHeight - Math.min(1, sectorHeight)) / 2), width: Math.min(2, sectorWidth), height: Math.min(1, sectorHeight), rotationDeg: 0, sectorId: sector.id, sectorName: sector.name }, initial)
    const element = fitBedInSector(requested, sector)
    if (!element) { setWorkflowNotice('Escolha um canteiro menor ou desenhe uma forma que caiba neste setor.'); return }
    const next = [...elementsRef.current, element]
    pushHistory(next)
    setSelectedId(element.id)
    setSelectedIds([element.id])
    setTool('select')
  }

  const addCurvedElement = () => {
    if (!canPlaceBeds) {
      setEditorStage('sectors')
      setTool('sector')
      setWorkflowNotice('Defina pelo menos um setor dentro da área da horta antes de adicionar canteiros.')
      return
    }
    const sector = selectedSector ?? sectorElements[0]
    const count = elementsRef.current.length + 1
    const id = `demo-curved-${crypto.randomUUID()}`
    nextId.current += 1
    const centerX = sector.x + sector.width / 2
    const centerY = sector.y + sector.height / 2
    const ringRadius = Math.min(sector.width, sector.height) * 0.25
    const radialLength = Math.min(1.4, Math.max(0.8, ringRadius * 0.24))
    const placement = {
      bedStyle: 'curved' as const,
      centerX,
      centerY,
      ringRadius,
      angleDeg: 0,
      angularWidthDeg: 72,
      radialLength,
    }
    const requested = updateMandalaPlacement({
      id,
      name: `Canteiro curvilíneo ${count}`,
      role: 'bed',
      sectorId: sector.id,
      sectorName: sector.name,
      shape: 'rectangle',
      x: 0,
      y: 0,
      width: 1,
      height: radialLength,
      rotationDeg: 0,
    }, placement, initial)
    const element = fitBedInSector(requested, sector)
    if (!element) { setWorkflowNotice('O canteiro curvilíneo não cabe neste setor. Desenhe uma forma menor.'); return }
    pushHistory([...elementsRef.current, element])
    setSelectedId(element.id)
    setSelectedIds([element.id])
    setTool('select')
  }

  const openMandala = () => {
    if (!canPlaceBeds) {
      setEditorStage('sectors')
      setTool('sector')
      setWorkflowNotice('Defina pelo menos um setor dentro da área da horta antes de gerar uma mandala.')
      return
    }
    const sector = selectedSector ?? sectorElements[0]
    const radius = Math.max(0.8, Math.min(sector.width, sector.height) * 0.45)
    setMandalaConfig(DEFAULT_MANDALA_CONFIG)
    setMandalaConfig((current) => ({ ...current, centerX: sector.x + sector.width / 2, centerY: sector.y + sector.height / 2, radius, innerRadius: Math.min(current.innerRadius, radius * 0.35) }))
    setMandalaShowValidation(false)
    setMandalaOpen(true)
  }

  const updateMandalaNumber = (field: keyof MandalaConfig, value: string) => {
    const parsed = Number(value)
    setMandalaShowValidation(false)
    setMandalaConfig((current) => ({ ...current, [field]: Number.isFinite(parsed) ? parsed : 0 }))
  }

  const updateMandalaStyle = (bedStyle: MandalaConfig['bedStyle']) => {
    setMandalaShowValidation(false)
    setMandalaConfig((current) => bedStyle === 'curved'
      ? { ...current, bedStyle, divisions: 3, rings: 1 }
      : { ...current, bedStyle })
  }

  const applyMandala = () => {
    if (mandalaError || mandalaPreview.length === 0) return
    const sector = selectedSector ?? sectorElements[0]
    if (!sector) return
    const instanceId = crypto.randomUUID()
    const scopedPreview = normalizeVisualSectorAssignments([...elementsRef.current, ...mandalaPreview.map((element) => element.role === 'bed'
      ? { ...element, id: `${instanceId}-${element.id}`, sectorId: sector.id, sectorName: sector.name }
      : { ...element, id: `${instanceId}-${element.id}`, sectorId: undefined, sectorName: undefined })]).slice(elementsRef.current.length)
    if (!scopedPreview.every((element) => element.role !== 'bed' || Boolean(element.sectorId))) {
      setWorkflowNotice('Cada canteiro da mandala precisa sobrepor um setor delimitado. Ajuste a posição ou o raio.')
      return
    }
    const next = [...elementsRef.current, ...scopedPreview]
    pushHistory(next)
    setFieldDraft({})
    setSelectedId(scopedPreview[0]?.id ?? '')
    setSelectedIds(scopedPreview.map((element) => element.id))
    setTool('select')
    setMandalaOpen(false)
  }

  const chooseTerrainPreset = (preset: TerrainPreset) => {
    const left = initial.width * 0.1, top = initial.height * 0.1
    const width = initial.width * 0.8, height = initial.height * (preset === 'rectangle' ? 0.55 : 0.8)
    const side = Math.min(width, height)
    const points = preset === 'circle' ? Array.from({ length: 24 }, (_, index) => ({ x: left + width / 2 + Math.cos(index * Math.PI / 12) * side / 2, y: top + height / 2 + Math.sin(index * Math.PI / 12) * side / 2 }))
      : preset === 'l-shape' ? [{ x: left, y: top }, { x: left + width / 2, y: top }, { x: left + width / 2, y: top + height / 2 }, { x: left + width, y: top + height / 2 }, { x: left + width, y: top + height }, { x: left, y: top + height }]
      : [{ x: left, y: top }, { x: left + (preset === 'square' ? side : width), y: top }, { x: left + (preset === 'square' ? side : width), y: top + (preset === 'square' ? side : height) }, { x: left, y: top + (preset === 'square' ? side : height) }]
    setBoundaryDraft(points); setDraftClosed(true); setFieldDraft({}); setTool('terrain'); setEditorStage('terrain')
    setWorkflowNotice('Formato pronto para editar. Arraste os nós; clique esquerdo adiciona e clique direito remove. Conclua para aplicar.')
  }

  const removeTerrainNode = (index: number) => {
    setBoundaryDraft((points) => points.filter((_, i) => i !== index)); setFieldDraft({}); setInteraction(undefined)
  }

  const beginSectorOutline = (sector?: Extract<DemoElement, { shape: 'rectangle' }>, useTerrain = false) => {
    if (!boundary) return
    setEditingSectorId(sector?.id)
    setBoundaryDraft((sector?.outline ?? (useTerrain ? boundary : [])).map((point) => ({ ...point })))
    setDraftClosed(Boolean(sector?.outline || useTerrain)); setTool('sector-outline'); setEditorStage('sectors'); setPropertiesOpen(false)
    setWorkflowNotice('Desenhe o setor por nós dentro do terreno. Clique direito remove nós; clique no primeiro para concluir.')
  }

  const finishSectorOutline = () => {
    if (!boundary || !validBoundary(boundaryDraft)) { setWorkflowNotice('O setor precisa formar uma área sem cruzamentos, com pelo menos três nós.'); return }
    const existing = elementsRef.current.find((element) => element.id === editingSectorId)
    const minX = Math.min(...boundaryDraft.map((point) => point.x)), minY = Math.min(...boundaryDraft.map((point) => point.y))
    const sector: Extract<DemoElement, { shape: 'rectangle' }> = {
      id: existing?.id ?? crypto.randomUUID(), name: existing?.name ?? 'Setor ' + (sectorElements.length + 1), role: 'sector', shape: 'rectangle',
      x: minX, y: minY, width: Math.max(...boundaryDraft.map((point) => point.x)) - minX, height: Math.max(...boundaryDraft.map((point) => point.y)) - minY, rotationDeg: 0,
      outline: boundaryDraft.map((point) => ({ ...point })),
    }
    if (!boundaryContainsElement(boundary, sector)) { setWorkflowNotice('Todos os limites do setor precisam ficar dentro do terreno.'); return }
    const next = existing ? elementsRef.current.map((element) => element.id === existing.id ? sector : element) : [...elementsRef.current, sector]
    if (!layoutFitsSectors(next)) { setWorkflowNotice('A edição precisa manter os canteiros associados a um setor.'); return }
    pushHistory(next); setBoundaryDraft([]); setEditingSectorId(undefined); setTool('select'); setEditorStage('beds'); setSelectedId(sector.id); setSelectedIds([sector.id]); setWorkflowNotice('Setor delimitado conforme o contorno livre. Agora adicione os canteiros.')
  }

  const terrainHasSectors = sectorElements.length > 0
  const removeTerrain = () => {
    if (!boundary) return
    if (elementsRef.current.some((element) => element.role === 'sector')) {
      setWorkflowNotice('Remova todos os setores antes de remover o terreno.'); return
    }
    const snapshot = cloneElements(elementsRef.current)
    const emptyTerrain = { ...initial, boundary: undefined, elements: snapshot }
    writeInteractiveDraft(route.garden, emptyTerrain)
    setResetBaseline(emptyTerrain); setBoundary(undefined); setBoundaryDraft([]); setDraftClosed(false); setEditingSectorId(undefined)
    setHistory([snapshot]); setHistoryIndex(0); setFieldDraft({}); setSelectedId(''); setSelectedIds([]); setPropertiesOpen(false)
    setEditorStage('terrain'); setTool('terrain'); setZoom(1); setPan({ x: 0, y: 0 })
    setWorkflowNotice('Terreno removido. Escolha uma base ou desenhe um novo contorno.')
  }

  const finishTerrain = () => {
    if (!validBoundary(boundaryDraft)) { setWorkflowNotice('Marque pelo menos três vértices, formando uma área sem cruzamentos.'); return }
    if (elements.some((element) => !boundaryContainsElement(boundaryDraft, element))) { setWorkflowNotice('O contorno precisa envolver os objetos existentes.'); return }
    setBoundary(boundaryDraft)
    setExtent({ width: Math.max(...boundaryDraft.map((point) => point.x)), height: Math.max(...boundaryDraft.map((point) => point.y)) })
    setDraftClosed(false); setBoundaryDraft([]); setFieldDraft({}); setEditorStage('sectors'); setZoom(1); setPan({ x: 0, y: 0 }); setTool('sector')
    setWorkflowNotice('Terreno fechado. Agora delimite os setores no mapa.')
  }

  const onWheel = (event: WheelEvent, canvas: SVGSVGElement) => {
    const rect = canvas.getBoundingClientRect()
    zoomAt(zoom * wheelZoomFactor(event), { x: event.clientX - rect.left, y: event.clientY - rect.top })
  }
  const wheelRef = useCanvasWheel(onWheel)
  const setSvgRef = useCallback((canvas: SVGSVGElement | null) => { svgRef.current = canvas; wheelRef(canvas) }, [wheelRef])

  const removeNodeAtPointer = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (event.button !== 2 || !drawingOutline) return
    event.preventDefault(); event.stopPropagation()
    const rect = event.currentTarget.getBoundingClientRect()
    const pointer = { x: event.clientX - rect.left, y: event.clientY - rect.top }
    let nearest = -1, distance = 22
    boundaryDraft.forEach((point, index) => {
      const screen = worldToScreen(point, viewport, initial, zoom, pan)
      const current = Math.hypot(pointer.x - screen.x, pointer.y - screen.y)
      if (current < distance) { nearest = index; distance = current }
    })
    if (nearest >= 0) removeTerrainNode(nearest)
    else setWorkflowNotice('Clique com o botão direito sobre o nó que deseja remover.')
  }

  const onPointerDown = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (event.button !== 0 && event.button !== 1) return
    setAlignmentGuides({})
    const panGesture = spacePressed || event.altKey || event.button === 1
    if (panGesture) event.preventDefault()
    event.currentTarget.setPointerCapture(event.pointerId)
    const rect = event.currentTarget.getBoundingClientRect()
    const point = screenToWorld({ x: event.clientX - rect.left, y: event.clientY - rect.top }, viewport, initial, zoom, pan)
    if (panGesture) {
      setInteraction({ kind: 'pan', pointerX: event.clientX, pointerY: event.clientY, startX: pan.x, startY: pan.y })
      return
    }
    if (drawingOutline) {
      const bounded = { x: Math.max(0, point.x), y: Math.max(0, point.y) }
      const node = snapEnabled ? { x: snap(bounded.x), y: snap(bounded.y) } : bounded
      setBoundaryDraft((points) => {
        if (!draftClosed || points.length < 3) return [...points, node]
        let nearest = 0, distance = Infinity
        points.forEach((start, index) => {
          const end = points[(index + 1) % points.length], dx = end.x - start.x, dy = end.y - start.y
          const t = Math.max(0, Math.min(1, ((node.x - start.x) * dx + (node.y - start.y) * dy) / (dx * dx + dy * dy || 1)))
          const current = Math.hypot(node.x - start.x - t * dx, node.y - start.y - t * dy)
          if (current < distance) { distance = current; nearest = index }
        })
        return [...points.slice(0, nearest + 1), node, ...points.slice(nearest + 1)]
      })
      return
    }
    if (tool === 'sector' || tool === 'rectangle' || tool === 'circle') {
      if (!boundary) { setTool('terrain'); setEditorStage('terrain'); setWorkflowNotice('Delimite e conclua o terreno antes de desenhar setores.'); return }
      if (tool !== 'sector' && !canPlaceBeds) {
        setEditorStage('sectors')
        setTool('sector')
        setWorkflowNotice('Defina pelo menos um setor dentro da área da horta antes de desenhar canteiros.')
        return
      }
      const start = snapEnabled ? { x: snap(point.x), y: snap(point.y) } : point
      setInteraction({ kind: 'create', shape: tool === 'sector' ? 'rectangle' : tool, role: tool === 'sector' ? 'sector' : 'bed', start, current: start })
      return
    }
    if (tool === 'select') {
      const additive = event.shiftKey || event.ctrlKey || event.metaKey
      if (!additive) {
        setSelectedId('')
        setSelectedIds([])
      }
      setInteraction({ kind: 'marquee', start: point, current: point, baseSelectedIds: additive ? selectedIds : [] })
      return
    }
    setInteraction({ kind: 'pan', pointerX: event.clientX, pointerY: event.clientY, startX: pan.x, startY: pan.y })
  }

  const onElementPointerDown = (event: ReactPointerEvent<SVGElement>, id: string) => {
    if (event.button !== 0 && event.button !== 1) return
    setAlignmentGuides({})
    if (spacePressed || event.altKey || event.button === 1) {
      event.stopPropagation()
      event.preventDefault()
      event.currentTarget.ownerSVGElement?.setPointerCapture(event.pointerId)
      setInteraction({ kind: 'pan', pointerX: event.clientX, pointerY: event.clientY, startX: pan.x, startY: pan.y })
      return
    }
    // As ferramentas de desenho precisam receber o arraste mesmo quando ele
    // começa sobre um canteiro já existente.
    if (tool !== 'select') return
    event.stopPropagation()
    const additive = event.shiftKey || event.ctrlKey || event.metaKey
    const nextSelectedIds = additive
      ? selectedIds.includes(id) ? selectedIds.filter((selected) => selected !== id) : [...selectedIds, id]
      : selectedIds.includes(id) ? selectedIds : [id]
    setSelectedId(nextSelectedIds.includes(id) ? id : nextSelectedIds[0] ?? '')
    setSelectedIds(nextSelectedIds)
    if (!nextSelectedIds.includes(id)) return
    event.currentTarget.ownerSVGElement?.setPointerCapture(event.pointerId)
    setInteraction({ kind: 'move', elementId: id, elementIds: nextSelectedIds, pointerX: event.clientX, pointerY: event.clientY, startElements: cloneElements(elements) })
  }

  const onRotationPointerDown = (event: ReactPointerEvent<SVGElement>, element: DemoElement, rotationElements: DemoElement[]) => {
    if (event.button !== 0 && event.button !== 1) return
    if (element.role !== 'bed' || element.shape !== 'rectangle' || tool !== 'select' || rotationElements.length === 0) return
    event.stopPropagation()
    if (spacePressed || event.altKey || event.button === 1) {
      event.preventDefault()
      event.currentTarget.ownerSVGElement?.setPointerCapture(event.pointerId)
      setInteraction({ kind: 'pan', pointerX: event.clientX, pointerY: event.clientY, startX: pan.x, startY: pan.y })
      return
    }
    setSelectedId(element.id)
    const svg = event.currentTarget.ownerSVGElement
    const rect = svg?.getBoundingClientRect()
    if (!svg || !rect) return
    svg.setPointerCapture(event.pointerId)
    const pointer = screenToWorld({ x: event.clientX - rect.left, y: event.clientY - rect.top }, viewport, initial, zoom, pan)
    const center = rotationCenterFor(rotationElements)
    const pointerCenter = rotationVisualCenterFor(element, rotationElements)
    setInteraction({ kind: 'rotate', elementId: element.id, elementIds: rotationElements.map((item) => item.id), centerX: center.x, centerY: center.y, pointerCenterX: pointerCenter.x, pointerCenterY: pointerCenter.y, startAngle: angleBetween(pointer, pointerCenter), startRotation: element.rotationDeg, startElements: cloneElements(elementsRef.current) })
  }

  const onPointerMove = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (!interaction || !scale) return
    if (interaction.kind === 'pan') {
      event.preventDefault()
      setPan({ x: interaction.startX + event.clientX - interaction.pointerX, y: interaction.startY + event.clientY - interaction.pointerY })
      return
    }
    const rect = event.currentTarget.getBoundingClientRect()
    const pointer = screenToWorld({ x: event.clientX - rect.left, y: event.clientY - rect.top }, viewport, initial, zoom, pan)
    if (interaction.kind === 'outline-edge') {
      if (Math.hypot(event.clientX - interaction.pointerX, event.clientY - interaction.pointerY) < 4) return
      const points = interaction.startPoints, start = points[interaction.index], end = points[(interaction.index + 1) % points.length]
      const length = Math.hypot(end.x - start.x, end.y - start.y)
      if (length < 1e-9) return
      const normal = { x: -(end.y - start.y) / length, y: (end.x - start.x) / length }
      const raw = ((event.clientX - interaction.pointerX) * normal.x + (event.clientY - interaction.pointerY) * normal.y) / scale
      const distance = snapEnabled ? snap(raw) : raw
      const delta = { x: normal.x * distance, y: normal.y * distance }
      if (Math.min(start.x, end.x) + delta.x < 0 || Math.min(start.y, end.y) + delta.y < 0) return
      setBoundaryDraft(points.map((point, index) => index === interaction.index || index === (interaction.index + 1) % points.length ? { x: point.x + delta.x, y: point.y + delta.y } : point))
      setFieldDraft({})
      return
    }
    if (interaction.kind === 'terrain-vertex') {
      const point = snapPoint({ x: Math.max(0, pointer.x), y: Math.max(0, pointer.y) })
      setBoundaryDraft((points) => points.map((current, index) => index === interaction.index ? point : current))
      setFieldDraft({})
      return
    }
    if (interaction.kind === 'create') {
      setInteraction({ ...interaction, current: snapPoint(pointer) })
      return
    }
    if (interaction.kind === 'marquee') {
      setInteraction({ ...interaction, current: pointer })
      return
    }
    if (interaction.kind === 'rotate') {
      const angle = angleBetween(pointer, { x: interaction.pointerCenterX, y: interaction.pointerCenterY })
      const rawRotation = normalizeAngle(interaction.startRotation + angle - interaction.startAngle)
      const rotatingCurved = interaction.startElements.some((element) => interaction.elementIds.includes(element.id) && element.shape === 'rectangle' && element.mandala?.bedStyle === 'curved')
      const rotation = event.shiftKey
        ? normalizeAngle(Math.round(rawRotation / 15) * 15)
        : rotationSnapEnabled && !rotatingCurved ? snapRightAngle(rawRotation) : rawRotation
      const rotationDelta = normalizeAngle(rotation - interaction.startRotation)
      const selectedStart = interaction.startElements.filter((element) => interaction.elementIds.includes(element.id))
      const rotated = interaction.startElements.map((element) => interaction.elementIds.includes(element.id)
        ? rotateElementAround(element, { x: interaction.centerX, y: interaction.centerY }, rotationDelta)
        : element)
      const rotatedBounds = selectionBounds(rotated.filter((element) => interaction.elementIds.includes(element.id)))
      const correctionX = Math.max(-rotatedBounds.minX, Math.min(initial.width - rotatedBounds.maxX, 0))
      const correctionY = Math.max(-rotatedBounds.minY, Math.min(initial.height - rotatedBounds.maxY, 0))
      const next = rotated.map((element) => interaction.elementIds.includes(element.id)
        ? translateElement(element, correctionX, correctionY)
        : element)
      if (selectedStart.length === 0 || !layoutFitsSectors(next)) {
        setWorkflowNotice('O canteiro precisa sobrepor um setor ao girar.')
        return
      }
      applyLiveElements(next)
      return
    }
    const deltaX = (event.clientX - interaction.pointerX) / scale
    const deltaY = (event.clientY - interaction.pointerY) / scale
    const primary = interaction.startElements.find((element) => element.id === interaction.elementId)
    if (!primary) return
    const primaryAnchor = elementAnchor(primary)
    const snappedAnchor = snapEnabled ? { x: snap(primaryAnchor.x + deltaX), y: snap(primaryAnchor.y + deltaY) } : { x: primaryAnchor.x + deltaX, y: primaryAnchor.y + deltaY }
    let groupDelta = { x: snappedAnchor.x - primaryAnchor.x, y: snappedAnchor.y - primaryAnchor.y }
    let nextGuides: AlignmentGuides = {}
    const alignmentTargets = interaction.startElements.filter((item) => !interaction.elementIds.includes(item.id))
    if (alignmentEnabled) {
      const aligned = alignElement(translateElement(primary, groupDelta.x, groupDelta.y), alignmentTargets)
      const alignedAnchor = elementAnchor(aligned.element)
      groupDelta = { x: groupDelta.x + alignedAnchor.x - (primaryAnchor.x + groupDelta.x), y: groupDelta.y + alignedAnchor.y - (primaryAnchor.y + groupDelta.y) }
      nextGuides = aligned.guides
    }
    const groupStartBounds = selectionBounds(interaction.startElements.filter((element) => interaction.elementIds.includes(element.id)))
    groupDelta = {
      x: Math.max(-groupStartBounds.minX, Math.min(initial.width - groupStartBounds.maxX, groupDelta.x)),
      y: Math.max(-groupStartBounds.minY, Math.min(initial.height - groupStartBounds.maxY, groupDelta.y)),
    }
    const next = interaction.startElements.map((element) => interaction.elementIds.includes(element.id)
      ? translateElement(element, groupDelta.x, groupDelta.y)
      : element)
    if (!layoutFitsSectors(next)) {
      setWorkflowNotice('O canteiro precisa sobrepor um setor ao mover.')
      return
    }
    setAlignmentGuides(nextGuides)
    applyLiveElements(next)
  }

  const snapPoint = (point: { x: number; y: number }) => snapEnabled ? { x: snap(point.x), y: snap(point.y) } : point

  const endInteraction = (event: ReactPointerEvent<SVGSVGElement>) => {
    if (svgRef.current?.hasPointerCapture(event.pointerId)) svgRef.current.releasePointerCapture(event.pointerId)
    if (!interaction) return
    if (interaction.kind === 'outline-edge' && Math.hypot(event.clientX - interaction.pointerX, event.clientY - interaction.pointerY) < 4) {
      const rect = event.currentTarget.getBoundingClientRect()
      const point = snapPoint(screenToWorld({ x: event.clientX - rect.left, y: event.clientY - rect.top }, viewport, initial, zoom, pan))
      setBoundaryDraft((points) => [...points.slice(0, interaction.index + 1), point, ...points.slice(interaction.index + 1)])
    } else if (interaction.kind === 'terrain-vertex' && interaction.closeOnClick && Math.hypot(event.clientX - interaction.pointerX, event.clientY - interaction.pointerY) < 5) {
      if (tool === 'sector-outline') finishSectorOutline()
      else finishTerrain()
    } else if (interaction.kind === 'move' || interaction.kind === 'rotate') {
      commitCurrentHistory()
      if (interaction.kind === 'move' && interaction.elementIds.length === 1 && Math.hypot(event.clientX - interaction.pointerX, event.clientY - interaction.pointerY) < 4) {
        const clicked = elementsRef.current.find(element => element.id === interaction.elementId)
        if (clicked?.role === 'bed') { setPropertiesOpen(false); setBedMenuId(clicked.id) }
        else { setBedMenuId(undefined); setPropertiesOpen(true) }
      }
    } else if (interaction.kind === 'marquee') {
      const minX = Math.min(interaction.start.x, interaction.current.x)
      const maxX = Math.max(interaction.start.x, interaction.current.x)
      const minY = Math.min(interaction.start.y, interaction.current.y)
      const maxY = Math.max(interaction.start.y, interaction.current.y)
      const selectedInMarquee = elementsRef.current.filter((element) => {
        const bounds = elementBounds(element)
        return bounds.minX >= minX && bounds.maxX <= maxX && bounds.minY >= minY && bounds.maxY <= maxY
      }).map((element) => element.id)
      const nextSelectedIds = [...new Set([...interaction.baseSelectedIds, ...selectedInMarquee])]
      setSelectedIds(nextSelectedIds)
      setSelectedId(nextSelectedIds[0] ?? '')
    } else if (interaction.kind === 'create') {
      const id = `demo-${interaction.shape}-${crypto.randomUUID()}`
      nextId.current += 1
      const previewName = interaction.role === 'sector'
        ? `Setor ${elementsRef.current.filter((item) => item.role === 'sector').length + 1}`
        : interaction.shape === 'circle' ? `Círculo ${elementsRef.current.length + 1}` : `Retângulo ${elementsRef.current.length + 1}`
      const candidateSector = interaction.role === 'bed'
        ? sectorElements.find((sector) => sectorContainsElement(sector, elementFromPoints(interaction.shape, interaction.role, interaction.start, interaction.current, initial, id, previewName)))
        : undefined
      const element = elementFromPoints(interaction.shape, interaction.role, interaction.start, interaction.current, initial, id, previewName, candidateSector?.id, candidateSector?.name)
      const dragWidth = Math.abs(interaction.current.x - interaction.start.x)
      const dragHeight = Math.abs(interaction.current.y - interaction.start.y)
      const validSize = interaction.role === 'sector'
        ? dragWidth >= 0.2 && dragHeight >= 0.2
        : element.shape === 'circle' || element.width >= 0.2 && element.height >= 0.2
      const validSector = interaction.role === 'sector' || Boolean(candidateSector && sectorContainsElement(candidateSector, element))
      if (validSize && validSector) {
        const createdElement = interaction.role === 'sector'
          ? { ...element, sectorId: undefined, sectorName: undefined }
          : element
        const next = [...elementsRef.current, createdElement]
        pushHistory(next)
        setSelectedId(createdElement.id)
        setSelectedIds([createdElement.id])
        if (interaction.role === 'sector') {
          setEditorStage('beds')
          setWorkflowNotice('Setor criado. Agora escolha um canteiro e desenhe dentro da área delimitada.')
        }
        if (interaction.role === 'bed') setWorkflowNotice(undefined)
      } else if (interaction.role === 'bed') {
        setWorkflowNotice('O canteiro precisa ser desenhado sobreposto à área de um setor.')
      } else {
        setWorkflowNotice('O setor precisa ter pelo menos 0,2 unidades de largura e altura.')
      }
      // A ferramenta de desenho é de uso único: após criar uma peça, o próximo
      // clique volta a selecionar/mover objetos em vez de criar outra peça.
      setTool('select')
    }
    setInteraction(undefined)
    setAlignmentGuides({})
  }

  const cancelInteraction = () => {
    if (interaction?.kind === 'outline-edge') setBoundaryDraft(interaction.startPoints)
    setInteraction(undefined)
    setAlignmentGuides({})
  }

  const updateElementLive = (id: string, field: EditableField, value: string) => {
    setFieldDraft((current) => ({ ...current, [fieldKey(id, field)]: value }))
    if (field === 'name') {
      applyLiveElements(elementsRef.current.map((element) => element.id === id ? { ...element, name: value } : element))
      return
    }
    const parsed = parseNumber(value)
    if (parsed === undefined) return
    const next = elementsRef.current.map((element) => {
      if (element.id !== id) return element
      if (element.shape === 'rectangle') {
        if (element.mandala && (field === 'mandalaCenterX' || field === 'mandalaCenterY' || field === 'mandalaRadius' || field === 'mandalaAngle' || field === 'mandalaArcWidth' || field === 'mandalaRadialLength')) {
          const mandala = { ...element.mandala,
            centerX: field === 'mandalaCenterX' ? parsed : element.mandala.centerX,
            centerY: field === 'mandalaCenterY' ? parsed : element.mandala.centerY,
            ringRadius: field === 'mandalaRadius' ? parsed : element.mandala.ringRadius,
            angleDeg: field === 'mandalaAngle' ? parsed : element.mandala.angleDeg,
            angularWidthDeg: field === 'mandalaArcWidth' ? parsed : element.mandala.angularWidthDeg,
            radialLength: field === 'mandalaRadialLength' ? parsed : element.mandala.radialLength,
          }
          const priority = field === 'mandalaRadius' ? 'ringRadius' : field === 'mandalaRadialLength' ? 'radialLength' : undefined
          return updateMandalaPlacement(element, mandala, initial, priority)
        }
        return clampElement({ ...element, [field]: element.role === 'sector' && field === 'rotationDeg' ? 0 : parsed, mandala: undefined }, initial)
      }
      if (field === 'centerX') return clampElement({ ...element, centerX: parsed }, initial)
      if (field === 'centerY') return clampElement({ ...element, centerY: parsed }, initial)
      return clampElement({ ...element, radius: parsed }, initial)
    })
    if (!layoutFitsSectors(next)) {
      setWorkflowNotice('Mantenha a sobreposição dos canteiros com pelo menos um setor.')
      return
    }
    applyLiveElements(next)
  }

  const commitField = (element: DemoElement, field: EditableField) => {
    const key = fieldKey(element.id, field)
    setFieldDraft((current) => ({ ...current, [key]: field === 'name' ? element.name : String(elementField(element, field)) }))
    commitCurrentHistory()
  }

  const valueFor = (element: DemoElement, field: EditableField) => fieldDraft[fieldKey(element.id, field)] ?? (field === 'name' ? element.name : String(elementField(element, field)))

  const rotateByKeyboard = useCallback((id: string, delta: number, allowRightAngleSnap = true) => {
    const rotationIds = selectedIds.includes(id) && selectedIds.length > 1 ? selectedIds : [id]
    const rotationElements = elements.filter((element) => rotationIds.includes(element.id) && element.role === 'bed')
    const center = rotationCenterFor(rotationElements)
    const anchor = rotationElements.find((element) => element.shape === 'rectangle')
    const anchorRotation = anchor?.rotationDeg ?? 0
    const rotatingCurved = rotationElements.some((element) => element.shape === 'rectangle' && element.mandala?.bedStyle === 'curved')
    const nextRotation = rotationSnapEnabled && allowRightAngleSnap && !rotatingCurved ? snapRightAngle(anchorRotation + delta) : normalizeAngle(anchorRotation + delta)
    const rotationDelta = normalizeAngle(nextRotation - anchorRotation)
    const rotated = elements.map((element) => rotationIds.includes(element.id) ? rotateElementAround(element, center, rotationDelta) : element)
    const rotatedBounds = selectionBounds(rotated.filter((element) => rotationIds.includes(element.id)))
    const correctionX = Math.max(-rotatedBounds.minX, Math.min(initial.width - rotatedBounds.maxX, 0))
    const correctionY = Math.max(-rotatedBounds.minY, Math.min(initial.height - rotatedBounds.maxY, 0))
    const next = rotated.map((element) => rotationIds.includes(element.id) ? translateElement(element, correctionX, correctionY) : element)
    if (sameElements(next, elements)) return
    setHistory((current) => [...current.slice(0, historyIndex + 1), cloneElements(next)])
    setHistoryIndex(historyIndex + 1)
    setElements(next)
  }, [elements, historyIndex, initial, rotationSnapEnabled, selectedIds])

  const renderRotationGizmo = (element: DemoElement, rotationElements: DemoElement[]) => {
    if (element.role !== 'bed' || element.shape !== 'rectangle' || tool !== 'select') return null
    const bounds = selectionBounds(rotationElements)
    const topLeft = worldToScreen({ x: bounds.minX, y: bounds.minY }, viewport, initial, zoom, pan)
    const bottomRight = worldToScreen({ x: bounds.maxX, y: bounds.maxY }, viewport, initial, zoom, pan)
    const visualCenter = rotationVisualCenterFor(element, rotationElements)
    const centerPoint = worldToScreen(visualCenter, viewport, initial, zoom, pan)
    const centerX = centerPoint.x
    const centerY = centerPoint.y
    const width = bottomRight.x - topLeft.x
    const height = bottomRight.y - topLeft.y
    const ringRadius = Math.max(18, Math.min(32, Math.hypot(width, height) / 2 + 2))
    const rotationActive = interaction?.kind === 'rotate' && interaction.elementIds.includes(element.id)
    const keyRotation = (event: ReactKeyboardEvent<SVGCircleElement>) => {
      const step = event.shiftKey ? 15 : 5
      if (event.key === 'ArrowLeft') {
        event.preventDefault()
        rotateByKeyboard(element.id, -step, !event.shiftKey)
      } else if (event.key === 'ArrowRight') {
        event.preventDefault()
        rotateByKeyboard(element.id, step, !event.shiftKey)
      }
    }
    return <g className="interactive-rotation-gizmo" aria-label={`Controles de rotação de ${rotationElements.length > 1 ? `${rotationElements.length} objetos selecionados` : element.name}`}>
      <path className={rotationActive ? 'interactive-rotation-arc active' : 'interactive-rotation-arc'} d={arcPath(centerX, centerY, ringRadius, -140, 20)} fill="none" stroke="#c45c4b" strokeWidth={rotationActive ? 6 : 4} strokeLinecap="round" pointerEvents="none" />
      <path className={rotationActive ? 'interactive-rotation-arc active' : 'interactive-rotation-arc'} d={arcPath(centerX, centerY, ringRadius, 40, 200)} fill="none" stroke="#477b9e" strokeWidth={rotationActive ? 6 : 4} strokeLinecap="round" pointerEvents="none" />
      <circle className="interactive-rotation-hit-area" cx={centerX} cy={centerY} r={ringRadius} fill="none" stroke="transparent" strokeWidth="18" pointerEvents="stroke" role="button" tabIndex={0} aria-label={`Girar ${rotationElements.length > 1 ? `${rotationElements.length} objetos selecionados` : element.name}. Use as setas para girar cinco graus`} onPointerDown={(event) => onRotationPointerDown(event, element, rotationElements)} onKeyDown={keyRotation} />
    </g>
  }

  const renderAlignmentGuides = () => {
    const topLeft = worldToScreen({ x: 0, y: 0 }, viewport, initial, zoom, pan)
    const bottomRight = worldToScreen({ x: initial.width, y: initial.height }, viewport, initial, zoom, pan)
    const xGuide = alignmentGuides.x === undefined ? undefined : worldToScreen({ x: alignmentGuides.x, y: 0 }, viewport, initial, zoom, pan).x
    const yGuide = alignmentGuides.y === undefined ? undefined : worldToScreen({ x: 0, y: alignmentGuides.y }, viewport, initial, zoom, pan).y
    return <g className="interactive-alignment-guides" pointerEvents="none" aria-hidden="true">
      {xGuide !== undefined && <line x1={xGuide} y1={topLeft.y} x2={xGuide} y2={bottomRight.y} />}
      {yGuide !== undefined && <line x1={topLeft.x} y1={yGuide} x2={bottomRight.x} y2={yGuide} />}
    </g>
  }

  const renderSelectionMarquee = () => {
    if (interaction?.kind !== 'marquee') return null
    const start = worldToScreen(interaction.start, viewport, initial, zoom, pan)
    const current = worldToScreen(interaction.current, viewport, initial, zoom, pan)
    return <rect className="interactive-selection-marquee" x={Math.min(start.x, current.x)} y={Math.min(start.y, current.y)} width={Math.abs(current.x - start.x)} height={Math.abs(current.y - start.y)} pointerEvents="none" />
  }

  const renderMandalaPreview = (element: DemoElement) => {
    if (element.shape === 'circle') {
      const center = worldToScreen({ x: element.centerX, y: element.centerY }, viewport, initial, zoom, pan)
      return <circle key={element.id} className="interactive-mandala-preview-shape" cx={center.x} cy={center.y} r={element.radius * scale} />
    }
    if (mandalaConfig.bedStyle === 'curved' && element.mandala) {
      const center = worldToScreen({ x: element.mandala.centerX, y: element.mandala.centerY }, viewport, initial, zoom, pan)
      return <path key={element.id} className="interactive-mandala-preview-shape" d={mandalaPath(element, scale, center.x, center.y, 'curved')} />
    }
    const position = worldToScreen({ x: element.x, y: element.y }, viewport, initial, zoom, pan)
    const width = element.width * scale
    const height = element.height * scale
    const centerX = position.x + width / 2
    const centerY = position.y + height / 2
    return <rect key={element.id} className="interactive-mandala-preview-shape" x={position.x} y={position.y} width={width} height={height} rx="2" transform={`rotate(${element.rotationDeg} ${centerX} ${centerY})`} />
  }

  const renderMandalaModalPreview = (element: DemoElement) => {
    if (element.shape === 'circle') {
      return <circle key={element.id} cx={element.centerX} cy={element.centerY} r={element.radius} fill="#dbe5c9" stroke="#52735a" strokeWidth="0.12" />
    }
    if (mandalaConfig.bedStyle === 'curved' && element.mandala) {
      return <path key={element.id} d={mandalaPath(element, 1, element.mandala.centerX, element.mandala.centerY, 'curved')} fill="#b9d1a6" stroke="#52735a" strokeWidth="0.12" strokeLinejoin="round" />
    }
    return <rect key={element.id} x={element.x} y={element.y} width={element.width} height={element.height} rx="0.08" fill="#b9d1a6" stroke="#52735a" strokeWidth="0.12" transform={`rotate(${element.rotationDeg} ${element.x + element.width / 2} ${element.y + element.height / 2})`} />
  }

  const openElementMenu = (event: ReactKeyboardEvent<SVGElement>, element: DemoElement) => {
    if (event.key !== 'Enter' && event.key !== ' ') return
    event.preventDefault(); event.stopPropagation()
    setSelectedId(element.id); setSelectedIds([element.id])
    if (element.role === 'bed') { setPropertiesOpen(false); setBedMenuId(element.id) }
    else { setBedMenuId(undefined); setPropertiesOpen(true) }
  }

  const renderElement = (element: DemoElement) => {
    const isSelected = selectedIds.includes(element.id)
    const isHovered = hoveredId === element.id
    const hasConflict = conflicts.has(element.id)
    const isSector = element.role === 'sector'
    const fill = hasConflict ? '#e7b3a8' : isSector ? '#d7bd78' : element.shape === 'circle' ? '#b8c99a' : '#7aa566'
    const stroke = hasConflict ? '#8d3430' : isSelected ? '#1c4939' : isSector ? '#9b7a39' : '#52735a'
    const strokeWidth = isSelected ? 3 : isSector ? 2.5 : 2
    if (element.shape === 'circle') {
      const center = worldToScreen({ x: element.centerX, y: element.centerY }, viewport, initial, zoom, pan)
      return <g key={element.id}><circle data-element={element.id} className={`interactive-object${isSelected ? ' selected' : ''}${isHovered ? ' hovered' : ''}`} cx={center.x} cy={center.y} r={element.radius * scale} fill={fill} fillOpacity={isSelected ? '0.94' : isHovered ? '0.9' : '0.82'} stroke={stroke} strokeWidth={strokeWidth} role="button" tabIndex={0} aria-label={`${element.name}, círculo de raio ${formatMeters(element.radius)} unidades de desenho`} onPointerEnter={() => setHoveredId(element.id)} onPointerLeave={() => setHoveredId((current) => current === element.id ? undefined : current)} onPointerDown={(event) => onElementPointerDown(event, element.id)} onKeyDown={(event) => openElementMenu(event, element)} /><text x={center.x} y={center.y} textAnchor="middle" dominantBaseline="central" className="interactive-svg-label" pointerEvents="none">{canvasElementLabel(element)}</text></g>
    }
    if (element.mandala?.bedStyle === 'curved') {
      const center = worldToScreen({ x: element.mandala.centerX, y: element.mandala.centerY }, viewport, initial, zoom, pan)
      const label = mandalaLabelPoint(element, scale, center.x, center.y)
      return <g key={element.id}><path data-element={element.id} className={`interactive-curved-element${isSelected ? ' selected' : ''}${isHovered ? ' hovered' : ''}`} d={mandalaPath(element, scale, center.x, center.y)} fill={fill} fillOpacity={isSelected ? '0.94' : isHovered ? '0.9' : '0.82'} stroke={hasConflict ? '#8d3430' : '#52735a'} strokeWidth={2} strokeLinejoin="round" role="button" tabIndex={0} aria-label={`${element.name}, canteiro curvilíneo da mandala`} onPointerEnter={() => setHoveredId(element.id)} onPointerLeave={() => setHoveredId((current) => current === element.id ? undefined : current)} onPointerDown={(event) => onElementPointerDown(event, element.id)} onKeyDown={(event) => openElementMenu(event, element)} /><text x={label.x} y={label.y} textAnchor="middle" dominantBaseline="central" className="interactive-svg-label" pointerEvents="none">{canvasElementLabel(element)}</text></g>
    }
    const position = worldToScreen({ x: element.x, y: element.y }, viewport, initial, zoom, pan)
    const width = element.width * scale
    const height = element.height * scale
    const labelX = isSector ? position.x + 8 : position.x + width / 2
    const labelY = isSector ? position.y + 16 : position.y + height / 2
    if (element.outline) return <g key={element.id}><polygon points={element.outline.map((point) => { const screen = worldToScreen(point, viewport, initial, zoom, pan); return screen.x + ',' + screen.y }).join(' ')} className="interactive-object interactive-sector-object" fill={fill} fillOpacity="0.18" stroke={stroke} strokeWidth={strokeWidth} role="button" tabIndex={0} aria-label={element.name + ', setor de contorno livre'} onPointerDown={(event) => onElementPointerDown(event, element.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelectedId(element.id); setSelectedIds([element.id]); setPropertiesOpen(true) } }} /><text x={position.x + 6} y={position.y + 16} className="interactive-svg-label" pointerEvents="none">{element.name}</text></g>
    return <g key={element.id}><rect data-element={element.id} className={`interactive-object${isSector ? ' interactive-sector-object' : ''}${isSelected ? ' selected' : ''}${isHovered ? ' hovered' : ''}`} x={position.x} y={position.y} width={width} height={height} fill={fill} fillOpacity={isSelected ? '0.32' : isHovered ? '0.26' : isSector ? '0.18' : '0.82'} stroke={stroke} strokeWidth={strokeWidth} strokeDasharray={isSector ? '7 5' : undefined} rx="3" role="button" tabIndex={0} aria-label={`${element.name}, ${isSector ? 'setor de ' : ''}${formatMeters(element.width)} por ${formatMeters(element.height)} unidades de desenho`} transform={`rotate(${element.rotationDeg} ${position.x + width / 2} ${position.y + height / 2})`} onPointerEnter={() => setHoveredId(element.id)} onPointerLeave={() => setHoveredId((current) => current === element.id ? undefined : current)} onPointerDown={(event) => onElementPointerDown(event, element.id)} onKeyDown={(event) => openElementMenu(event, element)} /><text x={labelX} y={labelY} textAnchor={isSector ? 'start' : 'middle'} dominantBaseline={isSector ? 'auto' : 'central'} className="interactive-svg-label" pointerEvents="none" transform={isSector ? undefined : `rotate(${element.rotationDeg} ${position.x + width / 2} ${position.y + height / 2})`}>{canvasElementLabel(element)}{isSector ? ' · área de cultivo' : ''}</text></g>
  }

  const renderSectorProjection = (sector: NonNullable<DemoGarden['sectors']>[number]) => {
    const topLeft = worldToScreen({ x: sector.x, y: sector.y }, viewport, initial, zoom, pan)
    const bottomRight = worldToScreen({ x: sector.x + sector.width, y: sector.y + sector.height }, viewport, initial, zoom, pan)
    return <g key={`sector-${sector.id}`} pointerEvents="none">
      <rect x={topLeft.x} y={topLeft.y} width={Math.max(0, bottomRight.x - topLeft.x)} height={Math.max(0, bottomRight.y - topLeft.y)} rx="8" fill="#d9e8b8" fillOpacity="0.12" stroke="#7c9b68" strokeWidth="1.4" strokeDasharray="6 5" />
      <text x={topLeft.x + 6} y={topLeft.y + 15} className="interactive-svg-label">{sector.name} · projeção</text>
    </g>
  }

  const saveConfiguration = async () => {
    const result = await saving.save({ ...initial, boundary, elements: cloneElements(elements) })
    if (!result?.layout) return false
    const map = { ...initial, ...result.layout, boundary: result.layout.boundary ?? undefined, projectionKey: 'saved-layout:' + route.garden + ':' + result.version }
    setConfirmedSignature(JSON.stringify({ boundary: map.boundary ?? null, elements: map.elements }))
    setDraftBaseKey(map.projectionKey); setResetBaseline(map); setBoundary(map.boundary); setExtent({ width: map.width, height: map.height })
    applyHistorySnapshot(map.elements, 0); setHistory([cloneElements(map.elements)]); setFieldDraft({})
    writeInteractiveDraft(route.garden, map)
    setWorkflowNotice('Configuração salva. O mapa e os novos cadastros já estão disponíveis fora do modo interativo.')
    return true
  }

  const bedCount = elements.filter((element) => element.role === 'bed').length
  const selectedElements = elements.filter((element) => selectedIds.includes(element.id))
  const selectedBed = selectedElements.length === 1 && selectedElements[0].role === 'bed' ? selectedElements[0] : undefined
  const startTerrainEdit = () => { setDraftClosed(Boolean(boundary)); setBoundaryDraft(boundary ? boundary.map((point) => ({ ...point })) : []); setFieldDraft({}); setTool('terrain'); setEditorStage('terrain'); setWorkflowNotice(undefined) }
  const selectSectorsStage = () => { setEditorStage('sectors'); setTool('select'); setWorkflowNotice(undefined) }
  const selectBedsStage = () => { setEditorStage('beds'); setTool('select'); setWorkflowNotice(undefined) }
  const chooseSector = (id: string) => { setSelectedId(id); setSelectedIds(id ? [id] : []); setPropertiesOpen(false); setFieldDraft({}); setTool('select') }
  const editSector = (sector: Extract<DemoElement, { shape: 'rectangle' }>) => { setSelectedId(sector.id); setSelectedIds([sector.id]); setFieldDraft({}); setPropertiesOpen(true); setEditorStage('sectors'); setTool('select') }
  const toolHint = tool === 'terrain' ? (draftClosed && boundaryDraft.length > 2 ? 'Arraste nós ou arestas para ajustar · clique numa aresta adiciona nó · botão direito remove nó' : 'Clique para marcar os vértices · clique no primeiro ponto para fechar o contorno')
    : tool === 'sector-outline' ? 'Clique para marcar os nós do setor dentro do terreno · botão direito remove nó · clique no primeiro ponto para concluir'
    : tool === 'sector' ? 'Arraste dentro do terreno para desenhar o setor · Esc cancela'
    : tool === 'rectangle' ? 'Arraste dentro de um setor para desenhar o canteiro · Esc cancela'
    : tool === 'circle' ? 'Arraste do centro para fora para desenhar o canteiro circular · Esc cancela'
    : tool === 'pan' ? 'Arraste para mover a vista · a roda do mouse aproxima ou afasta'
    : 'Clique para selecionar · Shift soma à seleção · arraste para mover · Espaço + arraste move a vista'
  const stageInfo: Record<EditorStage, { title: string; description: string }> = {
    terrain: { title: boundary ? 'Contorno do terreno' : 'Delimite o terreno', description: 'Escolha um formato pronto ou marque os vértices no mapa. As medidas reais são opcionais: a grade usa unidades de desenho.' },
    sectors: { title: 'Setores', description: tool === 'sector-outline' ? 'Marque o contorno do setor no mapa e conclua.' : 'Divida o terreno em áreas de cultivo. Todo canteiro precisa ficar dentro de um setor.' },
    beds: { title: 'Canteiros', description: 'Desenhe no mapa ou insira uma forma pronta no setor escolhido. Clique num canteiro para configurar cultivo e plantio.' },
  }

  const terrainTools = <>
    {tool === 'terrain' ? <PanelSection title="Contorno em edição">
      <p className="mt-0 mb-2 text-xs text-horta-muted" role="status">{boundaryDraft.length} {boundaryDraft.length === 1 ? 'vértice' : 'vértices'}{boundaryDraft.length >= 3 ? ' · pronto para concluir' : ' · marque pelo menos 3'}</p>
      <div className="grid grid-cols-2 gap-2">
        <button type="button" className="secondary !min-h-10 !text-xs" disabled={boundaryDraft.length === 0} onClick={() => { setBoundaryDraft((points) => points.slice(0, -1)); setFieldDraft({}) }}>Desfazer ponto</button>
        <button type="button" className="secondary !min-h-10 !text-xs" disabled={boundaryDraft.length === 0} onClick={() => { setBoundaryDraft([]); setDraftClosed(false); setFieldDraft({}) }}>Recomeçar</button>
      </div>
      <button type="button" className="primary mt-2 w-full" disabled={!validBoundary(boundaryDraft)} onClick={() => finishTerrain()}>Concluir terreno</button>
      {boundary && <button type="button" className="text-button mt-1 w-full" onClick={() => { setBoundaryDraft([]); setFieldDraft({}); setTool('select'); setEditorStage('sectors') }}>Cancelar edição do terreno</button>}
    </PanelSection> : <PanelSection title="Contorno">
      <ToolTile label={boundary ? 'Editar contorno' : 'Desenhar contorno'} hint="Mover, adicionar ou remover nós" icon={<PenTool />} onClick={startTerrainEdit} />
    </PanelSection>}
    {tool === 'terrain' && <PanelSection title="Formatos prontos"><TerrainPresets compact select={chooseTerrainPreset} /></PanelSection>}
    {boundary && <PanelSection title="Remover" tone="danger"><button type="button" className="danger-outline w-full !text-xs" disabled={terrainHasSectors} aria-describedby="terrain-remove-help" onClick={removeTerrain}>Remover terreno</button><p id="terrain-remove-help" className="mt-2 mb-0 text-[11px] text-horta-muted">{terrainHasSectors ? 'Remova os setores para liberar a remoção do terreno.' : 'Remove apenas o contorno deste editor.'}</p></PanelSection>}
  </>

  const sectorTools = <>
    {tool === 'sector-outline' && <PanelSection title={editingSectorId ? 'Editando contorno do setor' : 'Novo setor por contorno'}>
      <p className="mt-0 mb-2 text-xs text-horta-muted" role="status">{boundaryDraft.length} {boundaryDraft.length === 1 ? 'nó marcado' : 'nós marcados'}</p>
      <button type="button" className="primary w-full" disabled={!validBoundary(boundaryDraft)} onClick={finishSectorOutline}>Concluir setor</button>
      <button type="button" className="text-button mt-1 w-full" onClick={() => { setBoundaryDraft([]); setEditingSectorId(undefined); setTool('select') }}>Cancelar setor</button>
    </PanelSection>}
    <PanelSection title="Criar setor">
      <div className="grid gap-1.5">
        <ToolTile label="Retângulo" hint="Arraste no mapa" shortcut="S" icon={<SquareDashed />} pressed={tool === 'sector'} disabled={!canEditSectors} onClick={() => { setEditorStage('sectors'); setTool(tool === 'sector' ? 'select' : 'sector') }} />
        <ToolTile label="Contorno livre" hint="Marque os nós no mapa" icon={<PenTool />} disabled={!canEditSectors} onClick={() => beginSectorOutline()} />
        <ToolTile label="Igual ao terreno" ariaLabel="Setor com contorno do terreno" hint="Usa o contorno inteiro" icon={<Frame />} disabled={!canEditSectors} onClick={() => beginSectorOutline(undefined, true)} />
      </div>
      {!boundary && <p className="mt-2 mb-0 text-[11px] text-horta-muted">Conclua o terreno para começar.</p>}
    </PanelSection>
    <PanelSection title={`Setores no mapa (${sectorElements.length})`}>
      {sectorElements.length === 0 ? <p className="m-0 text-xs text-horta-muted">Nenhum setor ainda.</p> : <ul className="m-0 grid list-none gap-1 p-0">
        {sectorElements.map((sector) => {
          const active = selectedSector?.id === sector.id
          const count = elements.filter((element) => element.role === 'bed' && element.sectorId === sector.id).length
          return <li key={sector.id} className={`flex items-center gap-1 rounded-md border pr-1 ${active ? 'border-horta-forest bg-[#eef4e8]' : 'border-horta-line bg-white'}`}>
            <button type="button" className="min-h-10 min-w-0 flex-1 truncate rounded-md border-0 bg-transparent px-2 text-left text-sm font-medium text-horta-ink shadow-none hover:!bg-transparent" aria-pressed={active} disabled={drawingOutline} onClick={() => chooseSector(sector.id)}>{sector.name}<span className="ml-1 text-xs font-normal text-horta-muted">· {count} {count === 1 ? 'canteiro' : 'canteiros'}</span></button>
            <button type="button" className="interactive-icon-button" title={`Editar ${sector.name}`} aria-label={`Editar ${sector.name}`} disabled={drawingOutline} onClick={() => editSector(sector)}><Pencil aria-hidden="true" /></button>
            <button type="button" className="interactive-icon-button danger" title={`Excluir ${sector.name}`} aria-label={`Excluir ${sector.name}`} disabled={drawingOutline} onClick={() => removeSelected(sector.id)}><Trash2 aria-hidden="true" /></button>
          </li>
        })}
      </ul>}
    </PanelSection>
    {sectorElements.length > 0 && !drawingOutline && <PanelSection title="Próxima etapa"><button type="button" className="secondary w-full" onClick={selectBedsStage}>Adicionar canteiros →</button></PanelSection>}
  </>

  const bedTools = <>
    <PanelSection title="Setor de destino">
      <select aria-label="Setor de destino das formas prontas" value={selectedSector?.id ?? sectorElements[0]?.id ?? ''} disabled={drawingOutline || sectorElements.length === 0} onChange={(event) => chooseSector(event.target.value)}>{sectorElements.map((sector) => <option key={sector.id} value={sector.id}>{sector.name}</option>)}</select>
      <p className="mt-1.5 mb-0 text-[11px] text-horta-muted">Formas prontas e mandalas entram neste setor.</p>
    </PanelSection>
    <PanelSection title="Desenhar no mapa">
      <div className="grid grid-cols-2 gap-1.5">
        <ToolTile label="Retângulo" hint="Arrastar" shortcut="R" icon={<Square />} pressed={tool === 'rectangle'} disabled={!canPlaceBeds} onClick={() => { setEditorStage('beds'); setTool(tool === 'rectangle' ? 'select' : 'rectangle') }} />
        <ToolTile label="Círculo" hint="Arrastar" shortcut="C" icon={<Circle />} pressed={tool === 'circle'} disabled={!canPlaceBeds} onClick={() => { setEditorStage('beds'); setTool(tool === 'circle' ? 'select' : 'circle') }} />
      </div>
    </PanelSection>
    <PanelSection title="Inserir forma pronta">
      <div className="grid grid-cols-2 gap-1.5">
        <ToolTile label="Retângulo" ariaLabel="+ Retângulo" hint="2 × 1 u" icon={<RectangleHorizontal />} disabled={!canPlaceBeds} onClick={() => addElement('rectangle')} />
        <ToolTile label="Círculo" ariaLabel="+ Círculo" hint="No centro" icon={<Circle />} disabled={!canPlaceBeds} onClick={() => addElement('circle')} />
        <ToolTile label="Curvilíneo" ariaLabel="+ Canteiro curvilíneo" hint="Arco de 72°" icon={<Rainbow />} disabled={!canPlaceBeds} onClick={addCurvedElement} />
        <ToolTile label="Mandala" ariaLabel="Gerar mandala" hint="Configurar" icon={<Sun />} disabled={!canPlaceBeds} onClick={openMandala} />
      </div>
    </PanelSection>
    <PanelSection title="Seleção">
      {selectedBed ? <div className="grid gap-1.5">
        <p className="m-0 truncate text-sm font-semibold text-horta-forest">{selectedBed.name}</p>
        <button type="button" className="primary w-full" onClick={() => { setPropertiesOpen(false); setBedMenuId(selectedBed.id) }}>Cultivo e plantio</button>
        <div className="grid grid-cols-2 gap-1.5"><button type="button" className="secondary !min-h-10 !text-xs" onClick={() => setPropertiesOpen(true)}>Editar forma</button><button type="button" className="danger-outline !min-h-10 !text-xs" onClick={() => removeSelected()}>Remover</button></div>
      </div> : selectedElements.length > 1 ? <div className="grid gap-1.5">
        <p className="m-0 text-sm"><strong>{selectedElements.length}</strong> objetos selecionados</p>
        <p className="m-0 text-[11px] text-horta-muted">Arraste para mover juntos ou use o arco para girar o grupo.</p>
        <button type="button" className="danger-outline !min-h-10 !text-xs" onClick={() => removeSelected()}>Remover selecionados</button>
      </div> : <p className="m-0 text-xs text-horta-muted">Clique num canteiro para configurar cultivo e plantio. Shift soma à seleção; arraste numa área vazia para selecionar vários.</p>}
    </PanelSection>
  </>

  const drawingTools = editorStage === 'terrain' ? terrainTools : editorStage === 'sectors' ? sectorTools : bedTools
  const saveDisabled = !organization || saving.busy || saving.error?.status === 409 || drawingOutline && boundaryDraft.length > 0 || !boundary && saving.version === 0
  const hasChanges = dirty || unsaved
  const saveStatus = saving.busy ? { tone: 'pending', text: 'Salvando…' }
    : saving.uncertain ? { tone: 'warning', text: 'Confirmação pendente' }
    : hasChanges ? { tone: 'warning', text: 'Alterações não salvas' }
    : saving.savedAt ? { tone: 'ok', text: 'Configuração salva · versão ' + saving.version }
    : { tone: 'muted', text: 'Ainda não salvo' }
  const stepsNav = <GardenSetupSteps stage={editorStage} hasTerrain={Boolean(boundary)} sectorCount={sectorElements.length} bedCount={bedCount} canEditSectors={canEditSectors} canPlaceBeds={canPlaceBeds}
    selectTerrain={startTerrainEdit} selectSectors={selectSectorsStage} selectBeds={selectBedsStage} />

  return <div className="interactive-garden">
    <header className="interactive-header">
      <div className="min-w-0">
        <h1 className="m-0 text-xl font-semibold text-horta-forest">Mapa interativo da horta</h1>
        <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-horta-muted">
          <span>{sectorElements.length} {sectorElements.length === 1 ? 'setor' : 'setores'} · {bedCount} {bedCount === 1 ? 'canteiro' : 'canteiros'}</span>
          <span aria-hidden="true">·</span>
          <span>{initial.areaSource === 'registered' ? `${formatMeters(initial.areaM2 ?? initial.width * initial.height)} m² cadastrados; a grade não tem escala real` : 'Grade em unidades de desenho, sem escala real'}</span>
          {!organization && <><span aria-hidden="true">·</span><span className="text-[#8d3430]">Cadastros indisponíveis; não é possível salvar</span></>}
          <details className="interactive-help-popover"><summary>Como funciona</summary><div><p>1. Delimite o terreno. 2. Divida em setores. 3. Posicione os canteiros dentro dos setores.</p><p>O rascunho fica só neste navegador até você usar <strong>Salvar configuração</strong>; então o mapa aparece na visão panorâmica e os canteiros novos são cadastrados.</p><p>Atalhos: V selecionar · H mover vista · S setor · R/C canteiro · F ajustar · Ctrl+Z desfazer · Delete remove · Esc cancela.</p></div></details>
        </div>
      </div>
      <div className="flex w-full flex-wrap items-center gap-2 sm:w-auto">
        <span className={`interactive-save-status w-full sm:w-auto ${saveStatus.tone}`} role="status">{saveStatus.text}</span>
        <a className="secondary action-link flex-1 sm:flex-none" href={routeHref(route.garden, 'visao-geral', 'painel')}>Ver visão panorâmica</a>
        {saving.error?.status === 409 && <button type="button" className="secondary" onClick={() => { if (window.confirm('Seu rascunho será descartado para carregar a configuração atual. Continuar?')) { window.sessionStorage.removeItem('horta-interativa-layout:v3:' + route.garden); window.location.reload() } }}>Recarregar configuração salva</button>}
        <button type="button" className="primary inline-flex flex-1 items-center justify-center gap-2 sm:flex-none" disabled={saveDisabled} onClick={() => void saveConfiguration()}><Save aria-hidden="true" className="size-4" />{saving.busy ? 'Salvando configuração…' : saving.uncertain ? 'Tentar salvar novamente' : 'Salvar configuração'}</button>
      </div>
    </header>
    {projectionWarnings.filter((warning) => !warning.includes("quadro") && !warning.includes("envelope quadrado")).map((warning) => <p className="notice" role="status" key={warning}>{warning}</p>)}
    {saving.error && <Failure error={saving.error} />}
    {saving.uncertain && <p className="notice" role="status">A confirmação não chegou. Tente salvar novamente para resolver a mesma tentativa antes de editar.</p>}

    {mandalaOpen && <div className="interactive-modal-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) setMandalaOpen(false) }}>
      <section className="interactive-mandala-modal panel" role="dialog" aria-modal="true" aria-labelledby="mandala-title">
        <div className="interactive-panel-heading"><div><span className="eyebrow">Preset geométrico local</span><h2 id="mandala-title">Gerar mandala</h2></div><button type="button" className="secondary" onClick={() => setMandalaOpen(false)}>Fechar</button></div>
        <p className="hint">A prévia usa o formato de canteiro escolhido. O padrão é uma mandala com três canteiros curvilíneos; o formato retangular fica disponível como configuração própria. Ao aplicar, cada peça poderá ser movida, girada ou removida.</p>
        <form onSubmit={(event) => { event.preventDefault(); setMandalaShowValidation(true); applyMandala() }}>
          <div className="interactive-mandala-layout">
            <div className="interactive-mandala-fields">
              <label>Centro X <span className="interactive-unit">u</span><input type="number" min="0" max={initial.width} step="0.1" value={mandalaConfig.centerX} onChange={(event) => updateMandalaNumber('centerX', event.target.value)} /></label>
              <label>Centro Y <span className="interactive-unit">u</span><input type="number" min="0" max={initial.height} step="0.1" value={mandalaConfig.centerY} onChange={(event) => updateMandalaNumber('centerY', event.target.value)} /></label>
              <label>Raio total <span className="interactive-unit">u</span><input type="number" min="0.8" max="14" step="0.1" value={mandalaConfig.radius} onChange={(event) => updateMandalaNumber('radius', event.target.value)} /><small className="interactive-field-help">Distância do centro até a borda externa.</small></label>
              <label>Raio central <span className="interactive-unit">u</span><input type="number" min="0" max="10" step="0.1" value={mandalaConfig.innerRadius} onChange={(event) => updateMandalaNumber('innerRadius', event.target.value)} /></label>
              <label className="interactive-mandala-wide">Formato dos canteiros<select value={mandalaConfig.bedStyle} onChange={(event) => updateMandalaStyle(event.target.value as MandalaConfig['bedStyle'])}><option value="curved">3 curvilíneos (padrão)</option><option value="rectangle">Retangulares configuráveis</option></select></label>
              <label>Divisões radiais<input type="number" min="3" max="16" step="1" value={mandalaConfig.divisions} disabled={mandalaConfig.bedStyle === 'curved'} onChange={(event) => updateMandalaNumber('divisions', event.target.value)} /></label>
              <label>Anéis<input type="number" min="1" max="4" step="1" value={mandalaConfig.rings} disabled={mandalaConfig.bedStyle === 'curved'} onChange={(event) => updateMandalaNumber('rings', event.target.value)} /></label>
              <label className="interactive-mandala-wide">Orientação <span className="interactive-unit">°</span><input type="number" step="15" value={mandalaConfig.orientationDeg} onChange={(event) => updateMandalaNumber('orientationDeg', event.target.value)} /></label>
            </div>
            <div className="interactive-mandala-preview" aria-label="Pré-visualização da mandala">
              <svg viewBox={`0 0 ${initial.width} ${initial.height}`} role="img" aria-label={`${mandalaPreview.length} peças na pré-visualização`}><rect x="0" y="0" width={initial.width} height={initial.height} fill="#f8faf5" stroke="#66856b" strokeWidth="0.12" />{mandalaPreview.map(renderMandalaModalPreview)}</svg>
              <span>{mandalaPreview.length} peças individuais</span>
            </div>
          </div>
          {mandalaShowValidation && mandalaError && <p className="feedback error" role="alert">{mandalaError}</p>}
          <div className="interactive-mandala-actions"><button type="button" className="secondary" onClick={() => setMandalaOpen(false)}>Cancelar</button><button type="submit" className="primary">Aplicar mandala</button></div>
        </form>
      </section>
    </div>}


    {propertiesOpen && selected && <section className="interactive-properties-popover" style={propertiesPosition} role="dialog" aria-labelledby="properties-title" onPointerDown={(event) => event.stopPropagation()}>
        <header className="interactive-properties-header">
          <div><span className="eyebrow">Edição do objeto</span><h2 id="properties-title">{selected.name}</h2><p>As alterações ficam no rascunho local e respeitam os limites do terreno.</p></div>
          <button ref={propertiesCloseRef} type="button" className="interactive-properties-close" onClick={() => setPropertiesOpen(false)} aria-label="Fechar propriedades">×</button>
        </header>
        <div className="interactive-properties-body">
          {selected.role === 'bed' && <button type="button" className="primary mb-4 w-full" onClick={() => { setPropertiesOpen(false); setBedMenuId(selected.id) }}>Configurar cultivo e plantio</button>}
          <div className="interactive-properties-summary"><span className={selected.shape === 'circle' ? 'interactive-element-swatch circle' : 'interactive-element-swatch'} aria-hidden="true" /><div><strong>{selected.role === 'sector' ? 'Setor de cultivo' : selected.shape === 'circle' ? 'Círculo' : selected.mandala?.bedStyle === 'curved' ? 'Canteiro curvilíneo' : selected.mandala ? 'Canteiro radial' : 'Retângulo'}</strong><span>{formatMeters(elementArea(selected))} u² de área desenhada</span></div></div>
          {selected.shape === 'rectangle' && selected.role === 'bed' && <p className="interactive-rotation-hint">{selected.mandala?.bedStyle === 'curved' ? 'Arraste o arco para girar o canteiro ao redor do centro da mandala. Use Shift para passos de 15°.' : 'Arraste o arco ao redor do objeto para girar. Perto de 0° ou 90°, ele se alinha automaticamente; use Shift para passos de 15°.'}</p>}
          <div className="interactive-field-grid full">
            <label className="interactive-name-field">Nome<input type="text" value={valueFor(selected, 'name')} onChange={(event) => updateElementLive(selected.id, 'name', event.target.value)} onBlur={() => commitField(selected, 'name')} /></label>
            {selected.shape === 'rectangle' ? selected.outline ? <button type="button" className="secondary" onClick={() => beginSectorOutline(selected)}>Editar contorno do setor</button> : <>
              {selected.mandala?.bedStyle === 'curved' ? <>
                <label>Centro X <span className="interactive-unit">u</span><input inputMode="decimal" type="text" min={selectedMandalaLimits?.minCenterX} max={selectedMandalaLimits?.maxCenterX} value={valueFor(selected, 'mandalaCenterX')} onChange={(event) => updateElementLive(selected.id, 'mandalaCenterX', event.target.value)} onBlur={() => commitField(selected, 'mandalaCenterX')} /></label>
                <label>Centro Y <span className="interactive-unit">u</span><input inputMode="decimal" type="text" min={selectedMandalaLimits?.minCenterY} max={selectedMandalaLimits?.maxCenterY} value={valueFor(selected, 'mandalaCenterY')} onChange={(event) => updateElementLive(selected.id, 'mandalaCenterY', event.target.value)} onBlur={() => commitField(selected, 'mandalaCenterY')} /></label>
              </> : <>
                <label>X <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'x')} onChange={(event) => updateElementLive(selected.id, 'x', event.target.value)} onBlur={() => commitField(selected, 'x')} /></label>
                <label>Y <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'y')} onChange={(event) => updateElementLive(selected.id, 'y', event.target.value)} onBlur={() => commitField(selected, 'y')} /></label>
                <label>Largura <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'width')} onChange={(event) => updateElementLive(selected.id, 'width', event.target.value)} onBlur={() => commitField(selected, 'width')} /></label>
                <label>Altura <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'height')} onChange={(event) => updateElementLive(selected.id, 'height', event.target.value)} onBlur={() => commitField(selected, 'height')} /></label>
                {selected.role !== 'sector' && <label>Rotação <span className="interactive-unit">°</span><input inputMode="decimal" type="text" value={valueFor(selected, 'rotationDeg')} onChange={(event) => updateElementLive(selected.id, 'rotationDeg', event.target.value)} onBlur={() => commitField(selected, 'rotationDeg')} /></label>}
              </>}
              {selected.mandala && <>
                <p className="interactive-circular-title">{selected.mandala.bedStyle === 'curved' ? 'Dimensão curvilínea da mandala' : 'Dimensão circular da mandala'}</p>
                <label>Raio do anel <span className="interactive-unit">u</span><input inputMode="decimal" type="text" min="0.4" max={selectedMandalaLimits?.maxRingRadius} value={valueFor(selected, 'mandalaRadius')} onChange={(event) => updateElementLive(selected.id, 'mandalaRadius', event.target.value)} onBlur={() => commitField(selected, 'mandalaRadius')} /></label>
                <label>Abertura no arco <span className="interactive-unit">°</span><input inputMode="decimal" type="text" min="2" max="120" value={valueFor(selected, 'mandalaArcWidth')} onChange={(event) => updateElementLive(selected.id, 'mandalaArcWidth', event.target.value)} onBlur={() => commitField(selected, 'mandalaArcWidth')} /></label>
                <label>Profundidade radial <span className="interactive-unit">u</span><input inputMode="decimal" type="text" min="0.2" max={selectedMandalaLimits?.maxRadialLength} value={valueFor(selected, 'mandalaRadialLength')} onChange={(event) => updateElementLive(selected.id, 'mandalaRadialLength', event.target.value)} onBlur={() => commitField(selected, 'mandalaRadialLength')} /></label>
                <label>Ângulo no anel <span className="interactive-unit">°</span><input inputMode="decimal" type="text" min="-180" max="180" value={valueFor(selected, 'mandalaAngle')} onChange={(event) => updateElementLive(selected.id, 'mandalaAngle', event.target.value)} onBlur={() => commitField(selected, 'mandalaAngle')} /></label>
                {selectedMandalaLimits && <p className="interactive-geometry-limit"><strong>Limites atuais</strong> · raio do anel até {formatMeters(selectedMandalaLimits.maxRingRadius)} u · profundidade radial até {formatMeters(selectedMandalaLimits.maxRadialLength)} u · abertura entre 2° e 120°.</p>}
              </>}
            </> : <>
              <label>Centro X <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'centerX')} onChange={(event) => updateElementLive(selected.id, 'centerX', event.target.value)} onBlur={() => commitField(selected, 'centerX')} /></label>
              <label>Centro Y <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'centerY')} onChange={(event) => updateElementLive(selected.id, 'centerY', event.target.value)} onBlur={() => commitField(selected, 'centerY')} /></label>
              <label>Raio <span className="interactive-unit">u</span><input inputMode="decimal" type="text" value={valueFor(selected, 'radius')} onChange={(event) => updateElementLive(selected.id, 'radius', event.target.value)} onBlur={() => commitField(selected, 'radius')} /></label>
            </>}
          </div>
          <div className="interactive-properties-actions"><button type="button" className="danger-outline" onClick={() => removeSelected()}>Remover objeto</button><button type="button" className="primary" onClick={() => setPropertiesOpen(false)}>Concluir</button></div>
        </div>
    </section>}

    <div className="interactive-editor" inert={!bedMenuElement && (saving.busy || saving.uncertain)}>
      <section className="interactive-canvas-panel" aria-label="Mapa 2D da horta">

        <div className="drawing-workspace grid min-w-0 items-stretch gap-3 lg:grid-cols-[minmax(0,1fr)_auto] lg:grid-rows-[minmax(0,1fr)]">
        <div className="drawing-grid order-1 flex min-w-0 flex-col lg:col-start-1 lg:row-start-1">
        <CanvasToolbar tool={tool === 'select' ? 'select' : tool === 'pan' ? 'pan' : 'other'} selectTool={setTool} zoom={zoom}
          zoomIn={() => zoomAt(zoom + 0.15, { x: viewport.width / 2, y: viewport.height / 2 })} zoomOut={() => zoomAt(zoom - 0.15, { x: viewport.width / 2, y: viewport.height / 2 })} fit={fit}
          canUndo={historyIndex > 0} canRedo={historyIndex < history.length - 1} undo={undo} redo={redo} canReset={dirty} reset={reset}
          options={{ grid: showGrid, snap: snapEnabled, rotationSnap: rotationSnapEnabled, alignment: alignmentEnabled }}
          changeOption={(option, value) => { if (option === 'grid') setShowGrid(value); else if (option === 'snap') setSnapEnabled(value); else if (option === 'rotationSnap') setRotationSnapEnabled(value); else setAlignmentEnabled(value) }}
          disabled={Boolean(bedMenuElement) || saving.busy || saving.uncertain} />

        <div className="relative min-w-0">
        <div className={`interactive-canvas tool-${tool}${spacePressed || interaction?.kind === 'pan' ? ' panning' : ''}`} ref={canvasRef} inert={bedFormOpen || saving.busy || saving.uncertain} style={{ touchAction: tool === "select" && !spacePressed ? "pan-y" : "none" }}>
          {viewport.width > 0 && <svg ref={setSvgRef} width="100%" height="100%" viewBox={`0 0 ${viewport.width} ${viewport.height}`} role="application" aria-label="Mapa 2D projetado a partir do cadastro; selecione objetos no mapa e use as ferramentas para criar formas" onPointerDownCapture={removeNodeAtPointer} onContextMenu={(event) => { if (drawingOutline) { event.preventDefault(); event.stopPropagation() } }} onAuxClick={(event) => { if (event.button === 1) event.preventDefault() }} onPointerDown={onPointerDown} onPointerMove={onPointerMove} onPointerUp={endInteraction} onPointerCancel={cancelInteraction}>
            <defs><pattern id="interactive-grid" width={scale} height={scale} patternUnits="userSpaceOnUse" x={terrain.x} y={terrain.y}><path d={`M ${scale} 0 L 0 0 0 ${scale}`} fill="none" stroke="#d9e4d7" strokeWidth="1" /></pattern><pattern id="interactive-grid-major" width={scale * 5} height={scale * 5} patternUnits="userSpaceOnUse" x={terrain.x} y={terrain.y}><path d={`M ${scale * 5} 0 L 0 0 0 ${scale * 5}`} fill="none" stroke="#b5c8b2" strokeWidth="1.2" /></pattern></defs>
            <rect x="0" y="0" width={viewport.width} height={viewport.height} fill="#f8faf5" />
            {showGrid && <rect x={0} y={0} width={viewport.width} height={viewport.height} fill="url(#interactive-grid)" pointerEvents="none" />}
            {showGrid && <rect x={0} y={0} width={viewport.width} height={viewport.height} fill="url(#interactive-grid-major)" pointerEvents="none" />}

            {!boundary && <text x={24} y={28} className="interactive-svg-label">Terreno não definido · marque os vértices e conclua o contorno</text>}
            {boundary && <polygon points={boundary.map((point) => { const screen = worldToScreen(point, viewport, initial, zoom, pan); return screen.x + ',' + screen.y }).join(' ')} fill="rgba(102,133,107,.07)" stroke="#365c3b" strokeWidth="3" pointerEvents="none" />}

            {renderSelectionMarquee()}
            {mandalaOpen && mandalaPreview.map(renderMandalaPreview)}
            {boundary && sectorElements.length === 0 && initial.sectors?.map(renderSectorProjection)}
            {boundary && elements.filter((element) => element.role === 'sector').map(renderElement)}
            {boundary && elements.filter((element) => element.role !== 'sector').map(renderElement)}
            {renderAlignmentGuides()}
            {(() => {
              const rotationElements = elements.filter((element) => selectedIds.includes(element.id) && element.role === 'bed')
              const rotationAnchor = rotationElements.find((element) => element.shape === 'rectangle')
              return rotationAnchor ? renderRotationGizmo(rotationAnchor, rotationElements) : null
            })()}
            {interaction?.kind === 'create' && renderElement(elementFromPoints(interaction.shape, interaction.role, interaction.start, interaction.current, initial, 'preview', interaction.role === 'sector' ? 'Novo setor' : 'Pré-visualização'))}
            {drawingOutline && <g><polyline points={(draftClosed && boundaryDraft.length > 2 ? [...boundaryDraft, boundaryDraft[0]] : boundaryDraft).map((point) => { const screen = worldToScreen(point, viewport, initial, zoom, pan); return screen.x + ',' + screen.y }).join(' ')} fill="none" stroke="#a56628" strokeWidth="3" />{boundaryDraft.map((start, index, points) => {
              if (!draftClosed && index === points.length - 1 || points.length < 2) return null
              const end = points[(index + 1) % points.length]
              const a = worldToScreen(start, viewport, initial, zoom, pan), b = worldToScreen(end, viewport, initial, zoom, pan)
              return <line key={'edge-' + index} x1={a.x} y1={a.y} x2={b.x} y2={b.y} stroke="transparent" strokeWidth="18" pointerEvents="stroke" className="cursor-grab hover:stroke-[#a5662830] focus-visible:stroke-[#a5662850]" role="button" tabIndex={0} aria-label={'Mover aresta ' + (index + 1)} onPointerDown={(event) => { if (event.button !== 0 || event.altKey || spacePressed) return; event.preventDefault(); event.stopPropagation(); event.currentTarget.ownerSVGElement?.setPointerCapture(event.pointerId); setInteraction({ kind: 'outline-edge', index, pointerX: event.clientX, pointerY: event.clientY, startPoints: boundaryDraft.map((point) => ({ ...point })) }) }} onKeyDown={(event) => {
                const offset = event.key === 'ArrowUp' ? { x: 0, y: -0.5 } : event.key === 'ArrowDown' ? { x: 0, y: 0.5 } : event.key === 'ArrowLeft' ? { x: -0.5, y: 0 } : event.key === 'ArrowRight' ? { x: 0.5, y: 0 } : undefined
                if (!offset) return
                event.preventDefault(); event.stopPropagation()
                const length = Math.hypot(end.x - start.x, end.y - start.y)
                if (length < 1e-9) return
                const nx = -(end.y - start.y) / length, ny = (end.x - start.x) / length, distance = offset.x * nx + offset.y * ny
                const dx = nx * distance, dy = ny * distance
                if (Math.min(start.x, end.x) + dx < 0 || Math.min(start.y, end.y) + dy < 0) return
                setBoundaryDraft((current) => current.map((point, i) => i === index || i === (index + 1) % points.length ? { x: point.x + dx, y: point.y + dy } : point))
              }} />
            })}{boundaryDraft.map((point, index) => { const screen = worldToScreen(point, viewport, initial, zoom, pan); return <circle key={index} cx={screen.x} cy={screen.y} r={index === 0 && boundaryDraft.length >= 3 ? 10 : 7} fill={index === 0 && boundaryDraft.length >= 3 ? "#1c5141" : "#a56628"} role="button" tabIndex={0} aria-label={index === 0 && boundaryDraft.length >= 3 ? (tool === "sector-outline" ? "Fechar setor no ponto inicial" : "Fechar terreno no ponto inicial") : "Mover vértice " + (index + 1)} onKeyDown={(event) => { if (event.key === "Delete" || event.key === "Backspace") { event.preventDefault(); event.stopPropagation(); removeTerrainNode(index); return } if (index === 0 && boundaryDraft.length >= 3 && (event.key === "Enter" || event.key === " ")) { event.preventDefault(); event.stopPropagation(); if (tool === 'sector-outline') finishSectorOutline(); else finishTerrain() } }} onContextMenu={(event) => { event.preventDefault(); event.stopPropagation() }} onPointerDown={(event) => { event.stopPropagation(); if (event.button !== 0) return; event.currentTarget.ownerSVGElement?.setPointerCapture(event.pointerId); setInteraction({ kind: "terrain-vertex", index, pointerX: event.clientX, pointerY: event.clientY, closeOnClick: index === 0 && boundaryDraft.length >= 3 }) }} /> })}</g>}
            <text x={terrain.x + 6} y={terrain.y + initial.height * scale - 8} className="interactive-svg-label">Origem · 0 u</text>
          </svg>}
        </div>
        {(workflowNotice || conflicts.size > 0) && <div className="interactive-canvas-notices">
          {conflicts.size > 0 && <p className="interactive-canvas-notice error" role="alert">Há canteiros sobrepostos. Ajuste as posições antes de salvar.</p>}
          {workflowNotice && <p className="interactive-canvas-notice" role="status" aria-live="polite"><span>{workflowNotice}</span><button type="button" className="interactive-icon-button" aria-label="Dispensar aviso" onClick={() => setWorkflowNotice(undefined)}><X aria-hidden="true" /></button></p>}
        </div>}
        </div>
        <div className="interactive-canvas-footer"><span aria-live="polite">{toolHint}</span><span className="interactive-scale" aria-label="Escala do desenho"><span>0 u</span><span className="interactive-scale-line" style={{ width: Math.max(48, scale * 5) }} /><span>5 u</span></span></div>
        </div>
        {bedMenuElement?.role === 'bed' ? <InteractiveBedMenu key={bedMenuElement.id} garden={route.garden} element={bedMenuElement} editing={setBedFormOpen} needsMapSave={dirty || unsaved} saving={saving.busy} uncertain={saving.uncertain} saveError={saving.error} saveBlocked={!organization || saving.error?.status === 409 || drawingOutline && boundaryDraft.length > 0} saveMap={saveConfiguration} close={() => setBedMenuId(undefined)} editShape={() => { setSelectedId(bedMenuElement.id); setSelectedIds([bedMenuElement.id]); setBedMenuId(undefined); setPropertiesOpen(true) }} /> : <DrawingMenu steps={<div inert={saving.busy || saving.uncertain}>{stepsNav}</div>} title={stageInfo[editorStage].title} description={stageInfo[editorStage].description}>{drawingTools}</DrawingMenu>}
        </div>
      </section>


    </div>
  </div>
}
