using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.UpdateSpecies;

internal static class UpdateSpeciesEndpoints
{
    internal static RouteGroupBuilder MapUpdateSpeciesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/species/{id:guid}", async (Guid id, UpdateSpeciesRequest request, UpdateSpeciesCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.UpdateSpecies").WithTags("Cultivo e produção")
            .WithSummary("Editar espécie").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SpeciesResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
