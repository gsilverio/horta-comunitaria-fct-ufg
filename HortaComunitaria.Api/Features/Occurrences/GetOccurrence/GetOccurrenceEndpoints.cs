using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.GetOccurrence;

internal static class GetOccurrenceEndpoints
{
    internal static RouteGroupBuilder MapGetOccurrenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/occurrences/{id:guid}", async (Guid gardenId, Guid id, GetOccurrenceQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Occurrences.GetOccurrence").WithTags("Ocorrências")
            .WithSummary("Consultar ocorrência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<OccurrenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
