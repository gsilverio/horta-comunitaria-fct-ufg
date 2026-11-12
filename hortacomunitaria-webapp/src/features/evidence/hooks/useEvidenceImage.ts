import { useEffect, useState } from 'react'
import { evidenceApi } from '../services/evidenceApi'

/**
 * Carrega a fotografia pelo cliente autenticado e expõe uma URL local (blob) para <img>; o endereço da API não
 * funciona em <img> porque o navegador não envia o access token. A URL local é liberada ao desmontar ou trocar.
 */
export function useEvidenceImage(contentPath: string) {
  const [image, setImage] = useState<{ path: string; url?: string; failed: boolean }>()
  useEffect(() => {
    const controller = new AbortController()
    let url: string | undefined
    evidenceApi.content(contentPath, controller.signal).then(blob => {
      if (controller.signal.aborted) return
      url = URL.createObjectURL(blob)
      setImage({ path: contentPath, url, failed: false })
    }, () => { if (!controller.signal.aborted) setImage({ path: contentPath, failed: true }) })
    return () => { controller.abort(); if (url) URL.revokeObjectURL(url) }
  }, [contentPath])
  return image?.path === contentPath ? image : { path: contentPath, url: undefined, failed: false }
}
