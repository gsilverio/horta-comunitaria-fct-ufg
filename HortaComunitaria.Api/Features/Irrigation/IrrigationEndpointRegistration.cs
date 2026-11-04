using HortaComunitaria.Api.Features.Irrigation.InvalidateIrrigation;
using HortaComunitaria.Api.Features.Irrigation.ListIrrigations;
using HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;
using HortaComunitaria.Api.Features.Irrigation.GetIrrigation;
using HortaComunitaria.Api.Features.Irrigation.CorrectIrrigation;
using HortaComunitaria.Api.Features.Irrigation.ListIrrigationHistory;

namespace HortaComunitaria.Api.Features.Irrigation;

internal static class IrrigationEndpointRegistration
{
    internal static RouteGroupBuilder MapIrrigationEndpoints(this RouteGroupBuilder group) =>
        group.MapRegisterIrrigationEndpoints().MapListIrrigationsEndpoints().MapListIrrigationHistoryEndpoints()
            .MapInvalidateIrrigationEndpoints().MapGetIrrigationEndpoints().MapCorrectIrrigationEndpoints();
}
