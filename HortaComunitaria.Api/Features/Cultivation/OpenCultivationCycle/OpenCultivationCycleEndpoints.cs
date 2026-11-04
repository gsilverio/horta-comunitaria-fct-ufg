using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.OpenCultivationCycle;

internal static class OpenCultivationCycleEndpoints
{
    internal static RouteGroupBuilder MapOpenCultivationCycleEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/cultivation-cycles", async (Guid gardenId, OpenCultivationCycleRequest request, OpenCultivationCycleCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/cultivation-cycles/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Cultivation.OpenCultivationCycle").WithTags("Cultivo e produção")
            .WithSummary("Abrir ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<CultivationCycleResponse>(201).Produces<CultivationCycleResponse>(200).Produces<CultivationCycleResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
