using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.GetSamplingPoint;

internal static class GetSamplingPointEndpoints
{
    internal static RouteGroupBuilder MapGetSamplingPointEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/sampling-points/{id:guid}", async (Guid gardenId, Guid id, GetSamplingPointQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.GetSamplingPoint").WithTags("Ambiente")
            .WithSummary("Consultar ponto de coleta").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SamplingPointResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
