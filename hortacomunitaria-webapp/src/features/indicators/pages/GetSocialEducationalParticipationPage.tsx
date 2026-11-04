import { formatDate } from "../../../services/dates"
import { useCallback } from "react"
import { IndicatorPanel } from "../../../components/IndicatorPanel"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useListFilters } from "../../../hooks/useListFilters"
import { useLookups } from "../../../hooks/useLookups"
import { useRemote } from "../../../hooks/useRemote"
import { indicatorOriginHref as routeHref } from "../../../services/indicatorOriginHref"
import { formatNumber } from "../../irrigation/formValues"
import type { RecordRoute } from "../../../types/records"
import { indicatorApi } from "../services/indicatorApi"
export function GetSocialEducationalParticipationPage({ route }: { route: RecordRoute }) {
const filters = useListFilters(route, ['from', 'to', 'dimension', 'institutionId'], true)
const options = useLookups(route.garden, ['institution'])
const load = useCallback((signal: AbortSignal) => indicatorApi.getSocialEducationalParticipation(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, dimension: filters.values.dimension || undefined, institutionId: filters.values.institutionId || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.dimension, filters.values.institutionId])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Participação social e educação" unit="Atividades, presenças e pessoas identificadas" competence="data da atividade" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: record.definition, availability: record.availability, summary: [{ label: 'Atividades distintas', value: formatNumber(record.activityCount) }, { label: 'Atividades sociais', value: formatNumber(record.socialCount) }, { label: 'Atividades educacionais', value: formatNumber(record.educationalCount) }, { label: 'Público conhecido', value: record.knownAudience === null ? "Não informado" : formatNumber(record.knownAudience) }, { label: 'Eventos sem público total', value: formatNumber(record.eventsWithoutAudienceTotal) }, { label: 'Presenças nominais', value: formatNumber(record.nominalPresences) }, { label: 'Pessoas identificadas distintas', value: formatNumber(record.identifiedPeople) }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="dimension" label="Dimensão" value={filters.draft.dimension} change={value => filters.change("dimension", value)} options={[{ id: 'Social', label: 'Social' }, { id: 'Educational', label: 'Educacional' }]} />
<SelectField name="institutionId" label="Instituição" value={filters.draft.institutionId} change={value => filters.change("institutionId", value)} options={options.data?.institution ?? []} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
>{record?.origins.items.map((item, index) => <li key={index} className="rounded-xl border border-horta-line p-4"><Facts items={[{ label: 'Data da atividade', value: formatDate(item.performedOn) }, { label: 'Social', value: item.isSocial ? "Sim" : "Não" }, { label: 'Educacional', value: item.isEducational ? "Sim" : "Não" }, { label: 'Presenças nominais', value: formatNumber(item.nominalPresences) }, { label: 'Público conhecido', value: item.knownAudience === null ? "Não informado" : formatNumber(item.knownAudience) }, { label: 'Cobertura da lista nominal', value: item.audienceListComplete === null ? "Não informado" : item.audienceListComplete ? "Sim" : "Não" }]} /><a className="mt-4 inline-block font-semibold underline" href={routeHref(route.garden, 'atividades', 'registros', item.activityId, new URLSearchParams({ from: filters.values.from, to: filters.values.to, dimension: filters.values.dimension, institutionId: filters.values.institutionId }))}>Abrir origem</a></li>)}</IndicatorPanel>
}
