using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetPlanting;

internal static class GetPlantingEndpoints
{
    internal static RouteGroupBuilder MapGetPlantingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/plantings/{id:guid}", async (Guid gardenId, Guid id, GetPlantingQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetPlanting").WithTags("Cultivo e produção")
            .WithSummary("Consultar plantio").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PlantingResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
