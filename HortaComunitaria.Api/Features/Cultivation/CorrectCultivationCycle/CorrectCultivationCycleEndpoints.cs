using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectCultivationCycle;

internal static class CorrectCultivationCycleEndpoints
{
    internal static RouteGroupBuilder MapCorrectCultivationCycleEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/cultivation-cycles/{id:guid}", async (Guid gardenId, Guid id, CorrectCultivationCycleRequest request, CorrectCultivationCycleCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectCultivationCycle").WithTags("Cultivo e produção")
            .WithSummary("Corrigir ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivationCycleResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
