using HortaComunitaria.Api.Features.Occurrences;
using HortaComunitaria.Api.Features.Costs;
using HortaComunitaria.Api.Features.Environment;
using HortaComunitaria.Api.Features.Cultivation;
using HortaComunitaria.Api.Features.Gardens;
using HortaComunitaria.Api.Features.Activities;
using HortaComunitaria.Api.Features.Irrigation;
using HortaComunitaria.Api.Features.Indicators;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gardens");
        group.MapGardensEndpoints();
        group.MapActivitiesEndpoints();
        group.MapIrrigationEndpoints();
        group.MapIndicatorsEndpoints();
        app.MapGroup("/api/v1").MapCultivationGlobalEndpoints();
        group.MapCultivationEndpoints();
        app.MapGroup("/api/v1").MapEnvironmentGlobalEndpoints();
        group.MapEnvironmentEndpoints();
        app.MapGroup("/api/v1").MapActivitiesGlobalEndpoints();
        group.MapCostsEndpoints();
        group.MapOccurrencesEndpoints();
        return app;
    }
}
