import { Temporal } from '@js-temporal/polyfill'
import type { IrrigationDetail, IrrigationValues, Status, VolumeSource } from './types/irrigation'

export const timeZone = 'America/Sao_Paulo'
export const today = () => Temporal.Now.plainDateISO(timeZone).toString()
export function localInstant(value: string): string {
  return Temporal.Instant.from(value).toZonedDateTimeISO(timeZone).toPlainDateTime().toString({ smallestUnit: 'millisecond' })
}
export function utcInstant(value: string): string {
  return Temporal.PlainDateTime.from(value).toZonedDateTime(timeZone, { disambiguation: 'reject' }).toInstant().toString()
}
export const formatNumber = (value: number) => new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 }).format(value)
export const formatVolume = (value: number | null) => value === null ? 'Não informado' : `${formatNumber(value)} L`
export const formatInstant = (value: string) => new Intl.DateTimeFormat('pt-BR', { timeZone, dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
export const statusLabel = (value: Status) => value === 'Completed' ? 'Concluída' : 'Interrompida'
export interface Draft {
  sectorId: string; bedIds: string[]; startedAt: string; endedAt: string
  activeDurationMinutes: string; volumeLiters: string; flowRateLitersPerMinute: string
  volumeSource: VolumeSource; volumeMethod: string; status: Status; responsibleParticipantId: string; notes: string
  waterPrice: string
}
export const emptyDraft = (waterPrice = ''): Draft => ({ sectorId: '', bedIds: [], startedAt: `${today()}T08:00`, endedAt: '',
  activeDurationMinutes: '', volumeLiters: '', flowRateLitersPerMinute: '', volumeSource: 'Measured', volumeMethod: '', status: 'Completed', responsibleParticipantId: '', notes: '', waterPrice })
// Preferência de preenchimento por horta (não é registro): o último preço do m³ informado neste navegador.
const waterPriceKey = (garden: string) => `horta-water-price:${garden}`
export function rememberedWaterPrice(garden: string) { try { return window.localStorage.getItem(waterPriceKey(garden)) ?? '' } catch { return '' } }
export function rememberWaterPrice(garden: string, value: string) { try { if (value.trim()) window.localStorage.setItem(waterPriceKey(garden), value.trim()) } catch { /* preferência opcional */ } }
export function editDraft(detail: IrrigationDetail): Draft {
  return { ...detail, startedAt: localInstant(detail.startedAt), endedAt: detail.endedAt ? localInstant(detail.endedAt) : '',
    activeDurationMinutes: detail.activeDurationMinutes === null ? '' : String(detail.activeDurationMinutes).replace('.', ','),
    volumeLiters: detail.volumeLiters === null ? '' : String(detail.volumeLiters).replace('.', ','),
    flowRateLitersPerMinute: detail.flowRateLitersPerMinute === null ? '' : String(detail.flowRateLitersPerMinute).replace('.', ','),
    volumeSource: detail.volumeSource ?? 'Measured', volumeMethod: detail.volumeMethod ?? '', responsibleParticipantId: detail.responsibleParticipantId ?? '', notes: detail.notes ?? '', waterPrice: '' }
}
export class FormFailure extends Error {
  fields: Record<string, string[]>
  constructor(fields: Record<string, string[]>) { super('Revise os campos indicados.'); this.fields = fields }
}
export function toValues(draft: Draft, original?: IrrigationDetail): IrrigationValues {
  const errors: Record<string, string[]> = {}
  const measure = (key: 'volumeLiters' | 'activeDurationMinutes' | 'flowRateLitersPerMinute') => {
    const input = draft[key].trim()
    if (!input) return null
    if (!/^\d{1,11}([.,]\d{1,3})?$/.test(input)) { errors[key] = ['Use um número não negativo, com até 3 casas decimais e 11 dígitos inteiros.']; return null }
    return Number(input.replace(',', '.'))
  }
  const instant = (key: 'startedAt' | 'endedAt') => {
    try {
      const prior = original?.[key]
      // O controle HTML representa milissegundos; preservar a precisão original quando não foi editado.
      if (prior && Temporal.PlainDateTime.from(draft[key]).equals(Temporal.PlainDateTime.from(localInstant(prior)))) return prior
      return utcInstant(draft[key])
    } catch { errors[key] = ['Informe data e hora válidas no fuso de São Paulo.']; return '' }
  }
  if (!draft.sectorId) errors.sectorId = ['Selecione o setor.']
  const startedAt = instant('startedAt'), endedAt = draft.endedAt ? instant('endedAt') : null
  const volumeLiters = measure('volumeLiters'), activeDurationMinutes = measure('activeDurationMinutes'), flowRateLitersPerMinute = measure('flowRateLitersPerMinute')
  if (endedAt && startedAt && Temporal.Instant.compare(endedAt, startedAt) < 0) errors.endedAt = ['O fim não pode ser anterior ao início.']
  if (activeDurationMinutes !== null && endedAt && startedAt && !errors.endedAt &&
    activeDurationMinutes > (Number(Temporal.Instant.from(endedAt).epochMilliseconds) - Number(Temporal.Instant.from(startedAt).epochMilliseconds)) / 60000) errors.activeDurationMinutes = ['A duração ativa não pode superar o intervalo informado.']
  if (volumeLiters !== null && !draft.volumeMethod.trim()) errors.volumeMethod = ['Informe o método de obtenção do volume.']
  const priceInput = original ? '' : draft.waterPrice.trim()
  let waterPricePerCubicMeterBrl: number | null = null
  if (priceInput) {
    if (!/^\d{1,9}([.,]\d{1,4})?$/.test(priceInput) || Number(priceInput.replace(',', '.')) <= 0) errors.waterPrice = ['Use um valor positivo em reais, com até 4 casas decimais.']
    else waterPricePerCubicMeterBrl = Number(priceInput.replace(',', '.'))
  }
  if (Object.keys(errors).length) throw new FormFailure(errors)
  return { sectorId: draft.sectorId, bedIds: draft.bedIds, startedAt, endedAt, volumeLiters, activeDurationMinutes, flowRateLitersPerMinute,
    volumeSource: volumeLiters === null ? null : draft.volumeSource, volumeMethod: volumeLiters === null ? null : draft.volumeMethod.trim(),
    status: draft.status, responsibleParticipantId: draft.responsibleParticipantId || null, notes: draft.notes.trim() || null,
    ...(original ? {} : { waterPricePerCubicMeterBrl }) }
}
