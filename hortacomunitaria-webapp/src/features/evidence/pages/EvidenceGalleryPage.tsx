import { linkTo } from '../../../routes/routes'
import type { AppRoute } from '../../../routes/routes'
import { useState } from 'react'
import { FilterFields, SelectField } from '../../../components/Fields'
import { Failure, Pagination } from '../../../components/Feedback'
import { PageHeader } from '../../../components/PageHeader'
import { useGardenState } from '../../../hooks/useGardenState'

import { EvidenceGrid } from '../components/EvidenceGrid'
import { EvidenceViewer } from '../components/EvidenceViewer'
import { useEvidenceGallery } from '../hooks/useEvidenceGallery'
import type { Evidence, EvidenceTarget } from '../types/Evidence'
import { evidenceTargets } from '../types/Evidence'

const targetOptions = (Object.keys(evidenceTargets) as EvidenceTarget[]).map(id => ({ id, label: evidenceTargets[id].label }))
const isTarget = (value: string | null): value is EvidenceTarget => !!value && value in evidenceTargets

/** Galeria geral da horta. O envio acontece no registro (aba Fotos), para que toda foto tenha um vínculo concreto. */
export function EvidenceGalleryPage({ route }: { route: AppRoute }) {
  const garden = useGardenState()
  const requested = route.search.get('targetType')
  const [targetType, setTargetType] = useState<EvidenceTarget | ''>(isTarget(requested) ? requested : '')
  const [validity, setValidity] = useState<'Valid' | 'Invalid' | ''>('Valid')
  const gallery = useEvidenceGallery(route.garden, { targetType: targetType || undefined, validity: validity || undefined }, 24)
  const [open, setOpen] = useState<Evidence>()
  const items = gallery.list.data?.items ?? []
  const filter = (apply: () => void) => { apply(); gallery.setPage(1) }
  return <>
    <PageHeader title="Fotografias" description="Registros fotográficos de campo, mais recentes primeiro. Para enviar, abra o canteiro, ciclo, plantio, observação, colheita, atividade ou ocorrência e use a aba Fotos." />
    <FilterFields><div className="mb-4 grid gap-3 sm:grid-cols-2 lg:max-w-2xl">
      <SelectField name="targetType" label="Tipo de registro" value={targetType} change={value => filter(() => setTargetType(isTarget(value) ? value : ''))} options={targetOptions} />
      <SelectField name="validity" label="Validade" value={validity} change={value => filter(() => setValidity(value === 'Valid' || value === 'Invalid' ? value : ''))} options={[{ id: 'Valid', label: 'Válida' }, { id: 'Invalid', label: 'Invalidada' }]} />
    </div></FilterFields>
    {gallery.list.loading && <p role="status">Carregando fotografias…</p>}
    {gallery.list.error && <Failure error={gallery.list.error} retry={gallery.reload} />}
    {gallery.list.data && <p className="mb-3 text-sm text-horta-muted" aria-live="polite">{gallery.list.data.totalCount} fotografia(s) no filtro atual.</p>}
    {gallery.list.data && items.length === 0 && <p className="panel text-sm text-horta-muted">Nenhuma fotografia encontrada. Abra um registro e use a aba Fotos para enviar a primeira.</p>}
    {items.length > 0 && <EvidenceGrid items={items} imageUrl={gallery.imageUrl} showTarget open={item => { gallery.clearChangeError(); setOpen(item) }} />}
    {gallery.list.data && <div className="mt-4"><Pagination data={gallery.list.data} change={gallery.setPage} label="Páginas da galeria" /></div>}
    {open && <EvidenceViewer key={open.id} item={open} imageUrl={gallery.imageUrl(open.id)} canChange={garden.isActive}
      targetHref={linkTo(route.garden, evidenceTargets[open.targetType].page, open.targetId, new URLSearchParams({ tab: 'fotos' }))}
      busy={gallery.changing} error={gallery.changeError} clearError={gallery.clearChangeError}
      correct={async (values, reason) => !!await gallery.correct(open, values, reason)} invalidate={async reason => !!await gallery.invalidate(open, reason)} close={() => setOpen(undefined)} />}
  </>
}
