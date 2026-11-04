using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CompleteDryingLot;

internal static class CompleteDryingLotEndpoints
{
    internal static RouteGroupBuilder MapCompleteDryingLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/drying-lots/{id:guid}/completion", async (Guid gardenId, Guid id, CompleteDryingLotRequest request, CompleteDryingLotCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CompleteDryingLot").WithTags("Cultivo e produção")
            .WithSummary("Concluir secagem com massa seca conhecida").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<DryingLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
