using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidateDryingLot;

internal static class InvalidateDryingLotEndpoints
{
    internal static RouteGroupBuilder MapInvalidateDryingLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/drying-lots/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateDryingLotRequest request, InvalidateDryingLotCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InvalidateDryingLot").WithTags("Cultivo e produção")
            .WithSummary("Invalidar lote de secagem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<DryingLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
