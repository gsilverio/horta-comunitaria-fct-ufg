import { useCallback } from 'react'
import { useRemote } from '../../../hooks/useRemote'
import { useGardenState } from '../../../hooks/useGardenState'
import { routeHref } from '../../../hooks/useRoute'
import type { RecordRoute } from '../../../types/records'
import type { IrrigationSector } from '../types/IrrigationSector'
import { useOrganizationList } from '../hooks/useOrganizationList'
import { organizationBrowseApi } from '../services/organizationBrowseApi'
import { CreateLink, OrganizationCollection, OrganizationHeading, OrganizationRow, OrganizationTrail } from '../components/OrganizationLayout'
import { formatNumber, today } from '../../irrigation/formValues'

export function SectorBeds({ route, sector }: { route: RecordRoute; sector: IrrigationSector }) {
  const garden = useGardenState()
  const { page, revision, retry, changePage } = useOrganizationList(route)
  const on = today()
  const load = useCallback((signal: AbortSignal) => organizationBrowseApi.sectorBeds(route.garden, sector.id, page, on, signal), [route.garden, sector.id, page, on])
  const result = useRemote(`${route.garden}/${sector.id}/${page}/${revision}`, load)
  return <><OrganizationTrail sector={sector} /><OrganizationHeading eyebrow={garden.name} title={sector.name} description={sector.description || 'Os canteiros deste setor ficam reunidos aqui.'} action={garden.isActive && sector.isActive && <CreateLink href={routeHref(route.garden, 'horta', 'canteiros', 'novo', new URLSearchParams({ sectorId: sector.id }))}>Novo canteiro</CreateLink>} />
    {sector.isActive ? <OrganizationCollection title="Canteiros do setor" {...result} retry={retry} changePage={changePage} emptyTitle="Este setor ainda não tem canteiros" emptyText="Cadastre um canteiro e confirme a área e o setor para concluir sua organização. A lista mostra as associações vigentes hoje.">
      {result.data?.items.map(bed => <OrganizationRow key={bed.id} title={bed.name} description={bed.configuration ? `${formatNumber(bed.configuration.cultivableAreaM2)} m² de área cultivável${bed.location ? ' · ' + bed.location : ''}` : bed.location || 'Informe a área para completar este canteiro.'} status={!bed.configuration ? 'Área pendente' : undefined} href={routeHref(route.garden, 'horta', 'canteiros', bed.id, new URLSearchParams({ sectorId: sector.id }))} action="Ver canteiro" />)}
    </OrganizationCollection> : <p className="notice">Este setor está inativo. Consulte os canteiros e seus vínculos pelo catálogo da horta.</p>}
    <div className="organization-secondary-links"><a href={routeHref(route.garden, 'horta', 'setores')}>← Voltar aos setores</a><a href={routeHref(route.garden, 'horta', 'canteiros')}>Todos os canteiros e cadastros pendentes</a></div>
  </>
}
