using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectPropagationLot;

internal static class CorrectPropagationLotEndpoints
{
    internal static RouteGroupBuilder MapCorrectPropagationLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/propagation-lots/{id:guid}", async (Guid gardenId, Guid id, CorrectPropagationLotRequest request, CorrectPropagationLotCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectPropagationLot").WithTags("Cultivo e produção")
            .WithSummary("Corrigir lote de origem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PropagationLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
