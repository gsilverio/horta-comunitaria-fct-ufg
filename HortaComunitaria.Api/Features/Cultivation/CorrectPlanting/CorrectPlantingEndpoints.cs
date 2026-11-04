using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectPlanting;

internal static class CorrectPlantingEndpoints
{
    internal static RouteGroupBuilder MapCorrectPlantingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/plantings/{id:guid}", async (Guid gardenId, Guid id, CorrectPlantingRequest request, CorrectPlantingCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectPlanting").WithTags("Cultivo e produção")
            .WithSummary("Corrigir plantio").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PlantingResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
