import { formatDate } from '../../../services/dates'
import { formatInstant } from '../../irrigation/formValues'
import type { Evidence } from '../types/Evidence'
import { useEvidenceImage } from '../hooks/useEvidenceImage'
import { evidenceTargets } from '../types/Evidence'

/** Grade de miniaturas; cada miniatura é um botão que abre a visualização ampliada. */
export function EvidenceGrid({ items, imageUrl, open, showTarget = false }: { items: Evidence[]; imageUrl: (id: string) => string; open: (item: Evidence) => void; showTarget?: boolean }) {
  return <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4" aria-label="Fotografias">
    {items.map(item => <li key={item.id} className="min-w-0">
      <button type="button" onClick={() => open(item)} className="group grid w-full min-h-11 gap-0 border border-horta-line bg-white p-0 text-left text-horta-ink hover:border-horta-forest focus-visible:outline-2 focus-visible:outline-horta-focus" aria-label={`Abrir fotografia: ${item.title}`}>
        <span className="relative block aspect-[4/3] overflow-hidden bg-horta-canvas">
          <Thumbnail path={imageUrl(item.id)} />
          {item.validity === 'Invalid' && <span className="badge invalid absolute left-2 top-2">Invalidada</span>}
        </span>
        <span className="grid gap-0.5 p-2">
          <strong className="truncate text-sm font-medium">{item.title}</strong>
          <span className="truncate text-xs text-horta-muted">{showTarget ? `${evidenceTargets[item.targetType].label} · ` : ''}{item.capturedOn ? formatDate(item.capturedOn) : `Enviada ${formatInstant(item.recordedAt)}`}</span>
        </span>
      </button>
    </li>)}
  </ul>
}

function Thumbnail({ path }: { path: string }) {
  const image = useEvidenceImage(path)
  if (!image.url) return <span className="flex h-full w-full items-center justify-center text-xs text-horta-muted">{image.failed ? 'Imagem indisponível' : 'Carregando…'}</span>
  return <img src={image.url} alt="" decoding="async" className="h-full w-full object-cover transition-transform duration-200 group-hover:scale-[1.02] motion-reduce:transition-none" />
}
