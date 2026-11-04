using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectDryingLot;

internal static class CorrectDryingLotEndpoints
{
    internal static RouteGroupBuilder MapCorrectDryingLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/drying-lots/{id:guid}", async (Guid gardenId, Guid id, CorrectDryingLotRequest request, CorrectDryingLotCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectDryingLot").WithTags("Cultivo e produção")
            .WithSummary("Corrigir lote de secagem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<DryingLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
