using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidateCultivationCycle;

internal static class InvalidateCultivationCycleEndpoints
{
    internal static RouteGroupBuilder MapInvalidateCultivationCycleEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/cultivation-cycles/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateCultivationCycleRequest request, InvalidateCultivationCycleCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InvalidateCultivationCycle").WithTags("Cultivo e produção")
            .WithSummary("Invalidar ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivationCycleResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
