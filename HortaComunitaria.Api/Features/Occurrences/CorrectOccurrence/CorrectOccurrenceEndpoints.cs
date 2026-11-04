using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.CorrectOccurrence;

internal static class CorrectOccurrenceEndpoints
{
    internal static RouteGroupBuilder MapCorrectOccurrenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/occurrences/{id:guid}", async (Guid gardenId, Guid id, CorrectOccurrenceRequest request, CorrectOccurrenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Occurrences.CorrectOccurrence").WithTags("Ocorrências")
            .WithSummary("Corrigir ocorrência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<OccurrenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
