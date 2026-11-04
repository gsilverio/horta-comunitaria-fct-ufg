using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidateEstablishmentAssessment;

internal static class InvalidateEstablishmentAssessmentEndpoints
{
    internal static RouteGroupBuilder MapInvalidateEstablishmentAssessmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/establishment-assessments/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateEstablishmentAssessmentRequest request, InvalidateEstablishmentAssessmentCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InvalidateEstablishmentAssessment").WithTags("Cultivo e produção")
            .WithSummary("Invalidar avaliação de pegamento").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EstablishmentAssessmentResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
