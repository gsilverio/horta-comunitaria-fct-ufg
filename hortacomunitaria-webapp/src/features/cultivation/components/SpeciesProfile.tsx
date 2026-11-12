import type { AppRoute } from '../../../routes/routes'
import { linkTo, samePage } from '../../../routes/routes'
import { CropPhoto } from './CropCatalogEntry'
import { useSpeciesImage } from '../../interactive-garden/hooks/useSpeciesCatalog'

import { useCanManage } from '../../auth/hooks/useAuth'
import type { Species } from '../types/Species'

export function SpeciesProfile({ species, route }: { species: Species; route: AppRoute }) {
  const image = useSpeciesImage(species.id, !!species.imageUrl)
  return <div className="grid min-w-0 gap-5 sm:grid-cols-[minmax(0,180px)_minmax(0,1fr)] lg:gap-7">
    <figure className="m-0 min-w-0 max-w-56">
      <CropPhoto name={species.commonName} image={image} large />
      <figcaption className="mt-2 text-xs text-horta-muted">Imagem ilustrativa da espécie.</figcaption>
      {image.error && <button type="button" className="text-button !mb-0 text-sm" onClick={image.retry}>Recarregar imagem</button>}
    </figure>
    <div className="min-w-0">
      <span className="eyebrow">Catálogo de cultivo</span>
      <h1 tabIndex={-1} className="break-words">{species.commonName}</h1>
      {species.scientificName && <p className="text-horta-muted italic">{species.scientificName}</p>}
      <span className="inline-block rounded-md bg-horta-soft px-2.5 py-1 text-xs font-medium text-horta-forest">{species.isActive ? 'Espécie ativa' : 'Espécie inativa'}</span>
      <section className="mt-4 border-t border-horta-line pt-3" aria-label="Descrição da espécie">
        <h2 className="mb-1">Sobre a espécie</h2>
        <p className="mb-0 whitespace-pre-line break-words text-horta-muted">{species.description || 'Esta espécie ainda não possui uma descrição. Você pode complementar seus dados em Editar cadastro.'}</p>
      </section>
      <a className="mt-2 inline-flex min-h-11 items-center text-sm font-medium underline underline-offset-4" href={linkTo(route.garden, 'catalogo/cultivares', '', new URLSearchParams({ speciesId: species.id }))}>Ver cultivares desta espécie →</a>
    </div>
  </div>
}

export function SpeciesVisualStages({ species, route }: { species: Species; route: AppRoute }) {
  const canManage = useCanManage()
  const configured = species.visualGrowthDays !== null && species.visualHarvestDays !== null
  const stages = [
    { label: 'Plantio', value: 'Dia 0' },
    { label: 'Crescimento intermediário', value: configured ? `Após ${species.visualGrowthDays} dias` : 'Não configurado' },
    { label: 'Colheita prevista', value: configured ? `Após ${species.visualHarvestDays} dias` : 'Não configurado' },
  ]
  return <section aria-labelledby="species-stages-title">
    <div className="flex flex-wrap items-start justify-between gap-3"><div><h2 id="species-stages-title" className="mb-1">Fases no panorama</h2><p className="text-horta-muted">Prazos ilustrativos contados a partir do plantio. Cultivares podem ter seus próprios prazos.</p></div>
      {canManage && species.isActive && <a className="action-link secondary" href={samePage(route, species.id, route.search, 'fases')}>Configurar fases visuais</a>}
    </div>
    <ol className="m-0 mt-3 grid list-none gap-3 p-0 sm:grid-cols-3">{stages.map((stage, index) => <li key={stage.label} className="border-l-2 border-horta-line pl-4"><span className="text-xs font-semibold text-horta-muted">0{index + 1}</span><strong className="mt-1 block text-sm">{stage.label}</strong><span className="mt-1 block text-sm text-horta-muted">{stage.value}</span></li>)}</ol>
  </section>
}
