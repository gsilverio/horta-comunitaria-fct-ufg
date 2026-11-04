using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetPropagationLot;

internal static class GetPropagationLotEndpoints
{
    internal static RouteGroupBuilder MapGetPropagationLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/propagation-lots/{id:guid}", async (Guid gardenId, Guid id, GetPropagationLotQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetPropagationLot").WithTags("Cultivo e produção")
            .WithSummary("Consultar lote de origem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PropagationLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
