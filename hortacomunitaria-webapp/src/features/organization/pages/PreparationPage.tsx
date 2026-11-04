import { today } from '../../irrigation/formValues'
import { useCallback, useState } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { useGardenState } from '../../../hooks/useGardenState'
import { routeHref } from '../../../hooks/useRoute'
import { Failure, Pagination } from '../../../components/Feedback'
import { organizationSetupApi } from '../services/organizationSetupApi'
import type { RecordRoute } from '../../../types/records'
export function PreparationPage({ route }: { route: RecordRoute }) {
  const garden = useGardenState()
  const [page, setPage] = useState(1), [revision, setRevision] = useState(0), on = today()
  const load = useCallback((signal: AbortSignal) => organizationSetupApi.get(route.garden, page, on, signal), [route.garden, page, on])
  const data = useRemote(`${route.garden}/${page}/${on}/${revision}`, load)
  return <><div className="page-heading"><div><span className="eyebrow">Primeiros passos</span><h1>Preparar a horta</h1><p>Siga a organização física antes de começar os registros de campo.</p></div></div>
    {data.loading && <p role="status">Conferindo a organização da horta…</p>}
    {data.error && <Failure error={data.error} retry={() => setRevision(value => value + 1)} />}
    {data.data && <><ol className="setup-steps">
      <li><span className="step-number">1</span><div><h2>Horta selecionada</h2><p>Os próximos cadastros pertencem à horta indicada no topo.</p><a href={routeHref(route.garden, 'horta', 'hortas', route.garden)}>Consultar a horta</a></div></li>
      <li><span className="step-number">2</span><div><h2>Cadastre os setores</h2><p>{data.data.sectors.length} setor(es) ativo(s). Organize as áreas atendidas pela irrigação.</p>{garden.isActive && <a className="action-link" href={routeHref(route.garden, 'horta', 'setores', 'novo')}>Cadastrar setor</a>}<a href={routeHref(route.garden, 'horta', 'setores')}>Consultar setores</a></div></li>
      <li><span className="step-number">3</span><div><h2>Cadastre e configure os canteiros</h2><p>Depois da identificação, informe a área cultivável, a data de início e o setor. Sem área, não é possível abrir um ciclo; sem associação na data, o canteiro não aparece na irrigação.</p>{garden.isActive && <a className="action-link" href={routeHref(route.garden, 'horta', 'canteiros', 'novo')}>Cadastrar canteiro</a>}<a href={routeHref(route.garden, 'horta', 'canteiros')}>Consultar canteiros</a>
      <ul className="setup-beds">{data.data.beds.items.map(bed => <li key={bed.id}><strong>{bed.name}</strong><span>{bed.configuration ? 'Área informada' : 'Área pendente'} · {bed.sectorAssignment ? 'Setor associado' : 'Setor pendente'} na data atual</span><a href={routeHref(route.garden, 'horta', 'canteiros', bed.id)}>Configurar {bed.name}</a></li>)}</ul><Pagination data={data.data.beds} change={setPage} label="Páginas de canteiros em preparação" /></div></li>
      <li><span className="step-number">4</span><div><h2>Comece os registros</h2><p>Irrigação usa setor e canteiros associados. Cultivo começa pela espécie e por um ciclo em canteiro com área configurada.</p><a href={routeHref(route.garden, 'irrigacao', 'registros')}>Registrar irrigações</a><a href={routeHref(route.garden, 'cultivo', 'especies')}>Preparar cultivo</a><a href={routeHref(route.garden, 'visao-geral', 'painel')}>Ver dashboard</a></div></li>
    </ol></>}
  </>
}
