using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InactivateCultivar;

internal static class InactivateCultivarEndpoints
{
    internal static RouteGroupBuilder MapInactivateCultivarEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/species/{speciesId:guid}/cultivars/{id:guid}/inactivation", async (Guid speciesId, Guid id, InactivateCultivarRequest request, InactivateCultivarCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(speciesId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InactivateCultivar").WithTags("Cultivo e produção")
            .WithSummary("Inativar cultivar").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivarResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
