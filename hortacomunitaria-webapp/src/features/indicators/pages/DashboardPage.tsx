import { OverviewPage } from './OverviewPage'
import { GetSoilMoisturePage } from "./GetSoilMoisturePage"
import { GetSeedlingEstablishmentPage } from "./GetSeedlingEstablishmentPage"
import { GetProductivityPage } from "./GetProductivityPage"
import { GetHarvestLossesPage } from "./GetHarvestLossesPage"
import { GetDryingYieldPage } from "./GetDryingYieldPage"
import { GetWorkHoursPage } from "./GetWorkHoursPage"
import { GetOperationalOccurrencesPage } from "./GetOperationalOccurrencesPage"
import { GetSocialEducationalParticipationPage } from "./GetSocialEducationalParticipationPage"
import { GetBasicCostsPage } from "./GetBasicCostsPage"
import { WaterPage } from "./WaterPage"
import type { RecordRoute } from "../../../types/records"
export function DashboardPage({ route }: { route: RecordRoute }) { switch(route.resource) {
case 'painel': return <OverviewPage route={route} />
case 'soil-moisture': return <GetSoilMoisturePage route={route} />
case 'seedling-establishment': return <GetSeedlingEstablishmentPage route={route} />
case 'productivity': return <GetProductivityPage route={route} />
case 'harvest-losses': return <GetHarvestLossesPage route={route} />
case 'drying-yield': return <GetDryingYieldPage route={route} />
case 'work-hours': return <GetWorkHoursPage route={route} />
case 'operational-occurrences': return <GetOperationalOccurrencesPage route={route} />
case 'social-educational-participation': return <GetSocialEducationalParticipationPage route={route} />
case 'basic-costs': return <GetBasicCostsPage route={route} />
case "water-consumption": return <WaterPage route={route} />
default: return <p>Indicador não encontrado. Escolha um indicador na navegação.</p> } }
