import { lazy, Suspense } from 'react'
import type { RecordRoute } from '../../../types/records'
import { routeHref } from '../../../hooks/useRoute'
const GardenPage = lazy(() => import('./GardenPage').then(module => ({ default: module.GardenPage })))
const IrrigationSectorPage = lazy(() => import('./IrrigationSectorPage').then(module => ({ default: module.IrrigationSectorPage })))
const BedPage = lazy(() => import('./BedPage').then(module => ({ default: module.BedPage })))

export function OrganizationPage(props: { route: RecordRoute; saved: (message: string) => void }) {
  const { route } = props
  const links = [
    { label: 'Visão geral', href: routeHref(route.garden, 'visao-geral', 'painel'), active: false },
    { label: 'Configurar horta', href: routeHref(route.garden, 'horta', 'interativa'), active: false },
    { label: 'Setores', href: routeHref(route.garden, 'horta', 'setores'), active: route.resource === 'setores' },
    { label: 'Canteiros', href: routeHref(route.garden, 'horta', 'canteiros'), active: route.resource === 'canteiros' },
    { label: 'Dados da horta', href: routeHref(route.garden, 'horta', 'hortas', route.garden), active: route.resource === 'hortas' },
  ]
  const cultivation = route.search.has('cultivate') || route.search.has('plantingCycle')
  return <div className={cultivation ? 'min-w-0' : 'organization-workspace'}>
    {route.garden && <nav aria-label="Gestão da horta" className="mb-8 flex flex-wrap gap-x-6 gap-y-1 border-b border-horta-line pb-3">
      {links.map(link => <a key={link.label} href={link.href} aria-current={link.active ? 'page' : undefined} className="inline-flex min-h-11 items-center border-b-2 border-transparent text-sm text-horta-muted hover:text-horta-forest aria-[current=page]:border-horta-forest aria-[current=page]:font-semibold aria-[current=page]:text-horta-forest">{link.label}</a>)}
    </nav>}
    <Suspense fallback={<p role="status">Carregando cadastro…</p>}>
    {props.route.resource === 'hortas' ? <GardenPage {...props} /> : props.route.resource === 'setores' ? <IrrigationSectorPage {...props} /> : <BedPage {...props} />}
    </Suspense>
  </div>
}
