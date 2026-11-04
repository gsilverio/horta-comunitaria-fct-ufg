import { useEffect, useState } from 'react'
import { apiFailure } from '../services/http'
import type { ApiFailure } from '../types/api'

/** Cada resposta pertence à chave de consulta; cleanup impede publicação de respostas antigas. */
export function useRemote<T>(key: string, load: (signal: AbortSignal) => Promise<T>) {
  const [result, setResult] = useState<{ key: string; data?: T; error?: ApiFailure }>()
  useEffect(() => {
    const controller = new AbortController()
    load(controller.signal).then(data => {
      if (!controller.signal.aborted) setResult({ key, data })
    }, error => {
      if (!controller.signal.aborted) setResult({ key, error: apiFailure(error) })
    })
    return () => controller.abort()
  }, [key, load])
  return result?.key === key ? { ...result, loading: false } : { data: undefined, error: undefined, loading: true }
}


