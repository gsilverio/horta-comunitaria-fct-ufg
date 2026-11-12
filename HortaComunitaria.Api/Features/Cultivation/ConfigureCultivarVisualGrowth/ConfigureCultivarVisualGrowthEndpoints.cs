using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ConfigureCultivarVisualGrowth;
internal static class ConfigureCultivarVisualGrowthEndpoints
{
    internal static RouteGroupBuilder MapConfigureCultivarVisualGrowthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/species/{speciesId:guid}/cultivars/{id:guid}/visual-growth", async (Guid speciesId, Guid id, ConfigureCultivarVisualGrowthRequest request, ConfigureCultivarVisualGrowthCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(speciesId, id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Cultivation.ConfigureCultivarVisualGrowth").WithTags("Cultivo e produção")
            .WithSummary("Configurar prazos da ilustração de cultivar")
            .WithDescription("Prazos opcionais em dias após o plantio; configuração da cultivar prevalece sobre a espécie. Não confirma maturidade real. Ambos nulos removem a configuração. Exige versão e motivo.")
            .Produces<CultivarResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
