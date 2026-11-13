import type { AppRoute } from '../../../routes/routes'
import { linkTo } from '../../../routes/routes'
import { useCallback, useMemo } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { useGardenState } from '../../../hooks/useGardenState'
import { Failure } from '../../../components/Feedback'
import { GardenJourney } from '../components/GardenJourney'
import { formatDate } from '../../../services/dates'
import { today } from '../../irrigation/formValues'

import { GardenOverviewScene } from '../../interactive-garden/components/GardenOverviewScene'
import { createGardenProjection } from '../../interactive-garden/projection'
import { interactiveGardenApi } from '../../interactive-garden/services/interactiveGardenApi'
import { gardenCropsApi } from '../../interactive-garden/services/gardenCropsApi'

/** Visão geral: resumos da horta e a cena panorâmica, exibidos ao entrar na aplicação. */
export function OverviewPage({ route }: { route: AppRoute }) {
  const workspace = useGardenState()
  const on = today()
  const loadInteractive = useCallback((signal: AbortSignal) => interactiveGardenApi.get(route.garden, on, signal), [on, route.garden])
  const organization = useRemote(`${route.garden}/${on}`, loadInteractive)
  const loadCrops = useCallback((signal: AbortSignal) => gardenCropsApi.list(route.garden, on, signal), [on, route.garden])
  const crops = useRemote(`${route.garden}/${on}`, loadCrops)
  const projection = useMemo(() => createGardenProjection(workspace, organization.data), [organization.data, workspace])
  const draft = projection.garden
  return <>
    <h1 className="sr-only">Visão geral de {workspace.name}</h1>
    <GardenJourney garden={route.garden} sectors={organization.data?.sectors.length} beds={organization.data?.beds} crops={crops.data?.length} hasLayout={organization.data ? Boolean(draft.boundary || draft.elements.length > 0) : undefined} hasTerrain={organization.data ? Boolean(draft.boundary) : undefined} />
    <section className="garden-overview" aria-labelledby="garden-overview-title">
      {organization.loading && <p className="hint plant-status" role="status">Carregando setores e canteiros da horta…</p>}
      {organization.error && <Failure error={organization.error} />}
      {projection.warnings.map((warning) => <p className="notice" role="status" key={warning}>{warning}</p>)}
      {crops.loading && <p role="status">Carregando plantios da cena…</p>}
      {crops.error && <Failure error={crops.error} />}
      {organization.data && !organization.loading && !organization.error && (draft.boundary || draft.elements.length > 0 ? <GardenOverviewScene key={route.garden} gardenId={route.garden} garden={draft} crops={crops.data} sceneDate={on} heading={<><h2 id="garden-overview-title">Visão panorâmica da horta</h2><p>{workspace.name} · cena de {formatDate(on)}</p></>} actions={<a className="scene-hud-button" href={linkTo(route.garden, 'horta/mapa')}>Editar desenho</a>} legendNote={<p>Baixa, média e alta densidade são ilustrações da quantidade inicial por área ocupada; cada desenho pode representar várias plantas. Reposições não aumentam essa referência. Crescimento e colheita prevista dependem dos prazos configurados, sem confirmar maturidade. Posições e formas mostram a última configuração salva do mapa.</p>} /> : <div className="panel empty"><h2 id="garden-overview-title" className="text-[15px]">Visão panorâmica da horta</h2><p>Nenhum terreno salvo. Desenhe e salve a configuração da horta para vê-la aqui.</p><a className="action-link" href={linkTo(route.garden, 'horta/mapa')}>Abrir o mapa da horta</a></div>)}
    </section>
  </>
}
