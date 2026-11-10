using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace HortaComunitaria.Api.Features.Evidence.UploadEvidence;

internal static class UploadEvidenceEndpoints
{
    internal static RouteGroupBuilder MapUploadEvidenceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/evidences", async (Guid gardenId, IFormFile? file, [FromForm] string? operationId, [FromForm] string? targetType,
                [FromForm] string? targetId, [FromForm] string? title, [FromForm] string? description, [FromForm] string? capturedOn,
                UploadEvidenceCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, new(operationId, targetType, targetId, title, description, capturedOn),
                file is null ? null : new UploadedFile(file.FileName, file.Length, file.OpenReadStream), cancellationToken))
            .ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/evidences/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            // Sem autenticação por cookie no protótipo; antiforgery será revisto com a etapa de identidade.
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .WithName("Evidence.UploadEvidence").WithTags("Fotografias")
            .WithSummary("Enviar fotografia")
            .WithDescription("Multipart com file (JPEG, PNG ou WebP, limite configurado), operationId, targetType (Bed, CultivationCycle, Planting, EstablishmentAssessment, AgronomicObservation, Harvest, DryingLot, Activity, Occurrence), targetId, title, description opcional e capturedOn (AAAA-MM-DD) opcional. O tipo é conferido pela assinatura do arquivo.")
            .Produces<EvidenceResponse>(201).Produces<EvidenceResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
