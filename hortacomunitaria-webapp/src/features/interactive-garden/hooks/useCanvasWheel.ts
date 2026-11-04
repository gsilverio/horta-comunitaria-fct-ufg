import { useEffect, useState } from 'react'

/** Roda comum rola a página; Ctrl/Command + roda amplia somente o mapa. */
export function useCanvasWheel(onZoom: (event: WheelEvent, canvas: SVGSVGElement) => void) {
  const [canvas, setCanvas] = useState<SVGSVGElement | null>(null)
  useEffect(() => {
    if (!canvas) return
    const wheel = (event: WheelEvent) => {
      if (!(event.ctrlKey || event.metaKey) || event.deltaY === 0) return
      event.preventDefault()
      onZoom(event, canvas)
    }
    canvas.addEventListener('wheel', wheel, { passive: false })
    return () => canvas.removeEventListener('wheel', wheel)
  }, [canvas, onZoom])
  return setCanvas
}

export function wheelZoomFactor(event: WheelEvent) {
  const delta = event.deltaY * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? 240 : 1)
  return Math.exp(-Math.max(-150, Math.min(150, delta)) * 0.002)
}
