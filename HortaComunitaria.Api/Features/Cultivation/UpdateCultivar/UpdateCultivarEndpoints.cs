using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.UpdateCultivar;

internal static class UpdateCultivarEndpoints
{
    internal static RouteGroupBuilder MapUpdateCultivarEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/species/{speciesId:guid}/cultivars/{id:guid}", async (Guid speciesId, Guid id, UpdateCultivarRequest request, UpdateCultivarCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(speciesId, id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Cultivation.UpdateCultivar").WithTags("Cultivo e produção")
            .WithSummary("Editar cultivar").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivarResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
