import { useState } from 'react'
import { Icon } from '../../../components/Icon'
import { useSpeciesImage } from '../../interactive-garden/hooks/useSpeciesCatalog'
import type { Species } from '../types/Species'

type CropImageState = ReturnType<typeof useSpeciesImage>
interface CropCatalogEntryProps {
  name: string
  href: string
  image: CropImageState
  scientificName?: string | null
  active: boolean
  speciesPhoto?: boolean
}

export function CropPhoto({ name, image, large = false, speciesPhoto = false }: { name: string; image: CropImageState; large?: boolean; speciesPhoto?: boolean }) {
  const [failedSource, setFailedSource] = useState<string>()
  const source = image.data ? `data:${image.data.mimeType};base64,${image.data.base64}` : undefined
  const unavailable = !!image.error || !!source && source === failedSource
  const fallback = image.loading ? 'Carregando imagem' : unavailable ? 'Imagem indisponível' : 'Sem imagem cadastrada'
  return <span className={`flex shrink-0 items-center justify-center overflow-hidden rounded-md bg-horta-soft text-horta-muted ${large ? 'aspect-square w-full' : 'size-12'}`} aria-busy={image.loading}>
    {source && !unavailable ? <img src={source} width={large ? 320 : 64} height={large ? 320 : 64} loading={large ? 'eager' : 'lazy'} className="size-full object-cover" alt={speciesPhoto ? `Imagem ilustrativa da espécie de ${name}` : `Imagem ilustrativa de ${name}`} onError={() => setFailedSource(source)} /> : <span className="flex flex-col items-center gap-3 p-3 text-center" title={fallback}><Icon name="cultivo" className={large ? 'size-12' : 'size-6'} /><span className={large ? 'text-sm' : 'sr-only'}>{fallback}</span></span>}
  </span>
}

export function CropCatalogEntry({ name, href, image, scientificName, active, speciesPhoto = false }: CropCatalogEntryProps) {
  return <li className="!p-0">
    <a className="group flex min-w-0 items-center gap-3 px-2 py-2.5 no-underline" href={href} aria-label={name}>
      <CropPhoto name={name} image={image} speciesPhoto={speciesPhoto} />
      <span className="min-w-0 flex-1">
        <span className="block text-sm font-semibold break-words group-hover:underline">{name}</span>
        {scientificName && <span className="block text-[13px] font-normal text-horta-muted italic">{scientificName}</span>}
        {!active && <span className="block text-xs font-normal text-horta-muted">Cadastro inativo</span>}
      </span>
      <Icon name="chevron" className="size-4 text-horta-muted transition-transform group-hover:translate-x-1" />
    </a>
  </li>
}

export function SpeciesCatalogEntry({ species, href }: { species: Species; href: string }) {
  const image = useSpeciesImage(species.id, !!species.imageUrl)
  return <CropCatalogEntry name={species.commonName} scientificName={species.scientificName} active={species.isActive} href={href} image={image} />
}
