import { useCallback, useState } from 'react'
import { Failure } from '../../../components/Feedback'
import { useRemote } from '../../../hooks/useRemote'
import { PlantingForm } from '../components/PlantingForm'
import { bedCultivationApi } from '../services/bedCultivationApi'

export function BedPlantingPage({ garden, bedId, cycleId, saved, cancel }: { garden: string; bedId: string; cycleId: string; saved: () => void; cancel: () => void }) {
  const [revision, setRevision] = useState(0)
  const load = useCallback((signal: AbortSignal) => bedCultivationApi.plantingContext(garden, bedId, cycleId, signal), [garden, bedId, cycleId])
  const result = useRemote(`${garden}/${bedId}/${cycleId}/${revision}`, load)
  if (result.loading) return <p role="status">Carregando cultivo deste canteiro…</p>
  if (result.error) return <><Failure error={result.error} retry={() => setRevision(value => value + 1)} /><button className="secondary" onClick={cancel}>Voltar ao canteiro</button></>
  if (!result.data || result.data.cycle.validity !== 'Valid' || result.data.cycle.status !== 'InProgress') return <section className="panel"><h1>Cultivo indisponível para novo plantio</h1><p>Confira o cultivo e seus registros antes de continuar.</p><button className="secondary" onClick={cancel}>Voltar ao canteiro</button></section>
  return <PlantingForm garden={garden} fixedCycle={result.data.cycle} speciesName={result.data.speciesName} saved={saved} cancel={cancel} />
}
