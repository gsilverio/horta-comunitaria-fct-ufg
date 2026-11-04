using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectEstablishmentAssessment;

internal static class CorrectEstablishmentAssessmentEndpoints
{
    internal static RouteGroupBuilder MapCorrectEstablishmentAssessmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/establishment-assessments/{id:guid}", async (Guid gardenId, Guid id, CorrectEstablishmentAssessmentRequest request, CorrectEstablishmentAssessmentCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectEstablishmentAssessment").WithTags("Cultivo e produção")
            .WithSummary("Corrigir avaliação de pegamento").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EstablishmentAssessmentResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
