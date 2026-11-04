import { Temporal } from '@js-temporal/polyfill'
import { FormFailure, localInstant, utcInstant } from '../features/irrigation/formValues'
export const optionalText = (value: string) => value.trim() || null
export function numeric(value: string, field: string, required = false, scale = 3, signed = false): number | null {
  const text = value.trim()
  if (!text) { if (required) throw new FormFailure({ [field]: ['Informe o valor; vazio não significa zero.'] }); return null }
  const pattern = new RegExp(`^${signed ? '-?' : ''}\\d{1,${14 - scale}}${scale ? `([.,]\\d{1,${scale}})?` : ''}$`)
  if (!pattern.test(text)) throw new FormFailure({ [field]: [`Informe um número ${signed ? '' : 'não negativo '}com até ${scale} casas decimais, sem arredondamento.`] })
  return Number(text.replace(',', '.'))
}
export function requiredNumber(value: string, field: string, scale = 3, signed = false): number { const result = numeric(value, field, true, scale, signed); if (result === null) throw new FormFailure({ [field]: ['Informe o valor.'] }); return result }
export function instant(value: string, field: string, original?: string | null): string {
  try { if (original && Temporal.PlainDateTime.from(value).equals(Temporal.PlainDateTime.from(localInstant(original)))) return original; return utcInstant(value) }
  catch { throw new FormFailure({ [field]: ['Informe data e hora válidas no fuso de São Paulo.'] }) }
}
export function numberDraft(value: number | null | undefined) { return value === null || value === undefined ? '' : String(value).replace('.', ',') }
