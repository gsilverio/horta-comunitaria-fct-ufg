using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Evidence.InvalidateEvidence;

internal static class InvalidateEvidenceEndpoints
{
    internal static RouteGroupBuilder MapInvalidateEvidenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/evidences/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateEvidenceRequest request, InvalidateEvidenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Evidence.InvalidateEvidence").WithTags("Fotografias")
            .WithSummary("Invalidar fotografia")
            .WithDescription("Retira a foto da galeria com motivo, sem exclusão física do arquivo, dos metadados ou do histórico.")
            .Produces<EvidenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
