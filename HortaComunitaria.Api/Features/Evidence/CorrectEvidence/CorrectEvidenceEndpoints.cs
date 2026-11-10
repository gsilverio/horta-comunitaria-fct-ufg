using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Evidence.CorrectEvidence;

internal static class CorrectEvidenceEndpoints
{
    internal static RouteGroupBuilder MapCorrectEvidenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/evidences/{id:guid}", async (Guid gardenId, Guid id, CorrectEvidenceRequest request, CorrectEvidenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Evidence.CorrectEvidence").WithTags("Fotografias")
            .WithSummary("Corrigir legenda da fotografia")
            .WithDescription("Corrige título, descrição e data da foto com versão esperada e motivo; o arquivo e o vínculo não mudam.")
            .Produces<EvidenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
