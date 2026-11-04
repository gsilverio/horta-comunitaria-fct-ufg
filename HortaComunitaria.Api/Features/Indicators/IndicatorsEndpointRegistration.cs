using HortaComunitaria.Api.Features.Indicators.GetOperationalOccurrences;
using HortaComunitaria.Api.Features.Indicators.GetSoilMoisture;
using HortaComunitaria.Api.Features.Indicators.GetWorkHours;
using HortaComunitaria.Api.Features.Indicators.GetDryingYield;
using HortaComunitaria.Api.Features.Indicators.GetSocialEducationalParticipation;
using HortaComunitaria.Api.Features.Indicators.GetBasicCosts;
using HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;
using HortaComunitaria.Api.Features.Indicators.GetSeedlingEstablishment;
using HortaComunitaria.Api.Features.Indicators.GetProductivity;
using HortaComunitaria.Api.Features.Indicators.GetHarvestLosses;

namespace HortaComunitaria.Api.Features.Indicators;

internal static class IndicatorsEndpointRegistration
{
    internal static RouteGroupBuilder MapIndicatorsEndpoints(this RouteGroupBuilder group) => group.MapGetWaterConsumptionEndpoints().MapGetSoilMoistureEndpoints().MapGetHarvestLossesEndpoints().MapGetProductivityEndpoints().MapGetDryingYieldEndpoints().MapGetBasicCostsEndpoints().MapGetWorkHoursEndpoints().MapGetOperationalOccurrencesEndpoints().MapGetSocialEducationalParticipationEndpoints().MapGetSeedlingEstablishmentEndpoints();
}
