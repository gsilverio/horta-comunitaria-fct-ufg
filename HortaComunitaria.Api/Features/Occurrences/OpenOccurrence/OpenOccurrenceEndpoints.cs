using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.OpenOccurrence;

internal static class OpenOccurrenceEndpoints
{
    internal static RouteGroupBuilder MapOpenOccurrenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/occurrences", async (Guid gardenId, OpenOccurrenceRequest request, OpenOccurrenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/occurrences/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Occurrences.OpenOccurrence").WithTags("Ocorrências")
            .WithSummary("Abrir ocorrência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<OccurrenceResponse>(201).Produces<OccurrenceResponse>(200).Produces<OccurrenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
