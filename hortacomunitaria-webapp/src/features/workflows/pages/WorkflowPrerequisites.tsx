import type { ReactNode } from 'react'
import { useLookups } from '../../../hooks/useLookups'
import { routeHref } from '../../../hooks/useRoute'
import { Failure } from '../../../components/Feedback'
import type { LookupKey } from '../../../services/lookups'
import type { RecordRoute } from '../../../types/records'
interface Requirement { key: LookupKey; label: string; module: string; resource: string }
const species: Requirement = { key: 'species', label: 'Cadastrar espécie', module: 'cultivo', resource: 'especies' }
const cycle: Requirement = { key: 'cultivationCycle', label: 'Abrir ciclo de cultivo', module: 'cultivo', resource: 'ciclos' }
const requirements: Record<string, Requirement[]> = {
  'cultivo/cultivares': [species], 'cultivo/origens': [species],
  'cultivo/ciclos': [{ key: 'bed', label: 'Cadastrar e configurar canteiro', module: 'horta', resource: 'canteiros' }, species],
  'cultivo/plantios': [cycle], 'cultivo/observacoes': [cycle],
  'cultivo/avaliacoes': [{ key: 'planting', label: 'Registrar plantio', module: 'cultivo', resource: 'plantios' }],
  'producao/colheitas': [cycle], 'producao/secagens': [{ key: 'harvest', label: 'Registrar colheita e massa aproveitável', module: 'producao', resource: 'colheitas' }],
  'ambiente/medicoes': [{ key: 'environmentalVariable', label: 'Cadastrar variável ambiental', module: 'ambiente', resource: 'variaveis' }, { key: 'samplingPoint', label: 'Cadastrar ponto de coleta', module: 'ambiente', resource: 'pontos' }],
  'irrigacao/registros': [{ key: 'irrigationSector', label: 'Cadastrar setor', module: 'horta', resource: 'setores' }],
}
export function WorkflowPrerequisites({ route, children }: { route: RecordRoute; children: ReactNode }) {
  const needed = route.id === 'novo' ? requirements[`${route.module}/${route.resource}`] ?? [] : []
  const options = useLookups(route.garden, needed.map(item => item.key))
  if (!needed.length) return children
  if (options.loading) return <p role="status">Conferindo os cadastros necessários…</p>
  if (options.error) return <Failure error={options.error} retry={options.retry} />
  const missing = needed.filter(item => !options.data?.[item.key]?.some(choice => choice.isActive !== false))
  if (!missing.length && route.module === 'producao' && route.resource === 'secagens' && !options.data?.harvest?.some(item => item.usableMassKg !== null && item.usableMassKg !== undefined && item.usableMassKg > 0)) return <section className="panel"><h1>Confira a massa aproveitável</h1><p>As colheitas ainda não têm massa aproveitável positiva informada. Abra a origem e corrija a pesagem antes de destinar massa à secagem.</p><a className="action-link" href={routeHref(route.garden, 'producao', 'colheitas')}>Revisar colheitas</a></section>
  if (!missing.length) return children
  return <section className="panel"><span className="eyebrow">Antes de registrar</span><h1>Prepare os cadastros necessários</h1><p>Este formulário depende dos itens abaixo. Cadastre-os primeiro e volte a esta área; nenhum rascunho foi iniciado.</p><ul className="space-y-4">{missing.map(item => <li key={item.key}><a className="action-link" href={routeHref(route.garden, item.module, item.resource, 'novo')}>{item.label}</a></li>)}</ul><div className="next-step"><a href={routeHref(route.garden, route.module, route.resource)}>Voltar à consulta</a><a href={routeHref(route.garden, 'horta', 'preparacao')}>Ver preparação da horta</a></div></section>
}
