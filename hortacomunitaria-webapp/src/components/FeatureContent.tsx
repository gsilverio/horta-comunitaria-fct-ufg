import { WorkflowPrerequisites } from '../features/workflows/pages/WorkflowPrerequisites'
import { PreparationPage } from '../features/organization/pages/PreparationPage'
import { lazy, Suspense } from "react"
const GardenPage = lazy(() => import("../features/organization/pages/GardenPage").then(module => ({ default: module.GardenPage })))
const IrrigationSectorPage = lazy(() => import("../features/organization/pages/IrrigationSectorPage").then(module => ({ default: module.IrrigationSectorPage })))
const BedPage = lazy(() => import("../features/organization/pages/BedPage").then(module => ({ default: module.BedPage })))
const EnvironmentalVariablePage = lazy(() => import("../features/environment/pages/EnvironmentalVariablePage").then(module => ({ default: module.EnvironmentalVariablePage })))
const SamplingPointPage = lazy(() => import("../features/environment/pages/SamplingPointPage").then(module => ({ default: module.SamplingPointPage })))
const EnvironmentalMeasurementPage = lazy(() => import("../features/environment/pages/EnvironmentalMeasurementPage").then(module => ({ default: module.EnvironmentalMeasurementPage })))
const SpeciesPage = lazy(() => import("../features/cultivation/pages/SpeciesPage").then(module => ({ default: module.SpeciesPage })))
const CultivarPage = lazy(() => import("../features/cultivation/pages/CultivarPage").then(module => ({ default: module.CultivarPage })))
const PropagationLotPage = lazy(() => import("../features/cultivation/pages/PropagationLotPage").then(module => ({ default: module.PropagationLotPage })))
const CultivationCyclePage = lazy(() => import("../features/cultivation/pages/CultivationCyclePage").then(module => ({ default: module.CultivationCyclePage })))
const PlantingPage = lazy(() => import("../features/cultivation/pages/PlantingPage").then(module => ({ default: module.PlantingPage })))
const EstablishmentAssessmentPage = lazy(() => import("../features/cultivation/pages/EstablishmentAssessmentPage").then(module => ({ default: module.EstablishmentAssessmentPage })))
const AgronomicObservationPage = lazy(() => import("../features/cultivation/pages/AgronomicObservationPage").then(module => ({ default: module.AgronomicObservationPage })))
const HarvestPage = lazy(() => import("../features/production/pages/HarvestPage").then(module => ({ default: module.HarvestPage })))
const DryingLotPage = lazy(() => import("../features/production/pages/DryingLotPage").then(module => ({ default: module.DryingLotPage })))
const ParticipantPage = lazy(() => import("../features/activities/pages/ParticipantPage").then(module => ({ default: module.ParticipantPage })))
const AgriculturalInputPage = lazy(() => import("../features/activities/pages/AgriculturalInputPage").then(module => ({ default: module.AgriculturalInputPage })))
const InstitutionPage = lazy(() => import("../features/activities/pages/InstitutionPage").then(module => ({ default: module.InstitutionPage })))
const ActivityPage = lazy(() => import("../features/activities/pages/ActivityPage").then(module => ({ default: module.ActivityPage })))
const OccurrencePage = lazy(() => import("../features/occurrences/pages/OccurrencePage").then(module => ({ default: module.OccurrencePage })))
const FinancialEntryPage = lazy(() => import("../features/costs/pages/FinancialEntryPage").then(module => ({ default: module.FinancialEntryPage })))
import type { RecordRoute } from "../types/records"
export function FeatureContent(props: { route: RecordRoute; saved: (message: string) => void }) { return <Suspense fallback={<p role="status">Carregando área…</p>}><WorkflowPrerequisites key={`${props.route.garden}/${props.route.module}/${props.route.resource}/${props.route.id}`} route={props.route}><ResourceContent {...props} /></WorkflowPrerequisites></Suspense> }
function ResourceContent({ route, saved }: { route: RecordRoute; saved: (message: string) => void }) { switch (route.module + "/" + route.resource) {
case "horta/preparacao": return <PreparationPage route={route} />
case "horta/hortas": return <GardenPage route={route} saved={saved} />
case "horta/setores": return <IrrigationSectorPage route={route} saved={saved} />
case "horta/canteiros": return <BedPage route={route} saved={saved} />
case "ambiente/variaveis": return <EnvironmentalVariablePage route={route} saved={saved} />
case "ambiente/pontos": return <SamplingPointPage route={route} saved={saved} />
case "ambiente/medicoes": return <EnvironmentalMeasurementPage route={route} saved={saved} />
case "cultivo/especies": return <SpeciesPage route={route} saved={saved} />
case "cultivo/cultivares": return <CultivarPage route={route} saved={saved} />
case "cultivo/origens": return <PropagationLotPage route={route} saved={saved} />
case "cultivo/ciclos": return <CultivationCyclePage route={route} saved={saved} />
case "cultivo/plantios": return <PlantingPage route={route} saved={saved} />
case "cultivo/avaliacoes": return <EstablishmentAssessmentPage route={route} saved={saved} />
case "cultivo/observacoes": return <AgronomicObservationPage route={route} saved={saved} />
case "producao/colheitas": return <HarvestPage route={route} saved={saved} />
case "producao/secagens": return <DryingLotPage route={route} saved={saved} />
case "atividades/participantes": return <ParticipantPage route={route} saved={saved} />
case "atividades/insumos": return <AgriculturalInputPage route={route} saved={saved} />
case "atividades/instituicoes": return <InstitutionPage route={route} saved={saved} />
case "atividades/registros": return <ActivityPage route={route} saved={saved} />
case "ocorrencias/registros": return <OccurrencePage route={route} saved={saved} />
case "custos/registros": return <FinancialEntryPage route={route} saved={saved} />
default: return <section className="panel"><h2>Destino não encontrado</h2><p>Escolha uma área disponível na navegação.</p></section> } }
