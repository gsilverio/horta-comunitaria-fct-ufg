using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetCultivationCycle;

internal static class GetCultivationCycleEndpoints
{
    internal static RouteGroupBuilder MapGetCultivationCycleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/cultivation-cycles/{id:guid}", async (Guid gardenId, Guid id, GetCultivationCycleQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetCultivationCycle").WithTags("Cultivo e produção")
            .WithSummary("Consultar ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivationCycleResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
