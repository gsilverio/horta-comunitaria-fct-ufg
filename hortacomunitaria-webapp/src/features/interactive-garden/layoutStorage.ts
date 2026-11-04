import type { DemoElement, DemoGarden, MandalaPlacement } from './types'
import { validBoundary } from './geometry'

const STORAGE_PREFIX = 'horta-interativa-layout:v3:'

type UnknownRecord = Record<string, unknown>

function isRecord(value: unknown): value is UnknownRecord {
  return typeof value === 'object' && value !== null
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value)
}

function isMandala(value: unknown): value is MandalaPlacement {
  if (!isRecord(value)) return false
  return (value.bedStyle === 'curved' || value.bedStyle === 'rectangle')
    && isFiniteNumber(value.centerX)
    && isFiniteNumber(value.centerY)
    && isFiniteNumber(value.ringRadius)
    && isFiniteNumber(value.angleDeg)
    && isFiniteNumber(value.angularWidthDeg)
    && isFiniteNumber(value.radialLength)
}

function isElement(value: unknown): value is DemoElement {
  if (!isRecord(value) || typeof value.id !== 'string' || typeof value.name !== 'string') return false
  if (value.role !== 'bed' && value.role !== 'sector' && value.role !== 'reference') return false
  if (value.shape === 'circle') {
    return isFiniteNumber(value.centerX) && isFiniteNumber(value.centerY) && isFiniteNumber(value.radius)
  }
  if (value.shape !== 'rectangle') return false
  return isFiniteNumber(value.x)
    && isFiniteNumber(value.y)
    && isFiniteNumber(value.width)
    && isFiniteNumber(value.height)
    && isFiniteNumber(value.rotationDeg)
    && (value.mandala === undefined || isMandala(value.mandala))
    && (value.outline === undefined || Array.isArray(value.outline) && value.outline.length >= 3 && value.outline.every((point) => isRecord(point) && isFiniteNumber(point.x) && isFiniteNumber(point.y)))
}

function storageKey(gardenId: string) {
  return `${STORAGE_PREFIX}${gardenId}`
}

/**
 * Preserva o rascunho da aba sem substituir a configuração confirmada na API.
 */
export function readInteractiveDraft(gardenId: string, fallback: DemoGarden): DemoGarden {
  if (typeof window === 'undefined' || !gardenId) return fallback
  try {
    const raw = window.sessionStorage.getItem(storageKey(gardenId))
    if (!raw) return fallback
    const parsed: unknown = JSON.parse(raw)
    if (!isRecord(parsed) || !isFiniteNumber(parsed.width) || !isFiniteNumber(parsed.height) || !Array.isArray(parsed.elements)) return fallback
    const elements = parsed.elements.filter(isElement).map((element) => {
      // Rascunhos anteriores associavam o setor a ele mesmo e colocavam o
      // centro da mandala dentro do agrupamento de canteiros. Esses vínculos
      // não representam a hierarquia do editor e são corrigidos na leitura.
      if (element.role === 'sector' || element.role === 'reference') {
        return { ...element, sectorId: undefined, sectorName: undefined }
      }
      return element.shape === 'rectangle' && element.mandala
        ? { ...element, name: element.name.replace(/· Setor (\d+)/g, '· Canteiro $1') }
        : element
    })
    if (elements.length !== parsed.elements.length || parsed.width <= 0 || parsed.height <= 0) return fallback
    if (fallback.projectionVersion !== undefined && parsed.projectionVersion !== fallback.projectionVersion) return fallback
    if (fallback.projectionKey !== undefined && parsed.projectionKey !== fallback.projectionKey) return fallback
    // O contorno escolhido pelo usuário define o enquadramento do rascunho.
    const boundary = Array.isArray(parsed.boundary) && parsed.boundary.every((point) => isRecord(point) && isFiniteNumber(point.x) && isFiniteNumber(point.y))
      ? parsed.boundary.map((point) => ({ x: Number(point.x), y: Number(point.y) })) : undefined
    return { ...fallback, width: parsed.width, height: parsed.height, elements, boundary: boundary && validBoundary(boundary) ? boundary : undefined }
  } catch {
    return fallback
  }
}

export function writeInteractiveDraft(gardenId: string, garden: DemoGarden) {
  if (typeof window === 'undefined' || !gardenId) return
  try {
    window.sessionStorage.setItem(storageKey(gardenId), JSON.stringify(garden))
  } catch {
    // A full or disabled session storage must not interrupt the editor.
  }
}

