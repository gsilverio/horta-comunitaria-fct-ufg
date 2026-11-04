import { DEMO_GARDEN } from './types'
import type { DemoElement, DemoGarden, MandalaBedStyle, MandalaPlacement, Pan, RectangleElement } from './types'

export const VIEW_PADDING = 32
export const MIN_ZOOM = 0.65
export const MAX_ZOOM = 3
export const SNAP_STEP = 0.5
export const ALIGNMENT_THRESHOLD = 0.3
const GEOMETRY_EPSILON = 1e-9
export const MANDALA_BED_COUNT = 3
export const MANDALA_RING_COUNT = 1

export interface MandalaConfig {
  centerX: number
  centerY: number
  radius: number
  divisions: number
  rings: number
  innerRadius: number
  orientationDeg: number
  bedStyle: MandalaBedStyle
}

export const DEFAULT_MANDALA_CONFIG: MandalaConfig = {
  centerX: 10,
  centerY: 10,
  radius: 7,
  divisions: MANDALA_BED_COUNT,
  rings: MANDALA_RING_COUNT,
  innerRadius: 2,
  orientationDeg: 0,
  bedStyle: 'curved',
}

interface Point {
  x: number
  y: number
}

export function createDemoGarden(): DemoGarden {
  return {
    width: DEMO_GARDEN.width,
    height: DEMO_GARDEN.height,
    elements: DEMO_GARDEN.elements.map((element) => ({ ...element })),
  }
}

export function cloneElements(elements: DemoElement[]) {
  return elements.map((element) => ({ ...element }))
}

export function sameElements(left: DemoElement[], right: DemoElement[]) {
  return JSON.stringify(left) === JSON.stringify(right)
}

export function clampZoom(value: number) {
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, value))
}

export function baseScale(width: number, height: number, garden: DemoGarden) {
  const drawableWidth = Math.max(0, width - VIEW_PADDING * 2)
  const drawableHeight = Math.max(0, height - VIEW_PADDING * 2)
  return Math.min(drawableWidth / garden.width, drawableHeight / garden.height)
}

export function gardenOrigin(width: number, height: number, garden: DemoGarden) {
  const scale = baseScale(width, height, garden)
  return {
    x: (width - garden.width * scale) / 2,
    y: (height - garden.height * scale) / 2,
  }
}

export function worldToScreen(
  point: { x: number; y: number },
  viewport: { width: number; height: number },
  garden: DemoGarden,
  zoom: number,
  pan: Pan,
) {
  const scale = baseScale(viewport.width, viewport.height, garden) * zoom
  const origin = gardenOrigin(viewport.width, viewport.height, garden)
  return { x: origin.x + point.x * scale + pan.x, y: origin.y + point.y * scale + pan.y }
}

export function screenToWorld(
  point: { x: number; y: number },
  viewport: { width: number; height: number },
  garden: DemoGarden,
  zoom: number,
  pan: Pan,
) {
  const scale = baseScale(viewport.width, viewport.height, garden) * zoom
  const origin = gardenOrigin(viewport.width, viewport.height, garden)
  return {
    x: (point.x - origin.x - pan.x) / scale,
    y: (point.y - origin.y - pan.y) / scale,
  }
}

function mandalaPolygon(rectangle: RectangleElement, samples = 24): Point[] {
  const placement = rectangle.mandala
  if (!placement) return []
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

function rectangleCorners(rectangle: RectangleElement): Point[] {
  if (rectangle.outline) return rectangle.outline
  if (rectangle.mandala?.bedStyle === 'curved') return mandalaPolygon(rectangle)
  const centerX = rectangle.x + rectangle.width / 2
  const centerY = rectangle.y + rectangle.height / 2
  const angle = rectangle.rotationDeg * Math.PI / 180
  const cos = Math.cos(angle)
  const sin = Math.sin(angle)
  return [
    { x: rectangle.x, y: rectangle.y },
    { x: rectangle.x + rectangle.width, y: rectangle.y },
    { x: rectangle.x + rectangle.width, y: rectangle.y + rectangle.height },
    { x: rectangle.x, y: rectangle.y + rectangle.height },
  ].map((point) => ({
    x: centerX + (point.x - centerX) * cos - (point.y - centerY) * sin,
    y: centerY + (point.x - centerX) * sin + (point.y - centerY) * cos,
  }))
}

function rectangleAxes(rectangle: RectangleElement): Point[] {
  const angle = rectangle.rotationDeg * Math.PI / 180
  const cos = Math.cos(angle)
  const sin = Math.sin(angle)
  return [{ x: cos, y: sin }, { x: -sin, y: cos }]
}

function project(points: Point[], axis: Point) {
  const values = points.map((point) => point.x * axis.x + point.y * axis.y)
  return { min: Math.min(...values), max: Math.max(...values) }
}

function intervalsOverlap(left: { min: number; max: number }, right: { min: number; max: number }) {
  return left.min < right.max - GEOMETRY_EPSILON && right.min < left.max - GEOMETRY_EPSILON
}

export function pointInPolygon(point: Point, polygon: Point[]) {
  let inside = false
  for (let index = 0, previous = polygon.length - 1; index < polygon.length; previous = index, index += 1) {
    const currentPoint = polygon[index]
    const previousPoint = polygon[previous]
    const crossesRay = currentPoint.y > point.y !== previousPoint.y > point.y
    if (crossesRay && point.x < (previousPoint.x - currentPoint.x) * (point.y - currentPoint.y) / (previousPoint.y - currentPoint.y) + currentPoint.x) inside = !inside
  }
  return inside
}

/** Rejeita contornos degenerados ou cruzados antes de concluir o desenho. */
export function validBoundary(points: Point[]) {
  if (points.length < 3 || points.some((p) => !Number.isFinite(p.x) || !Number.isFinite(p.y))) return false
  const area = points.reduce((sum, p, i) => sum + p.x * points[(i + 1) % points.length].y - points[(i + 1) % points.length].x * p.y, 0)
  if (Math.abs(area) < 0.01) return false
  for (let i = 0; i < points.length; i++) {
    for (let j = i + 1; j < points.length; j++) {
      if (Math.hypot(points[i].x - points[j].x, points[i].y - points[j].y) < 0.001) return false
      if (j === i + 1 || i === 0 && j === points.length - 1) continue
      if (segmentsIntersect(points[i], points[(i + 1) % points.length], points[j], points[(j + 1) % points.length])) return false
    }
  }
  return true
}

export function boundaryContainsElement(boundary: Point[], element: DemoElement) {
  const points = element.shape === 'rectangle' ? rectangleCorners(element) : Array.from({ length: 96 }, (_, i) => ({
    x: element.centerX + element.radius * Math.cos(i * Math.PI / 48),
    y: element.centerY + element.radius * Math.sin(i * Math.PI / 48),
  }))
  const onEdge = (point: Point) => boundary.some((start, i) => distanceToSegmentSquared(point, start, boundary[(i + 1) % boundary.length]) < 1e-12)
  return points.every((point) => pointInPolygon(point, boundary) || onEdge(point))
    && !points.some((start, i) => boundary.some((edge, j) => segmentsIntersect(start, points[(i + 1) % points.length], edge, boundary[(j + 1) % boundary.length])))
}

function cross(left: Point, right: Point, point: Point) {
  return (right.x - left.x) * (point.y - left.y) - (right.y - left.y) * (point.x - left.x)
}

function segmentsIntersect(leftStart: Point, leftEnd: Point, rightStart: Point, rightEnd: Point) {
  const leftA = cross(leftStart, leftEnd, rightStart)
  const leftB = cross(leftStart, leftEnd, rightEnd)
  const rightA = cross(rightStart, rightEnd, leftStart)
  const rightB = cross(rightStart, rightEnd, leftEnd)
  return (leftA > GEOMETRY_EPSILON && leftB < -GEOMETRY_EPSILON || leftA < -GEOMETRY_EPSILON && leftB > GEOMETRY_EPSILON)
    && (rightA > GEOMETRY_EPSILON && rightB < -GEOMETRY_EPSILON || rightA < -GEOMETRY_EPSILON && rightB > GEOMETRY_EPSILON)
}

function polygonsOverlap(left: Point[], right: Point[]) {
  for (let leftIndex = 0; leftIndex < left.length; leftIndex += 1) {
    const leftStart = left[leftIndex]
    const leftEnd = left[(leftIndex + 1) % left.length]
    for (let rightIndex = 0; rightIndex < right.length; rightIndex += 1) {
      if (segmentsIntersect(leftStart, leftEnd, right[rightIndex], right[(rightIndex + 1) % right.length])) return true
    }
  }
  return pointInPolygon(left[0], right) || pointInPolygon(right[0], left)
}

function distanceToSegmentSquared(point: Point, start: Point, end: Point) {
  const deltaX = end.x - start.x
  const deltaY = end.y - start.y
  const lengthSquared = deltaX * deltaX + deltaY * deltaY
  const projection = lengthSquared === 0 ? 0 : Math.max(0, Math.min(1, ((point.x - start.x) * deltaX + (point.y - start.y) * deltaY) / lengthSquared))
  const nearest = { x: start.x + projection * deltaX, y: start.y + projection * deltaY }
  return (point.x - nearest.x) ** 2 + (point.y - nearest.y) ** 2
}

function polygonCircleOverlap(polygon: Point[], circle: Extract<DemoElement, { shape: 'circle' }>) {
  const center = { x: circle.centerX, y: circle.centerY }
  if (pointInPolygon(center, polygon)) return true
  const radiusSquared = circle.radius ** 2 - GEOMETRY_EPSILON
  for (let index = 0; index < polygon.length; index += 1) {
    if (distanceToSegmentSquared(center, polygon[index], polygon[(index + 1) % polygon.length]) < radiusSquared) return true
  }
  return false
}

function rectanglesOverlap(left: RectangleElement, right: RectangleElement) {
  if (left.outline || right.outline || left.mandala?.bedStyle === 'curved' || right.mandala?.bedStyle === 'curved') return polygonsOverlap(rectangleCorners(left), rectangleCorners(right))
  const leftCorners = rectangleCorners(left)
  const rightCorners = rectangleCorners(right)
  const axes = [...rectangleAxes(left), ...rectangleAxes(right)]
  return axes.every((axis) => intervalsOverlap(project(leftCorners, axis), project(rightCorners, axis)))
}

function rectangleCircleOverlap(rectangle: RectangleElement, circle: Extract<DemoElement, { shape: 'circle' }>) {
  if (rectangle.outline || rectangle.mandala?.bedStyle === 'curved') return polygonCircleOverlap(rectangleCorners(rectangle), circle)
  const centerX = rectangle.x + rectangle.width / 2
  const centerY = rectangle.y + rectangle.height / 2
  const angle = rectangle.rotationDeg * Math.PI / 180
  const cos = Math.cos(angle)
  const sin = Math.sin(angle)
  const deltaX = circle.centerX - centerX
  const deltaY = circle.centerY - centerY
  const localX = deltaX * cos + deltaY * sin
  const localY = -deltaX * sin + deltaY * cos
  const nearestX = Math.max(-rectangle.width / 2, Math.min(rectangle.width / 2, localX))
  const nearestY = Math.max(-rectangle.height / 2, Math.min(rectangle.height / 2, localY))
  const distanceX = localX - nearestX
  const distanceY = localY - nearestY
  return distanceX * distanceX + distanceY * distanceY < circle.radius * circle.radius - GEOMETRY_EPSILON
}

function circlesOverlap(left: Extract<DemoElement, { shape: 'circle' }>, right: Extract<DemoElement, { shape: 'circle' }>) {
  const distanceX = left.centerX - right.centerX
  const distanceY = left.centerY - right.centerY
  const radius = left.radius + right.radius
  return distanceX * distanceX + distanceY * distanceY < radius * radius - GEOMETRY_EPSILON
}

export function elementBounds(element: DemoElement) {
  if (element.shape === 'circle') {
    return { minX: element.centerX - element.radius, minY: element.centerY - element.radius, maxX: element.centerX + element.radius, maxY: element.centerY + element.radius }
  }
  const corners = rectangleCorners(element)
  return {
    minX: Math.min(...corners.map((point) => point.x)),
    minY: Math.min(...corners.map((point) => point.y)),
    maxX: Math.max(...corners.map((point) => point.x)),
    maxY: Math.max(...corners.map((point) => point.y)),
  }
}

export function clampElement(element: DemoElement, garden: DemoGarden): DemoElement {
  if (element.shape === 'circle') {
    const radius = Math.max(0.2, Math.min(element.radius, Math.min(garden.width, garden.height) / 2))
    return { ...element, radius, centerX: Math.min(garden.width - radius, Math.max(radius, element.centerX)), centerY: Math.min(garden.height - radius, Math.max(radius, element.centerY)) }
  }
  const width = Math.max(0.2, Math.min(element.width, garden.width))
  const height = Math.max(0.2, Math.min(element.height, garden.height))
  const rotationDeg = normalizeAngle(element.rotationDeg)
  const angle = rotationDeg * Math.PI / 180
  const halfExtentX = Math.abs(Math.cos(angle) * width / 2) + Math.abs(Math.sin(angle) * height / 2)
  const halfExtentY = Math.abs(Math.sin(angle) * width / 2) + Math.abs(Math.cos(angle) * height / 2)
  const centerX = Math.min(garden.width - halfExtentX, Math.max(halfExtentX, element.x + width / 2))
  const centerY = Math.min(garden.height - halfExtentY, Math.max(halfExtentY, element.y + height / 2))
  return { ...element, width, height, rotationDeg, x: centerX - width / 2, y: centerY - height / 2 }
}

export function normalizeAngle(value: number) {
  const normalized = value % 360
  return normalized > 180 ? normalized - 360 : normalized <= -180 ? normalized + 360 : normalized
}

export function snapRightAngle(value: number, threshold = 7) {
  const normalized = normalizeAngle(value)
  const rightAngle = normalizeAngle(Math.round(normalized / 90) * 90)
  return Math.abs(normalizeAngle(normalized - rightAngle)) <= threshold ? rightAngle : normalized
}

function alignmentAnchors(element: DemoElement) {
  const bounds = elementBounds(element)
  return {
    x: [bounds.minX, (bounds.minX + bounds.maxX) / 2, bounds.maxX],
    y: [bounds.minY, (bounds.minY + bounds.maxY) / 2, bounds.maxY],
  }
}

function translateElement(element: DemoElement, deltaX: number, deltaY: number): DemoElement {
  if (element.shape === 'rectangle') {
    return element.mandala
      ? { ...element, x: element.x + deltaX, y: element.y + deltaY, mandala: { ...element.mandala, centerX: element.mandala.centerX + deltaX, centerY: element.mandala.centerY + deltaY } }
      : { ...element, x: element.x + deltaX, y: element.y + deltaY, outline: element.outline?.map((point) => ({ x: point.x + deltaX, y: point.y + deltaY })) }
  }
  return { ...element, centerX: element.centerX + deltaX, centerY: element.centerY + deltaY }
}

export function alignElement(element: DemoElement, others: DemoElement[], threshold = ALIGNMENT_THRESHOLD) {
  const current = alignmentAnchors(element)
  let bestX: { offset: number; guide: number } | undefined
  let bestY: { offset: number; guide: number } | undefined
  for (const other of others) {
    if (other.id === element.id) continue
    const target = alignmentAnchors(other)
    for (const currentAnchor of current.x) {
      for (const targetAnchor of target.x) {
        const offset = targetAnchor - currentAnchor
        if (Math.abs(offset) <= threshold && (!bestX || Math.abs(offset) < Math.abs(bestX.offset))) bestX = { offset, guide: targetAnchor }
      }
    }
    for (const currentAnchor of current.y) {
      for (const targetAnchor of target.y) {
        const offset = targetAnchor - currentAnchor
        if (Math.abs(offset) <= threshold && (!bestY || Math.abs(offset) < Math.abs(bestY.offset))) bestY = { offset, guide: targetAnchor }
      }
    }
  }
  return {
    element: translateElement(element, bestX?.offset ?? 0, bestY?.offset ?? 0),
    guides: { x: bestX?.guide, y: bestY?.guide },
  }
}

export function elementArea(element: DemoElement) {
  if (element.shape === 'rectangle' && element.outline) return Math.abs(element.outline.reduce((sum, p, i, points) => sum + p.x * points[(i + 1) % points.length].y - points[(i + 1) % points.length].x * p.y, 0)) / 2
  if (element.shape === 'circle') return Math.PI * element.radius ** 2
  if (element.mandala?.bedStyle === 'curved') {
    const innerRadius = Math.max(0, element.mandala.ringRadius - element.mandala.radialLength / 2)
    const outerRadius = element.mandala.ringRadius + element.mandala.radialLength / 2
    return 0.5 * (outerRadius ** 2 - innerRadius ** 2) * element.mandala.angularWidthDeg * Math.PI / 180
  }
  return element.width * element.height
}

export function elementsOverlap(left: DemoElement, right: DemoElement) {
  if (left.shape === 'circle' && right.shape === 'circle') return circlesOverlap(left, right)
  if (left.shape === 'rectangle' && right.shape === 'rectangle') return rectanglesOverlap(left, right)
  if (left.shape === 'rectangle' && right.shape === 'circle') return rectangleCircleOverlap(left, right)
  if (left.shape === 'circle' && right.shape === 'rectangle') return rectangleCircleOverlap(right, left)
  return false
}

export function conflictIds(elements: DemoElement[]) {
  const ids = new Set<string>()
  for (let index = 0; index < elements.length; index += 1) {
    for (let other = index + 1; other < elements.length; other += 1) {
      if (elementsOverlap(elements[index], elements[other])) {
        ids.add(elements[index].id)
        ids.add(elements[other].id)
      }
    }
  }
  return ids
}

export function snap(value: number, step = SNAP_STEP) {
  return Math.round(value / step) * step
}

export function validateMandalaConfig(config: MandalaConfig, garden: DemoGarden) {
  const values = [config.centerX, config.centerY, config.radius, config.divisions, config.rings, config.innerRadius, config.orientationDeg]
  if (values.some((value) => !Number.isFinite(value))) return 'Preencha todos os parâmetros da mandala.'
  if (config.bedStyle !== 'curved' && config.bedStyle !== 'rectangle') return 'Escolha o formato dos canteiros da mandala.'
  if (!Number.isInteger(config.divisions) || config.divisions < 3 || config.divisions > 16) return 'Use entre 3 e 16 divisões radiais.'
  if (!Number.isInteger(config.rings) || config.rings < 1 || config.rings > 4) return 'Use entre 1 e 4 anéis.'
  if (config.bedStyle === 'curved' && (config.divisions !== MANDALA_BED_COUNT || config.rings !== MANDALA_RING_COUNT)) return 'O padrão curvilíneo usa 3 canteiros em um anel.'
  if (config.centerX < 0 || config.centerX > garden.width || config.centerY < 0 || config.centerY > garden.height) return 'O centro precisa ficar dentro do terreno.'
  const maximumRadius = Math.min(config.centerX, garden.width - config.centerX, config.centerY, garden.height - config.centerY) - 0.4
  if (config.radius <= 0.8 || config.radius > maximumRadius) return `O raio total precisa ficar entre 0,8 u e ${Math.max(0.8, maximumRadius).toFixed(1)} u para este centro.`
  if (config.innerRadius < 0 || config.innerRadius >= config.radius - 0.8) return 'O raio central precisa ser menor que o raio total.'
  if ((config.radius - config.innerRadius) / (config.rings + 1) < 0.55) return 'A distância entre os anéis ficou pequena demais.'
  return undefined
}

export function updateMandalaPlacement(element: RectangleElement, placement: MandalaPlacement, garden: DemoGarden, priority?: 'ringRadius' | 'radialLength'): RectangleElement {
  const angularWidthDeg = Math.max(2, Math.min(120, placement.angularWidthDeg))
  const requestedCenterX = Math.max(0, Math.min(garden.width, placement.centerX))
  const requestedCenterY = Math.max(0, Math.min(garden.height, placement.centerY))
  const edgeDistance = Math.min(requestedCenterX, garden.width - requestedCenterX, requestedCenterY, garden.height - requestedCenterY)
  const requestedRingRadius = Math.max(0.4, placement.ringRadius)
  const requestedRadialLength = Math.max(0.2, placement.radialLength)
  let ringRadius: number
  let radialLength: number
  if (priority === 'radialLength') {
    radialLength = Math.min(requestedRadialLength, Math.max(0.2, 2 * (edgeDistance - requestedRingRadius)))
    ringRadius = Math.max(0.4, Math.min(Math.max(0.4, edgeDistance - radialLength / 2), requestedRingRadius))
  } else {
    ringRadius = Math.max(0.4, Math.min(Math.max(0.4, edgeDistance - requestedRadialLength / 2), requestedRingRadius))
    radialLength = Math.min(requestedRadialLength, Math.max(0.2, 2 * (edgeDistance - ringRadius)))
  }
  const angleDeg = normalizeAngle(placement.angleDeg)
  const angle = angleDeg * Math.PI / 180
  const tangentialLength = Math.max(0.2, 2 * Math.PI * ringRadius * angularWidthDeg / 360)
  const boundaryRadius = ringRadius + radialLength / 2
  const mandalaCenterX = Math.min(garden.width - boundaryRadius, Math.max(boundaryRadius, requestedCenterX))
  const mandalaCenterY = Math.min(garden.height - boundaryRadius, Math.max(boundaryRadius, requestedCenterY))
  const centerX = mandalaCenterX + Math.cos(angle) * ringRadius
  const centerY = mandalaCenterY + Math.sin(angle) * ringRadius
  return clampElement({
    ...element,
    x: centerX - tangentialLength / 2,
    y: centerY - radialLength / 2,
    width: tangentialLength,
    height: radialLength,
    rotationDeg: normalizeAngle(angleDeg - 90),
    mandala: { ...placement, centerX: mandalaCenterX, centerY: mandalaCenterY, ringRadius, angularWidthDeg, radialLength, angleDeg },
  }, garden) as RectangleElement
}

export function generateMandalaElements(config: MandalaConfig, garden: DemoGarden): DemoElement[] {
  if (validateMandalaConfig(config, garden)) return []
  const divisions = config.bedStyle === 'curved' ? MANDALA_BED_COUNT : config.divisions
  const rings = config.bedStyle === 'curved' ? MANDALA_RING_COUNT : config.rings
  const ringSpacing = (config.radius - config.innerRadius) / (rings + 1)
  const elements: DemoElement[] = []
  if (config.innerRadius >= 0.4) {
    elements.push(clampElement({
      id: 'mandala-center',
      name: 'Mandala · Centro',
      // O centro da mandala é uma referência geométrica; setor é um cadastro
      // da horta e nunca deve ser criado implicitamente pelo preset visual.
      role: 'reference',
      shape: 'circle',
      centerX: config.centerX,
      centerY: config.centerY,
      radius: config.innerRadius,
    }, garden))
  }
  for (let ringIndex = 0; ringIndex < rings; ringIndex += 1) {
    const ringRadius = config.innerRadius + ringSpacing * (ringIndex + 1)
    const radialLength = config.bedStyle === 'curved'
      ? Math.max(0.9, Math.min(2.2, ringSpacing * 0.68))
      : Math.max(0.8, Math.min(2.4, ringSpacing * 0.72))
    for (let divisionIndex = 0; divisionIndex < divisions; divisionIndex += 1) {
      const angle = config.orientationDeg + divisionIndex * (360 / divisions)
      const radians = angle * Math.PI / 180
      const angularWidthDeg = config.bedStyle === 'curved'
        ? Math.min(96, (360 / divisions) * 0.74)
        : undefined
      const tangentialLength = angularWidthDeg === undefined
        ? Math.max(0.65, Math.min(1.4, (2 * Math.PI * ringRadius / divisions) * 0.5))
        : Math.max(0.2, 2 * Math.PI * ringRadius * angularWidthDeg / 360)
      const centerX = config.centerX + Math.cos(radians) * ringRadius
      const centerY = config.centerY + Math.sin(radians) * ringRadius
      elements.push(updateMandalaPlacement({
        id: `mandala-ring-${ringIndex + 1}-sector-${divisionIndex + 1}`,
        name: `Mandala · Anel ${ringIndex + 1} · Canteiro ${divisionIndex + 1}`,
        role: 'bed',
        shape: 'rectangle',
        x: centerX - tangentialLength / 2,
        y: centerY - radialLength / 2,
        width: tangentialLength,
        height: radialLength,
        rotationDeg: normalizeAngle(angle - 90),
      }, {
        centerX: config.centerX,
        centerY: config.centerY,
        ringRadius,
        angleDeg: angle,
        angularWidthDeg: angularWidthDeg ?? tangentialLength / ringRadius * 180 / Math.PI,
        radialLength,
        bedStyle: config.bedStyle,
      }, garden))
    }
  }
  return elements
}
