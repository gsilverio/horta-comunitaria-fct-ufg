using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.UpdateSamplingPoint;

internal static class UpdateSamplingPointEndpoints
{
    internal static RouteGroupBuilder MapUpdateSamplingPointEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/sampling-points/{id:guid}", async (Guid gardenId, Guid id, UpdateSamplingPointRequest request, UpdateSamplingPointCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.UpdateSamplingPoint").WithTags("Ambiente")
            .WithSummary("Editar ponto de coleta").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SamplingPointResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
