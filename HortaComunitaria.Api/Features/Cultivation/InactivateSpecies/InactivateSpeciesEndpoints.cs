using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InactivateSpecies;

internal static class InactivateSpeciesEndpoints
{
    internal static RouteGroupBuilder MapInactivateSpeciesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/species/{id:guid}/inactivation", async (Guid id, InactivateSpeciesRequest request, InactivateSpeciesCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Cultivation.InactivateSpecies").WithTags("Cultivo e produção")
            .WithSummary("Inativar espécie").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SpeciesResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
