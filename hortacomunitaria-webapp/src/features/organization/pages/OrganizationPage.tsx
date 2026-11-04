import { lazy, Suspense } from 'react'
import type { RecordRoute } from '../../../types/records'
const GardenPage = lazy(() => import('./GardenPage').then(module => ({ default: module.GardenPage })))
const IrrigationSectorPage = lazy(() => import('./IrrigationSectorPage').then(module => ({ default: module.IrrigationSectorPage })))
const BedPage = lazy(() => import('./BedPage').then(module => ({ default: module.BedPage })))

export function OrganizationPage(props: { route: RecordRoute; saved: (message: string) => void }) {
  return <Suspense fallback={<p role="status">Carregando cadastro…</p>}>
    {props.route.resource === 'hortas' ? <GardenPage {...props} /> : props.route.resource === 'setores' ? <IrrigationSectorPage {...props} /> : <BedPage {...props} />}
  </Suspense>
}
