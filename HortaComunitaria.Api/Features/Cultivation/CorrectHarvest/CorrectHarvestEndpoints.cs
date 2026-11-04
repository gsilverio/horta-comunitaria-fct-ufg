using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectHarvest;

internal static class CorrectHarvestEndpoints
{
    internal static RouteGroupBuilder MapCorrectHarvestEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/harvests/{id:guid}", async (Guid gardenId, Guid id, CorrectHarvestRequest request, CorrectHarvestCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectHarvest").WithTags("Cultivo e produção")
            .WithSummary("Corrigir colheita").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<HarvestResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
