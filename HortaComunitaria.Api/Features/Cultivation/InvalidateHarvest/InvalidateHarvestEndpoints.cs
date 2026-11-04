using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidateHarvest;

internal static class InvalidateHarvestEndpoints
{
    internal static RouteGroupBuilder MapInvalidateHarvestEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/harvests/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateHarvestRequest request, InvalidateHarvestCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InvalidateHarvest").WithTags("Cultivo e produção")
            .WithSummary("Invalidar colheita").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<HarvestResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
