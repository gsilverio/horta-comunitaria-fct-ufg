using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ConfigureSpeciesVisualGrowth;
internal static class ConfigureSpeciesVisualGrowthEndpoints
{
    internal static RouteGroupBuilder MapConfigureSpeciesVisualGrowthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/species/{id:guid}/visual-growth", async (Guid id, ConfigureSpeciesVisualGrowthRequest request, ConfigureSpeciesVisualGrowthCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Cultivation.ConfigureSpeciesVisualGrowth").WithTags("Cultivo e produção")
            .WithSummary("Configurar prazos da ilustração de espécie")
            .WithDescription("Prazos opcionais em dias após o plantio; configuração da cultivar prevalece sobre a espécie. Não confirma maturidade real. Ambos nulos removem a configuração. Exige versão e motivo.")
            .Produces<SpeciesResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
