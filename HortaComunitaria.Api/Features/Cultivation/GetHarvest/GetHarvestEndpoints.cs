using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetHarvest;

internal static class GetHarvestEndpoints
{
    internal static RouteGroupBuilder MapGetHarvestEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/harvests/{id:guid}", async (Guid gardenId, Guid id, GetHarvestQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetHarvest").WithTags("Cultivo e produção")
            .WithSummary("Consultar colheita").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<HarvestResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
