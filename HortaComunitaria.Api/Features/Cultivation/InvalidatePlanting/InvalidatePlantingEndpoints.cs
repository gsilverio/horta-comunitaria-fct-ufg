using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidatePlanting;

internal static class InvalidatePlantingEndpoints
{
    internal static RouteGroupBuilder MapInvalidatePlantingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/plantings/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidatePlantingRequest request, InvalidatePlantingCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InvalidatePlanting").WithTags("Cultivo e produção")
            .WithSummary("Invalidar plantio").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PlantingResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
