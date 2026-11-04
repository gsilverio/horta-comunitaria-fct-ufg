using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.InvalidateOccurrence;

internal static class InvalidateOccurrenceEndpoints
{
    internal static RouteGroupBuilder MapInvalidateOccurrenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/occurrences/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateOccurrenceRequest request, InvalidateOccurrenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Occurrences.InvalidateOccurrence").WithTags("Ocorrências")
            .WithSummary("Invalidar ocorrência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<OccurrenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
