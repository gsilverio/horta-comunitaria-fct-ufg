using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.InactivateSamplingPoint;

internal static class InactivateSamplingPointEndpoints
{
    internal static RouteGroupBuilder MapInactivateSamplingPointEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/sampling-points/{id:guid}/inactivation", async (Guid gardenId, Guid id, InactivateSamplingPointRequest request, InactivateSamplingPointCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.InactivateSamplingPoint").WithTags("Ambiente")
            .WithSummary("Inativar ponto de coleta").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SamplingPointResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
