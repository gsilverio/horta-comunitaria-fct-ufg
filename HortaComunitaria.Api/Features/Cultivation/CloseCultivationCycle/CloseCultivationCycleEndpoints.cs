using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CloseCultivationCycle;

internal static class CloseCultivationCycleEndpoints
{
    internal static RouteGroupBuilder MapCloseCultivationCycleEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/cultivation-cycles/{id:guid}/closure", async (Guid gardenId, Guid id, CloseCultivationCycleRequest request, CloseCultivationCycleCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CloseCultivationCycle").WithTags("Cultivo e produção")
            .WithSummary("Encerrar ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivationCycleResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
