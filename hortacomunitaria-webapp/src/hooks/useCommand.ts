import { useGardenState } from "./useGardenState"
import { useRef, useState } from 'react'
import { apiFailure } from '../services/http'
import { FormFailure } from '../features/irrigation/formValues'
import type { ApiFailure } from '../types/api'
export function useCommand<T extends { version: number }>(initial: T, load: () => Promise<T>, changed: () => void) {
  const garden = useGardenState()
  const [baseline, setBaseline] = useState(initial), [review, setReview] = useState<T>(), [error, setError] = useState<ApiFailure>(), [busy, setBusy] = useState(false), [notice, setNotice] = useState('')
  const sending = useRef(false)
  async function submit(run: (version: number) => Promise<unknown>) { if (sending.current || !garden.isActive || error?.status === 409) return; sending.current = true; setBusy(true); setError(undefined); try { await run(baseline.version); setNotice('Alteração confirmada. Recarregando o estado e o histórico.'); changed(); setBaseline(await load()) } catch (failure) { setError(failure instanceof FormFailure ? { message: failure.message, fields: failure.fields } : apiFailure(failure)) } finally { sending.current = false; setBusy(false) } }
  async function compare() { setBusy(true); try { setReview(await load()) } catch (failure) { setError(apiFailure(failure)) } finally { setBusy(false) } }
  function acknowledge() { if (review && !("validity" in review && review.validity === "Invalid") && !("isActive" in review && review.isActive === false)) { setBaseline(review); setReview(undefined); setError(undefined) } }
  return { submit, baseline, review, error, busy, notice, compare, acknowledge }
}
