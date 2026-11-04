using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.ResolveOccurrence;

internal static class ResolveOccurrenceEndpoints
{
    internal static RouteGroupBuilder MapResolveOccurrenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/occurrences/{id:guid}/resolution", async (Guid gardenId, Guid id, ResolveOccurrenceRequest request, ResolveOccurrenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Occurrences.ResolveOccurrence").WithTags("Ocorrências")
            .WithSummary("Resolver ocorrência com data e providência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<OccurrenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
