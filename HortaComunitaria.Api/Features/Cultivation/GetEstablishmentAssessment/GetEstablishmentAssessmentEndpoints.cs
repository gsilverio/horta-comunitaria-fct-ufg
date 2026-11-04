using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetEstablishmentAssessment;

internal static class GetEstablishmentAssessmentEndpoints
{
    internal static RouteGroupBuilder MapGetEstablishmentAssessmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/establishment-assessments/{id:guid}", async (Guid gardenId, Guid id, GetEstablishmentAssessmentQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetEstablishmentAssessment").WithTags("Cultivo e produção")
            .WithSummary("Consultar avaliação de pegamento").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EstablishmentAssessmentResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
