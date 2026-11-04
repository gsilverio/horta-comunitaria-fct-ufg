using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetCultivar;

internal static class GetCultivarEndpoints
{
    internal static RouteGroupBuilder MapGetCultivarEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species/{speciesId:guid}/cultivars/{id:guid}", async (Guid speciesId, Guid id, GetCultivarQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(speciesId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetCultivar").WithTags("Cultivo e produção")
            .WithSummary("Consultar cultivar").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivarResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
