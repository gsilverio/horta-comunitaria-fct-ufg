import { useCallback, useEffect, useRef, useState } from 'react'
import { Failure, Pagination } from '../../../components/Feedback'
import { useRemote } from '../../../hooks/useRemote'
import type { ApiFailure } from '../../../types/api'
import { formatDate } from '../../../services/dates'
import { formatNumber, today } from '../../irrigation/formValues'
import { bedContextApi } from '../../organization/services/bedContextApi'
import { InteractivePlantingForm } from './InteractivePlantingForm'
import { BedPlantingPage } from '../../cultivation/pages/BedPlantingPage'
import { bedCultivationApi } from '../../cultivation/services/bedCultivationApi'
import type { DemoElement } from '../types'
import type { Species } from '../../cultivation/types/Species'
import { SpeciesPicker } from '../components/SpeciesPicker'

interface Props {
  garden: string
  element: DemoElement
  needsMapSave: boolean
  saving: boolean
  uncertain: boolean
  saveBlocked: boolean
  saveError?: ApiFailure
  saveMap: () => Promise<boolean>
  close: () => void
  editShape: () => void
  editing: (active: boolean) => void
}

/** Compõe os cadastros reais sem transformar o desenho em medida física ou fato de plantio. */
export function InteractiveBedMenu({ garden, element, needsMapSave, saving, uncertain, saveBlocked, saveError, saveMap, close, editShape, editing }: Props) {
  const heading = useRef<HTMLHeadingElement>(null)
  const [stage, setStage] = useState<'home' | 'register' | 'cycle' | 'planting' | 'complete'>('home')
  const [species, setSpecies] = useState<Species>()
  const [cycleId, setCycleId] = useState('')
  const [revision, setRevision] = useState(0)
  const [page, setPage] = useState(1)
  const [notice, setNotice] = useState('')
  const on = today()
  const ready = !needsMapSave && !!element.entityId
  const loadBed = useCallback((signal: AbortSignal) => ready && element.entityId ? bedContextApi.on(garden, element.entityId, on, signal) : Promise.resolve(undefined), [ready, element.entityId, garden, on])
  const bedState = useRemote(`${garden}/${element.entityId}/${ready}/${revision}`, loadBed)
  const loadCrops = useCallback((signal: AbortSignal) => ready && element.entityId ? bedCultivationApi.list(garden, element.entityId, page, signal) : Promise.resolve(undefined), [ready, element.entityId, garden, page])
  const crops = useRemote(`${garden}/${element.entityId}/${ready}/${page}/${revision}`, loadCrops)
  const bed = bedState.data
  const goHome = () => { setStage('home'); setRevision(value => value + 1) }

  useEffect(() => {
    const previous = document.activeElement
    heading.current?.focus()
    return () => { if (previous instanceof HTMLElement || previous instanceof SVGElement) previous.focus() }
  }, [])

  useEffect(() => { editing(stage !== 'home' && stage !== 'complete'); return () => editing(false) }, [stage, editing])

  // Escolher a espécie já avança para a próxima etapa (sem botão de confirmação separado).
  const chooseSpecies = (chosen: Species) => {
    setSpecies(chosen)
    setNotice('')
    setStage(!ready ? 'register' : 'cycle')
  }

  return <aside className="interactive-cultivation drawing-menu order-1 flex max-h-[85dvh] w-full flex-col min-w-0 self-stretch overflow-hidden border border-[#a5b78b] bg-[#f3f5e9] lg:col-start-2 lg:row-start-1 lg:h-0 lg:max-h-none lg:min-h-full lg:w-[420px] xl:w-[460px]" aria-labelledby="bed-menu-title" onKeyDown={event => { if (event.key === 'Escape' && (stage === 'home' || stage === 'complete') && !saving && !uncertain) { event.preventDefault(); close() } }}>
    <header className="flex items-start justify-between gap-4 border-b-2 border-[#9cae78] bg-[#1c4939] p-5 text-[#f5efd3]">
      <div><span className="font-mono text-[10px] tracking-[.2em] text-[#c4d697] uppercase">Estação de cultivo</span><h2 ref={heading} tabIndex={-1} id="bed-menu-title" className="mt-1 mb-1 text-xl text-[#fff8de]">{element.name}</h2><p className="m-0 text-xs text-[#d3dfbb]">{element.sectorName ?? 'Canteiro da horta'} · configure e plante sem sair do mapa</p></div>
      {(stage === 'home' || stage === 'complete') && <button type="button" className="secondary !min-h-8 !px-3 !py-1" disabled={saving || uncertain} onClick={close} aria-label="Fechar menu do canteiro">×</button>}
    </header>
    <ol className="m-0 grid list-none grid-cols-3 border-b border-[#b4c29b] bg-[#e5ebd8] p-0 font-mono text-[10px] sm:text-xs" aria-label="Etapas do plantio">
      {['Espécie', 'Configuração', 'Plantio'].map((label, index) => <li key={label} className="border-r border-[#b4c29b] px-3 py-3 last:border-r-0" aria-current={stage === 'home' && index === 0 || (stage === 'register' || stage === 'cycle') && index === 1 || (stage === 'planting' || stage === 'complete') && index === 2 ? 'step' : undefined}><span className="mr-1 font-bold">0{index + 1}</span>{label}</li>)}
    </ol>
    <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain p-4">
      {notice && <p className="notice" role="status">{notice}</p>}
      {stage === 'home' && <>
        <div className="bed-menu-actions"><button type="button" className="secondary" disabled={saving || uncertain} onClick={editShape}>Editar forma e posição</button><button type="button" className="secondary" disabled={saving || uncertain} onClick={close}>Voltar ao mapa</button></div>
        <SpeciesPicker selected={species} choose={chooseSpecies} />
        {ready && <details className="mt-6 border-t border-[#c8d3b6] pt-3"><summary className="font-semibold">Consultar cultivos e retomar plantios</summary>
          {bedState.loading && <p role="status">Carregando canteiro…</p>}
          {bedState.error && <Failure error={bedState.error} retry={() => setRevision(value => value + 1)} />}
          <section aria-label="Cultivos do canteiro no mapa"><h3 className="mt-0 border-b border-[#c8d3b6] pb-3">Cultivos deste canteiro</h3>
            {crops.loading && <p role="status">Carregando cultivos e plantios…</p>}
            {crops.error && <Failure error={crops.error} retry={() => setRevision(value => value + 1)} />}
            {crops.data?.items.length === 0 && <p className="text-horta-muted">Nenhuma espécie configurada. Prepare o canteiro e escolha a primeira planta.</p>}
            {crops.data?.items.map(cycle => <article key={cycle.id} className="border-b border-[#c8d3b6] py-4 last:border-0">
              <div className="flex flex-wrap items-start justify-between gap-3"><div><h4 className="m-0 font-semibold text-horta-forest">{cycle.speciesName}</h4><p className="mt-1 mb-2 text-xs text-horta-muted">{formatNumber(cycle.occupiedAreaM2)} m² · desde {formatDate(cycle.startedOn)}</p></div><span className="font-mono text-[10px] uppercase text-horta-muted">{cycle.status === 'Closed' ? 'Encerrado' : cycle.plantings.totalCount ? 'Plantio registrado' : 'Falta registrar plantio'}</span></div>
              {cycle.plantings.items.map(planting => <p key={planting.id} className="my-1 text-xs">{formatNumber(planting.plantedQuantity)} plantas · {formatDate(planting.plantedOn)}{planting.kind === 'Replacement' ? ' · reposição' : ''}</p>)}
              {cycle.plantings.hasNextPage && <p className="hint">Exibindo parte dos registros; {cycle.plantings.totalCount} plantios no cultivo.</p>}
              {bed?.isActive && cycle.status === 'InProgress' && <button type="button" className="secondary mt-2" onClick={() => { setCycleId(cycle.id); setStage('planting') }}>{cycle.plantings.totalCount ? 'Registrar outro plantio' : 'Continuar: registrar plantio'}</button>}
            </article>)}
            {crops.data && crops.data.totalPages > 1 && <Pagination data={crops.data} change={setPage} label="Páginas de cultivos no mapa" />}
          </section>
        </details>}
      </>}
      {(stage === 'register' || stage === 'planting') && species && <div className="mb-4 border-b border-[#c8d3b6] pb-3"><span className="font-mono text-[10px] uppercase text-horta-muted">Espécie escolhida</span><strong className="block">{species.commonName}</strong></div>}
      {stage === 'register' && <section><h3 className="mt-0">Confirme o desenho para continuar</h3><p>A espécie já foi escolhida. Salve o mapa para cadastrar este canteiro no setor desenhado e seguir com a área real e o plantio.</p><button type="button" className="w-full" disabled={saving || saveBlocked} onClick={() => { void saveMap().then(saved => { if (saved) setStage('cycle') }) }}>{saving ? 'Salvando mapa…' : uncertain ? 'Confirmar salvamento do mapa' : 'Salvar mapa e continuar'}</button>{saveError && <Failure error={saveError} />}<button type="button" className="secondary mt-3 w-full" disabled={saving || uncertain} onClick={() => setStage('home')}>Voltar às espécies</button></section>}
      {stage !== 'home' && stage !== 'register' && bedState.loading && <p role="status">Atualizando cadastro…</p>}
      {stage !== 'home' && stage !== 'register' && bedState.error && <><Failure error={bedState.error} retry={() => setRevision(value => value + 1)} /><button type="button" onClick={goHome}>Voltar ao menu</button></>}
      {bed && species && stage === 'cycle' && <InteractivePlantingForm key={species.id} garden={garden} bed={bed} species={species} saved={() => { setNotice('Plantio confirmado e disponível na panorâmica.'); setRevision(value => value + 1); setStage('complete') }} cancel={goHome} resume={id => { setCycleId(id); setStage('planting') }} />}
      {stage === 'complete' && <section className="border-l-4 border-[#78944f] pl-4"><span className="font-mono text-[10px] uppercase text-horta-muted">Cultivo em andamento</span><h3 className="mt-1">Plantio concluído</h3><p>O plantio de {species?.commonName ?? 'seu cultivo'} foi salvo neste canteiro. Veja a mudança do solo e as plantas na visão panorâmica.</p><button type="button" className="w-full" onClick={close}>Voltar ao desenho</button><button type="button" className="secondary mt-3 w-full" onClick={goHome}>Ver cultivos e escolher outra espécie</button></section>}
      {bed && stage === 'planting' && <BedPlantingPage key={cycleId} garden={garden} bedId={bed.id} cycleId={cycleId} saved={() => { setNotice('Plantio confirmado e disponível na panorâmica.'); setRevision(value => value + 1); setStage('complete') }} cancel={goHome} />}
    </div>
  </aside>
}
