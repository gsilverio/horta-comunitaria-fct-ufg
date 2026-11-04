using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetSpecies;

internal static class GetSpeciesEndpoints
{
    internal static RouteGroupBuilder MapGetSpeciesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species/{id:guid}", async (Guid id, GetSpeciesQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetSpecies").WithTags("Cultivo e produção")
            .WithSummary("Consultar espécie").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SpeciesResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
