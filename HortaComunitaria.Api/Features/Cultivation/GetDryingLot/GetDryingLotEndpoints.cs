using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetDryingLot;

internal static class GetDryingLotEndpoints
{
    internal static RouteGroupBuilder MapGetDryingLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/drying-lots/{id:guid}", async (Guid gardenId, Guid id, GetDryingLotQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetDryingLot").WithTags("Cultivo e produção")
            .WithSummary("Consultar lote de secagem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<DryingLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
