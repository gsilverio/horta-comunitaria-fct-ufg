import { Failure, Pagination } from '../../../components/Feedback'
import type { Species } from '../../cultivation/types/Species'
import { useSpeciesCatalog, useSpeciesImage } from '../hooks/useSpeciesCatalog'

const externalUrl = (url: string) => /^https?:\/\//i.test(url) ? url : undefined

function SpeciesSlot({ species, selected, choose }: { species: Species; selected: boolean; choose: () => void }) {
  const image = useSpeciesImage(species.id, !!species.imageUrl)
  return <li className="min-w-0">
    <button type="button" aria-label={`Selecionar ${species.commonName}`} aria-pressed={selected} onClick={choose} className={`group relative !block w-full !border-2 !p-1 !text-left ${selected ? '!border-[#1c4939] !bg-[#dce9bf] !text-horta-forest' : '!border-[#bdcaaa] !bg-[#f9faf3] !text-horta-ink hover:!border-[#78944f]'}`}>
      <span className="relative flex aspect-square items-center justify-center overflow-hidden bg-[#e4ead6]">
        {image.data ? <img className="h-full w-full object-cover" src={`data:${image.data.mimeType};base64,${image.data.base64}`} alt={`Foto ilustrativa de ${species.commonName}`} /> : <span className="text-center font-mono text-[10px] text-horta-muted">{image.loading && species.imageUrl ? 'Carregando…' : image.error ? 'Foto indisponível' : 'Sem foto'}</span>}
        {selected && <span className="absolute right-1 bottom-1 bg-horta-forest px-1.5 py-0.5 text-xs text-white" aria-hidden="true">✓</span>}
      </span>
      <span className="mt-1 block min-h-9 break-words px-1 text-[11px] leading-tight font-semibold">{species.commonName}</span>
    </button>
    {image.error && <button type="button" className="secondary mt-1 !min-h-6 !px-1 !py-1 !text-[10px]" onClick={image.retry}>Recarregar foto de {species.commonName}</button>}
    {image.data && <details className="!my-1 text-[10px]"><summary className="!min-h-5 !py-0.5 !text-[10px]">Créditos da foto</summary><p className="mb-1 text-[10px]">Foto ilustrativa do grupo; não identifica a cultivar.</p><p className="mb-1 text-[10px]">{image.data.author} · <a className="underline" href={externalUrl(image.data.licenseUrl)} target="_blank" rel="noreferrer">{image.data.license}</a> · <a className="underline" href={externalUrl(image.data.sourceUrl)} target="_blank" rel="noreferrer">Origem</a></p><p className="mb-1 text-[10px]">{image.data.attribution}</p><p className="mb-1 text-[10px]">{image.data.changes}</p></details>}
  </li>
}

export function SpeciesPicker({ selected, choose, next }: { selected?: Species; choose: (species: Species) => void; next: () => void }) {
  const catalog = useSpeciesCatalog()
  return <section aria-label="Seleção de espécies do catálogo">
    <span className="font-mono text-[10px] uppercase tracking-widest text-horta-muted">Catálogo de espécies · base Embrapa</span>
    <h3 className="mt-1 mb-2">Escolha o que plantar</h3>
    <p className="text-xs text-horta-muted">Selecione uma espécie. A variedade e os dados do plantio vêm na próxima etapa.</p>
    {catalog.loading && <p role="status">Carregando catálogo…</p>}
    {catalog.error && <Failure error={catalog.error} retry={catalog.retry} />}
    {catalog.data?.items.length === 0 && <p className="notice">Nenhuma espécie ativa no catálogo. Confira a importação do catálogo Embrapa nos cadastros da aplicação.</p>}
    {catalog.data && <><ul className="m-0 grid list-none grid-cols-3 gap-2 p-0" aria-label="Espécies disponíveis">{catalog.data.items.map(species => <SpeciesSlot key={species.id} species={species} selected={selected?.id === species.id} choose={() => choose(species)} />)}</ul><Pagination data={catalog.data} change={catalog.changePage} label="Páginas do catálogo de espécies" /></>}
    {selected && <div className="mt-4 border-l-4 border-[#78944f] pl-3" role="status"><span className="font-mono text-[10px] uppercase text-horta-muted">Espécie selecionada</span><strong className="block text-sm">{selected.commonName}</strong>{selected.scientificName && <span className="block text-xs italic text-horta-muted">{selected.scientificName}</span>}</div>}
    <button type="button" className="mt-4 w-full" disabled={!selected} onClick={next}>Continuar com esta espécie</button>
  </section>
}
