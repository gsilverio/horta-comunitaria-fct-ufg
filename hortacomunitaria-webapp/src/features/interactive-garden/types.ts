export type ElementShape = 'rectangle' | 'circle'
export type ElementRole = 'bed' | 'sector' | 'reference'
export type MandalaBedStyle = 'curved' | 'rectangle'
export type ElementSource = 'catalog' | 'draft' | 'demo'

interface LayoutElementBase {
  id: string
  name: string
  role: ElementRole
  /** Identificador do cadastro quando o elemento veio do CRUD. */
  entityId?: string
  /** Setor vigente do canteiro na data de projeção. */
  sectorId?: string
  sectorName?: string
  source?: ElementSource
  /** Indica que a dimensão foi estimada porque o cadastro não informa comprimento e largura. */
  isEstimated?: boolean
}

export interface RectangleElement extends LayoutElementBase {
  /** Contorno livre do setor; x/y/width/height guardam seu envelope. */
  outline?: { x: number; y: number }[]
  shape: 'rectangle'
  x: number
  y: number
  width: number
  height: number
  rotationDeg: number
  mandala?: MandalaPlacement
}

export interface MandalaPlacement {
  bedStyle: MandalaBedStyle
  centerX: number
  centerY: number
  ringRadius: number
  angleDeg: number
  angularWidthDeg: number
  radialLength: number
}

export interface CircleElement extends LayoutElementBase {
  shape: 'circle'
  centerX: number
  centerY: number
  radius: number
}

export type DemoElement = RectangleElement | CircleElement

export interface DemoSector {
  id: string
  name: string
  areaM2: number
  x: number
  y: number
  width: number
  height: number
  bedIds: string[]
  /** Os cadastros atuais não têm geometria de setor; este envelope é calculado pelos canteiros. */
  isEstimated: true
}

export interface DemoGarden {
  /** Contorno visual em unidades livres, sem equivalência com medidas reais. */
  boundary?: { x: number; y: number }[]
  width: number
  height: number
  elements: DemoElement[]
  areaM2?: number
  areaSource?: 'registered' | 'fallback'
  projectionVersion?: number
  /** Assinatura dos dados usados para gerar a projeção; invalida rascunhos quando o CRUD muda. */
  projectionKey?: string
  sectors?: DemoSector[]
}

export interface Pan {
  x: number
  y: number
}

export const DEMO_GARDEN: DemoGarden = {
  width: 20,
  height: 20,
  elements: [
    {
      id: 'demo-bed-1',
      name: 'Canteiro demonstrativo',
      role: 'bed',
      source: 'demo',
      shape: 'rectangle',
      x: 7.5,
      y: 7.5,
      width: 1,
      height: 5,
      rotationDeg: 0,
    },
  ],
}
