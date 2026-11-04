using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CreateCultivar;

internal static class CreateCultivarEndpoints
{
    internal static RouteGroupBuilder MapCreateCultivarEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/species/{speciesId:guid}/cultivars", async (Guid speciesId, CreateCultivarRequest request, CreateCultivarCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(speciesId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/species/{speciesId}/cultivars/{value.Id}", value)))
            .WithName("Cultivation.CreateCultivar").WithTags("Cultivo e produção")
            .WithSummary("Cadastrar cultivar").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivarResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
