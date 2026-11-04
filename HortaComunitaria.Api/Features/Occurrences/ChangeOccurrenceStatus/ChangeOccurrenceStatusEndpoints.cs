using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.ChangeOccurrenceStatus;

internal static class ChangeOccurrenceStatusEndpoints
{
    internal static RouteGroupBuilder MapChangeOccurrenceStatusEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/occurrences/{id:guid}/status", async (Guid gardenId, Guid id, ChangeOccurrenceStatusRequest request, ChangeOccurrenceStatusCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Occurrences.ChangeOccurrenceStatus").WithTags("Ocorrências")
            .WithSummary("Atualizar acompanhamento da ocorrência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<OccurrenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
