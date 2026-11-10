import { useState } from 'react'
import { Failure, Pagination } from '../../../components/Feedback'
import { useGardenState } from '../../../hooks/useGardenState'
import { EvidenceGrid } from '../components/EvidenceGrid'
import { EvidenceUploadForm } from '../components/EvidenceUploadForm'
import { EvidenceViewer } from '../components/EvidenceViewer'
import { useEvidenceGallery } from '../hooks/useEvidenceGallery'
import type { Evidence, EvidenceTarget } from '../types/Evidence'

/** Aba "Fotos" de um registro: envio, galeria e visualização ampliada. Registros invalidados só exibem as fotos existentes. */
export function EvidenceSection({ garden, targetType, targetId, active }: { garden: string; targetType: EvidenceTarget; targetId: string; active: boolean }) {
  const gardenState = useGardenState()
  const gallery = useEvidenceGallery(garden, { targetType, targetId, validity: 'Valid' })
  const [open, setOpen] = useState<Evidence>()
  const canChange = active && gardenState.isActive
  const items = gallery.list.data?.items ?? []
  return <div className="panel grid gap-5">
    {canChange ? <EvidenceUploadForm busy={gallery.uploading} progress={gallery.progress} error={gallery.uploadError} submit={gallery.upload} clearError={gallery.clearUploadError} />
      : <p className="notice">Registro invalidado ou inativo: novas fotos não podem ser vinculadas.</p>}
    {gallery.notice && <p className="feedback success" role="status">{gallery.notice}</p>}
    <section aria-labelledby={`fotos-${targetId}`} className="grid gap-3">
      <h2 id={`fotos-${targetId}`} className="text-base font-semibold">Fotografias {gallery.list.data && <span className="font-normal text-horta-muted">({gallery.list.data.totalCount})</span>}</h2>
      {gallery.list.loading && <p role="status">Carregando fotografias…</p>}
      {gallery.list.error && <Failure error={gallery.list.error} retry={gallery.reload} />}
      {gallery.list.data && items.length === 0 && <p className="text-sm text-horta-muted">Nenhuma fotografia vinculada a este registro.</p>}
      {items.length > 0 && <EvidenceGrid items={items} imageUrl={gallery.imageUrl} open={item => { gallery.clearChangeError(); setOpen(item) }} />}
      {gallery.list.data && <Pagination data={gallery.list.data} change={gallery.setPage} label="Páginas de fotografias" />}
    </section>
    {open && <EvidenceViewer key={open.id} item={open} imageUrl={gallery.imageUrl(open.id)} canChange={canChange} busy={gallery.changing} error={gallery.changeError} clearError={gallery.clearChangeError}
      correct={async (values, reason) => !!await gallery.correct(open, values, reason)} invalidate={async reason => !!await gallery.invalidate(open, reason)} close={() => setOpen(undefined)} />}
  </div>
}
