using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CreateSpecies;

internal static class CreateSpeciesEndpoints
{
    internal static RouteGroupBuilder MapCreateSpeciesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/species", async (CreateSpeciesRequest request, CreateSpeciesCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/species/{value.Id}", value)))
            .WithName("Cultivation.CreateSpecies").WithTags("Cultivo e produção")
            .WithSummary("Cadastrar espécie").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SpeciesResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
