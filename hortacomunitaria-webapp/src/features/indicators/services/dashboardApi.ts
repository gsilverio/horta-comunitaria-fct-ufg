import { indicatorApi } from './indicatorApi'
import { irrigationApi } from '../../irrigation/services/irrigationApi'
import { formatNumber } from '../../irrigation/formValues'
import type { IndicatorAvailability } from '../../../types/enums'
export interface DashboardResult { availability: IndicatorAvailability; value: string; note: string; bars?: { label: string; value: number | null }[]; unit?: string; chartNote?: string }
interface Period { from: string; to: string }
export interface DashboardIndicator { id: string; title: string; group: string; competence: string; load: (garden: string, period: Period, signal: AbortSignal) => Promise<DashboardResult> }
const number = (value: number | null, unit: string) => value === null ? 'Não informado' : `${formatNumber(value)} ${unit}`
const query = (period: Period) => ({ ...period, page: 1, pageSize: 5 })
export const dashboardIndicators: DashboardIndicator[] = [
  { id: 'water-consumption', title: 'Água e irrigação por setor', group: 'Ambiente e água', competence: 'Início da irrigação · São Paulo', load: async (garden, period, signal) => {
    const data = await irrigationApi.consumption(garden, { ...period, sectorId: '', validity: 'Valid' }, 1, signal)
    return { availability: data.summary.availability, value: number(data.summary.knownVolumeLiters, 'L'), note: `${data.summary.eventCount} irrigações · ${data.summary.eventsWithoutVolume} sem volume informado${data.summary.includesEstimatedVolume ? ' · inclui estimativas' : ''}`, bars: data.bySector.items.map(item => ({ label: item.code + ' · ' + item.name, value: item.knownVolumeLiters })), unit: 'L', chartNote: data.bySector.hasNextPage ? 'Primeira página de setores; consulte todos no detalhamento.' : 'Volume conhecido por setor. Ausência de volume não é zero.' }
  } },
  { id: 'soil-moisture', title: 'Umidade do solo', group: 'Ambiente e água', competence: 'Data da coleta · São Paulo', load: async (garden, period, signal) => {
    const data = await indicatorApi.getSoilMoisture(garden, query(period), signal)
    return { availability: data.availability, value: `${data.origins.totalCount} coletas`, note: `${data.normalCount} normais · ${data.questionableCount} questionáveis. Pontos, métodos e unidades permanecem separados no detalhamento.` }
  } },
  { id: 'seedling-establishment', title: 'Pegamento das mudas', group: 'Cultivo e produção', competence: 'Data do plantio', load: async (garden, period, signal) => {
    const data = await indicatorApi.getSeedlingEstablishment(garden, query(period), signal)
    return { availability: data.availability, value: `${data.eligiblePlantingCount} plantios elegíveis`, note: `${data.withoutAssessmentCount} sem avaliação · ${data.ineligibleMaterialCount} com material não elegível. Percentuais por natureza e método no detalhamento.` }
  } },
  { id: 'productivity', title: 'Produção e produtividade', group: 'Cultivo e produção', competence: 'Data da colheita', load: async (garden, period, signal) => {
    const data = await indicatorApi.getProductivity(garden, query(period), signal)
    return { availability: data.availability, value: number(data.freshMassKg, 'kg frescos'), note: `${data.harvestCount} colheitas. Produtividade em kg/m² por ciclo no detalhamento; áreas não são somadas entre ciclos.` }
  } },
  { id: 'harvest-losses', title: 'Perdas na colheita', group: 'Cultivo e produção', competence: 'Data da colheita', load: async (garden, period, signal) => {
    const data = await indicatorApi.getHarvestLosses(garden, query(period), signal)
    return { availability: data.availability, value: number(data.knownLossKg, 'kg'), note: `${number(data.percentage, '%')} de perda · ${data.withoutUsableMassCount} colheitas sem massa aproveitável.` }
  } },
  { id: 'drying-yield', title: 'Rendimento da secagem', group: 'Cultivo e produção', competence: 'Início do lote de secagem', load: async (garden, period, signal) => {
    const data = await indicatorApi.getDryingYield(garden, query(period), signal)
    return { availability: data.availability, value: number(data.percentage, '%'), note: `${data.eligibleCount} lotes elegíveis · ${data.incompleteCount} incompletos · ${data.mixedSpeciesCount} mistos. Massa seca conhecida: ${number(data.knownDryMassKg, 'kg')}.` }
  } },
  { id: 'work-hours', title: 'Horas de trabalho', group: 'Pessoas e operação', competence: 'Data da atividade', load: async (garden, period, signal) => {
    const data = await indicatorApi.getWorkHours(garden, query(period), signal)
    return { availability: data.availability, value: number(data.knownHours, 'h'), note: `${data.activityCount} atividades · ${data.unknownParticipationCount} participações sem horas · ${data.activitiesWithoutParticipants} atividades sem participantes.` }
  } },
  { id: 'operational-occurrences', title: 'Ocorrências operacionais', group: 'Pessoas e operação', competence: 'Abertura no período · situação atual', load: async (garden, period, signal) => {
    const data = await indicatorApi.getOperationalOccurrences(garden, query(period), signal)
    return { availability: data.availability, value: `${data.count} ocorrências`, note: 'Ocorrências distintas abertas no período, classificadas pela situação atual.', bars: [{ label: 'Abertas', value: data.openCount }, { label: 'Em andamento', value: data.inProgressCount }, { label: 'Resolvidas', value: data.resolvedCount }], unit: '' }
  } },
  { id: 'social-educational-participation', title: 'Participação social e educacional', group: 'Pessoas e operação', competence: 'Data da atividade', load: async (garden, period, signal) => {
    const data = await indicatorApi.getSocialEducationalParticipation(garden, query(period), signal)
    return { availability: data.availability, value: number(data.knownAudience, 'presenças informadas'), note: `${data.activityCount} atividades · ${data.identifiedPeople} pessoas identificadas · ${data.eventsWithoutAudienceTotal} eventos sem público total. Público inclui os participantes nominais.` }
  } },
  { id: 'basic-costs', title: 'Custos previstos e realizados', group: 'Economia', competence: 'Data de competência do lançamento', load: async (garden, period, signal) => {
    const data = await indicatorApi.getBasicCosts(garden, query(period), signal)
    return { availability: data.availability, value: number(data.actualBrl, 'BRL realizados'), note: `${number(data.plannedBrl, 'BRL previstos')} · Diferença: ${number(data.differenceBrl, 'BRL')}. ${data.comparison}`, bars: [{ label: 'Previsto', value: data.plannedBrl }, { label: 'Realizado', value: data.actualBrl }], unit: 'BRL' }
  } },
]
