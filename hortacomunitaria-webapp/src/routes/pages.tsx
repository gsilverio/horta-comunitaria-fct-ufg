import { lazy, Suspense } from 'react'
import type { ComponentType, ReactNode } from 'react'
import { WorkflowPrerequisites } from '../features/workflows/pages/WorkflowPrerequisites'
import { GardenFlow } from '../features/irrigation/pages/IrrigationPage'
import { InteractiveGardenPage } from '../features/interactive-garden/pages/InteractiveGardenPage'
import type { Garden } from '../features/organization/types/Garden'
import type { AppRoute, PageKey } from './routes'
import { changesManagedRecord, isManagedPage, samePage } from './routes'
import { ManageAccess, useAuth } from '../features/auth/hooks/useAuth'
import { TerrainGate } from '../features/organization/components/TerrainGate'

/** Dados que o App fornece às páginas. */
export interface PageContext {
  route: AppRoute
  garden?: Garden
  saved: (message: string) => void
  /** Irrigação: bloqueia a troca de horta enquanto um registro está aberto. */
  modeChanged: (busy: boolean) => void
}

type PageProps = { route: AppRoute; saved: (message: string) => void }
function load<T extends string>(loader: () => Promise<Record<T, ComponentType<PageProps>>>, name: T) {
  return lazy(() => loader().then(module => ({ default: module[name] })))
}

const GardenPage = load(() => import('../features/organization/pages/GardenPage'), 'GardenPage')
const GardenDataPage = load(() => import('../features/organization/pages/GardenPage'), 'GardenDataPage')
const SectorPage = load(() => import('../features/organization/pages/IrrigationSectorPage'), 'IrrigationSectorPage')
const BedPage = load(() => import('../features/organization/pages/BedPage'), 'BedPage')
const OverviewPage = load(() => import('../features/indicators/pages/OverviewPage'), 'OverviewPage')
const WaterPage = load(() => import('../features/indicators/pages/WaterPage'), 'WaterPage')
const SoilMoisturePage = load(() => import('../features/indicators/pages/GetSoilMoisturePage'), 'GetSoilMoisturePage')
const SeedlingEstablishmentPage = load(() => import('../features/indicators/pages/GetSeedlingEstablishmentPage'), 'GetSeedlingEstablishmentPage')
const ProductivityPage = load(() => import('../features/indicators/pages/GetProductivityPage'), 'GetProductivityPage')
const HarvestLossesPage = load(() => import('../features/indicators/pages/GetHarvestLossesPage'), 'GetHarvestLossesPage')
const DryingYieldPage = load(() => import('../features/indicators/pages/GetDryingYieldPage'), 'GetDryingYieldPage')
const WorkHoursPage = load(() => import('../features/indicators/pages/GetWorkHoursPage'), 'GetWorkHoursPage')
const OccurrencesIndicatorPage = load(() => import('../features/indicators/pages/GetOperationalOccurrencesPage'), 'GetOperationalOccurrencesPage')
const ParticipationPage = load(() => import('../features/indicators/pages/GetSocialEducationalParticipationPage'), 'GetSocialEducationalParticipationPage')
const BasicCostsPage = load(() => import('../features/indicators/pages/GetBasicCostsPage'), 'GetBasicCostsPage')
const EnvironmentalVariablePage = load(() => import('../features/environment/pages/EnvironmentalVariablePage'), 'EnvironmentalVariablePage')
const SamplingPointPage = load(() => import('../features/environment/pages/SamplingPointPage'), 'SamplingPointPage')
const EnvironmentalMeasurementPage = load(() => import('../features/environment/pages/EnvironmentalMeasurementPage'), 'EnvironmentalMeasurementPage')
const SpeciesPage = load(() => import('../features/cultivation/pages/SpeciesPage'), 'SpeciesPage')
const CultivarPage = load(() => import('../features/cultivation/pages/CultivarPage'), 'CultivarPage')
const PropagationLotPage = load(() => import('../features/cultivation/pages/PropagationLotPage'), 'PropagationLotPage')
const CultivationCyclePage = load(() => import('../features/cultivation/pages/CultivationCyclePage'), 'CultivationCyclePage')
const PlantingPage = load(() => import('../features/cultivation/pages/PlantingPage'), 'PlantingPage')
const EstablishmentAssessmentPage = load(() => import('../features/cultivation/pages/EstablishmentAssessmentPage'), 'EstablishmentAssessmentPage')
const AgronomicObservationPage = load(() => import('../features/cultivation/pages/AgronomicObservationPage'), 'AgronomicObservationPage')
const HarvestPage = load(() => import('../features/production/pages/HarvestPage'), 'HarvestPage')
const DryingLotPage = load(() => import('../features/production/pages/DryingLotPage'), 'DryingLotPage')
const ParticipantPage = load(() => import('../features/activities/pages/ParticipantPage'), 'ParticipantPage')
const AgriculturalInputPage = load(() => import('../features/activities/pages/AgriculturalInputPage'), 'AgriculturalInputPage')
const InstitutionPage = load(() => import('../features/activities/pages/InstitutionPage'), 'InstitutionPage')
const ActivityPage = load(() => import('../features/activities/pages/ActivityPage'), 'ActivityPage')
const OccurrencePage = load(() => import('../features/occurrences/pages/OccurrencePage'), 'OccurrencePage')
const FinancialEntryPage = load(() => import('../features/costs/pages/FinancialEntryPage'), 'FinancialEntryPage')
const EvidenceGalleryPage = load(() => import('../features/evidence/pages/EvidenceGalleryPage'), 'EvidenceGalleryPage')

const simple = (Page: ComponentType<PageProps>) => ({ route, saved }: PageContext) => <Page route={route} saved={saved} />
// Setores e canteiros exigem o terreno desenhado no mapa (ver TerrainGate).
const afterTerrain = (Page: ComponentType<PageProps>, label: 'Setores' | 'Canteiros') => ({ route, saved }: PageContext) =>
  <TerrainGate key={route.garden} garden={route.garden} page={label}><Page route={route} saved={saved} /></TerrainGate>

/** Componente de cada página da tabela de rotas. */
const pages: Record<PageKey, (context: PageContext) => ReactNode> = {
  'hortas': simple(GardenPage),
  'visao-geral': simple(OverviewPage),
  'visao-geral/agua': simple(WaterPage),
  'visao-geral/umidade': simple(SoilMoisturePage),
  'visao-geral/pegamento': simple(SeedlingEstablishmentPage),
  'visao-geral/produtividade': simple(ProductivityPage),
  'visao-geral/perdas': simple(HarvestLossesPage),
  'visao-geral/secagem': simple(DryingYieldPage),
  'visao-geral/horas': simple(WorkHoursPage),
  'visao-geral/ocorrencias': simple(OccurrencesIndicatorPage),
  'visao-geral/participacao': simple(ParticipationPage),
  'visao-geral/custos': simple(BasicCostsPage),
  'horta/mapa': ({ route, garden }) => garden ? <InteractiveGardenPage key={garden.id} route={route} /> : null,
  'horta/setores': afterTerrain(SectorPage, 'Setores'),
  'horta/canteiros': afterTerrain(BedPage, 'Canteiros'),
  'horta/dados': simple(GardenDataPage),
  'irrigacao': ({ route, garden, modeChanged, saved }) => garden ? <GardenFlow key={`${garden.id}/${route.id}/${route.search}`} garden={garden} route={route} modeChanged={modeChanged} onSaved={saved} /> : null,
  'ambiente/variaveis': simple(EnvironmentalVariablePage),
  'ambiente/pontos': simple(SamplingPointPage),
  'ambiente/medicoes': simple(EnvironmentalMeasurementPage),
  'catalogo/especies': simple(SpeciesPage),
  'catalogo/cultivares': simple(CultivarPage),
  'cultivo/origens': simple(PropagationLotPage),
  'cultivo/ciclos': simple(CultivationCyclePage),
  'cultivo/plantios': simple(PlantingPage),
  'cultivo/avaliacoes': simple(EstablishmentAssessmentPage),
  'cultivo/observacoes': simple(AgronomicObservationPage),
  'producao/colheitas': simple(HarvestPage),
  'producao/secagens': simple(DryingLotPage),
  'fotos': simple(EvidenceGalleryPage),
  'atividades/registros': simple(ActivityPage),
  'participantes': simple(ParticipantPage),
  'atividades/insumos': simple(AgriculturalInputPage),
  'atividades/instituicoes': simple(InstitutionPage),
  'ocorrencias': simple(OccurrencePage),
  'custos': simple(FinancialEntryPage),
}

/** Página da rota atual. Trocar de horta, página ou registro remonta o conteúdo; a ação (ex.: editar) preserva o estado carregado. */
export function PageContent(context: PageContext) {
  const { route } = context
  const { isAdministrator } = useAuth()
  if (!route.page) return <NotFound />
  const canManage = isAdministrator || !isManagedPage(route.page)
  if (!canManage && changesManagedRecord(route)) return <Forbidden back={samePage(route, route.id === 'novo' || route.id === 'nova' ? '' : route.id, route.search)} />
  return <ManageAccess.Provider value={canManage}><Suspense fallback={<p role="status">Carregando área…</p>}>
    <WorkflowPrerequisites key={`${route.garden}|${route.page}|${route.id}`} route={route}>{pages[route.page](context)}</WorkflowPrerequisites>
  </Suspense></ManageAccess.Provider>
}

export function NotFound() {
  return <section className="panel"><h1 tabIndex={-1}>Página não encontrada</h1><p>O endereço não corresponde a uma área da aplicação. Escolha um destino na navegação.</p><a className="action-link" href="/hortas">Ir para as hortas</a></section>
}

/** Ação de estrutura/catálogo aberta por endereço direto com perfil Operador. */
export function Forbidden({ back }: { back: string }) {
  return <section className="panel"><h1 tabIndex={-1}>Ação restrita ao perfil Adm</h1><p>Seu perfil pode consultar estes dados e registrar fatos de campo. Cadastros de estrutura, catálogos e usuários são mantidos pela administração.</p><a className="action-link secondary" href={back}>Voltar</a></section>
}
