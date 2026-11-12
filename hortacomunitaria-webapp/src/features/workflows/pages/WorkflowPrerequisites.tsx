import type { ReactNode } from 'react'
import { useLookups } from '../../../hooks/useLookups'
import { Failure } from '../../../components/Feedback'
import type { LookupKey } from '../../../services/lookups'
import { linkTo } from '../../../routes/routes'
import type { AppRoute, PageKey } from '../../../routes/routes'
interface Requirement { key: LookupKey; label: string; page: PageKey }
const species: Requirement = { key: 'species', label: 'Cadastrar espécie', page: 'catalogo/especies' }
const cycle: Requirement = { key: 'cultivationCycle', label: 'Abrir ciclo de cultivo', page: 'cultivo/ciclos' }
/** Cadastros que precisam existir antes de abrir o formulário de criação de cada página. */
const requirements: Partial<Record<PageKey, Requirement[]>> = {
  'catalogo/cultivares': [species], 'cultivo/origens': [species],
  'cultivo/ciclos': [{ key: 'bed', label: 'Cadastrar e configurar canteiro', page: 'horta/canteiros' }, species],
  'cultivo/plantios': [cycle], 'cultivo/observacoes': [cycle],
  'cultivo/avaliacoes': [{ key: 'planting', label: 'Registrar plantio', page: 'cultivo/plantios' }],
  'producao/colheitas': [cycle], 'producao/secagens': [{ key: 'harvest', label: 'Registrar colheita e massa aproveitável', page: 'producao/colheitas' }],
  'ambiente/medicoes': [{ key: 'environmentalVariable', label: 'Cadastrar variável ambiental', page: 'ambiente/variaveis' }, { key: 'samplingPoint', label: 'Cadastrar ponto de coleta', page: 'ambiente/pontos' }],
  'irrigacao': [{ key: 'irrigationSector', label: 'Cadastrar setor', page: 'horta/setores' }],
}
export function WorkflowPrerequisites({ route, children }: { route: AppRoute; children: ReactNode }) {
  const needed = route.id === 'novo' && route.page ? requirements[route.page] ?? [] : []
  const options = useLookups(route.garden, needed.map(item => item.key))
  if (!needed.length) return children
  if (options.loading) return <p role="status">Conferindo os cadastros necessários…</p>
  if (options.error) return <Failure error={options.error} retry={options.retry} />
  const missing = needed.filter(item => !options.data?.[item.key]?.some(choice => choice.isActive !== false))
  if (!missing.length && route.page === 'producao/secagens' && !options.data?.harvest?.some(item => item.usableMassKg !== null && item.usableMassKg !== undefined && item.usableMassKg > 0)) return <section className="panel"><h1>Confira a massa aproveitável</h1><p>As colheitas ainda não têm massa aproveitável positiva informada. Abra a origem e corrija a pesagem antes de destinar massa à secagem.</p><a className="action-link" href={linkTo(route.garden, 'producao/colheitas')}>Revisar colheitas</a></section>
  if (!missing.length) return children
  return <section className="panel"><span className="eyebrow">Antes de registrar</span><h1>Prepare os cadastros necessários</h1><p>Este formulário depende dos itens abaixo. Cadastre-os primeiro e volte a esta área; nenhum rascunho foi iniciado.</p><ul className="m-0 flex list-none flex-wrap gap-2 p-0">{missing.map(item => <li key={item.key}><a className="action-link" href={linkTo(route.garden, item.page, 'novo')}>{item.label}</a></li>)}</ul><div className="next-step">{route.page && <a href={linkTo(route.garden, route.page)}>Voltar à consulta</a>}<a href={linkTo(route.garden, 'horta/setores')}>Ver setores e canteiros</a></div></section>
}
