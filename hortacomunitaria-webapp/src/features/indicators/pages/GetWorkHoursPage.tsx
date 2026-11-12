import type { AppRoute } from '../../../routes/routes'
import { linkTo, withReturn } from '../../../routes/routes'
import { formatDate } from "../../../services/dates"
import { useCallback } from "react"
import { IndicatorPanel } from "../../../components/IndicatorPanel"
import { Field, SelectField, Facts } from "../../../components/Fields"
import { Failure } from "../../../components/Feedback"
import { useListFilters } from "../../../hooks/useListFilters"
import { useLookups } from "../../../hooks/useLookups"
import { useRemote } from "../../../hooks/useRemote"
import { formatNumber } from "../../irrigation/formValues"

import { indicatorApi } from "../services/indicatorApi"
export function GetWorkHoursPage({ route }: { route: AppRoute }) {
const filters = useListFilters(route, ['from', 'to', 'participantId', 'category'], true)
const options = useLookups(route.garden, ['participant'])
const load = useCallback((signal: AbortSignal) => indicatorApi.getWorkHours(route.garden, { page: filters.page, pageSize: 5, from: filters.values.from || undefined, to: filters.values.to || undefined, participantId: filters.values.participantId || undefined, category: filters.values.category || undefined }, signal), [route.garden, filters.page, filters.values.from, filters.values.to, filters.values.participantId, filters.values.category])
const result = useRemote(JSON.stringify(filters.values) + "/" + filters.page + "/" + filters.revision, load), record = result.data
return <IndicatorPanel title="Horas de trabalho" unit="Horas individuais (h)" competence="data da atividade" apply={filters.apply} loading={result.loading} error={filters.error ?? result.error} retry={filters.retry} page={filters.changePage} result={record && { from: record.from, to: record.to, definition: 'Resultado de origens válidas; ausência de valor permanece desconhecida.', availability: record.availability, summary: [{ label: 'Horas conhecidas (h)', value: record.knownHours === null ? "Não informado" : formatNumber(record.knownHours) }, { label: 'Atividades distintas', value: formatNumber(record.activityCount) }, { label: 'Participações com horas conhecidas', value: formatNumber(record.knownParticipationCount) }, { label: 'Participações sem horas', value: formatNumber(record.unknownParticipationCount) }, { label: 'Atividades sem participantes registrados', value: formatNumber(record.activitiesWithoutParticipants) }], origins: record.origins }} filters={<>
<Field name="from" label="De" required type="date" value={filters.draft.from} change={value => filters.change("from", value)} error={filters.error} />
<Field name="to" label="Até" required type="date" value={filters.draft.to} change={value => filters.change("to", value)} error={filters.error} />
<SelectField name="participantId" label="Participante" value={filters.draft.participantId} change={value => filters.change("participantId", value)} options={options.data?.participant ?? []} />
<Field name="category" label="Categoria" type="text" value={filters.draft.category} change={value => filters.change("category", value)} />
{options.error && <Failure error={options.error} retry={options.retry} />}</>}
>{record?.origins.items.map((item, index) => <li key={index} className="border-b border-horta-line py-3"><Facts items={[{ label: 'Data da atividade', value: formatDate(item.performedOn) }, { label: 'Participante', value: options.data?.participant?.find(person => person.id === item.participantId)?.label ?? item.participantId }, { label: 'Horas individuais (h)', value: item.workHours === null ? "Não informado" : formatNumber(item.workHours) }]} /><a className="mt-4 inline-block font-semibold underline" href={linkTo(route.garden, 'atividades/registros', item.activityId, withReturn(new URLSearchParams({ from: filters.values.from, to: filters.values.to, participantId: filters.values.participantId, category: filters.values.category })))}>Abrir origem</a></li>)}</IndicatorPanel>
}
