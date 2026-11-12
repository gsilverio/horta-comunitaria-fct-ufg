using HortaComunitaria.Api.Features.Occurrences;
using HortaComunitaria.Api.Features.Costs;
using HortaComunitaria.Api.Features.Environment;
using HortaComunitaria.Api.Features.Cultivation;
using HortaComunitaria.Api.Features.Gardens;
using HortaComunitaria.Api.Features.Activities;
using HortaComunitaria.Api.Features.Irrigation;
using HortaComunitaria.Api.Features.Indicators;
using HortaComunitaria.Api.Features.Evidence;
using HortaComunitaria.Api.Features.Auth;
using HortaComunitaria.Api.Features.Users;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        // Toda rota exige autenticação (fallback policy); a senha inicial pendente bloqueia o restante da API.
        var v1 = app.MapGroup("/api/v1").AddEndpointFilter<PasswordChangeRequiredFilter>();
        v1.MapAuthEndpoints();
        v1.MapUsersEndpoints();
        var group = v1.MapGroup("/gardens");
        group.MapGardensEndpoints();
        group.MapActivitiesEndpoints();
        group.MapIrrigationEndpoints();
        group.MapIndicatorsEndpoints();
        v1.MapCultivationGlobalEndpoints();
        group.MapCultivationEndpoints();
        v1.MapEnvironmentGlobalEndpoints();
        group.MapEnvironmentEndpoints();
        v1.MapActivitiesGlobalEndpoints();
        group.MapCostsEndpoints();
        group.MapOccurrencesEndpoints();
        group.MapEvidenceEndpoints();
        return app;
    }
}
