using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.RegisterEstablishmentAssessment;

internal static class RegisterEstablishmentAssessmentEndpoints
{
    internal static RouteGroupBuilder MapRegisterEstablishmentAssessmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/establishment-assessments", async (Guid gardenId, RegisterEstablishmentAssessmentRequest request, RegisterEstablishmentAssessmentCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/establishment-assessments/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Cultivation.RegisterEstablishmentAssessment").WithTags("Cultivo e produção")
            .WithSummary("Registrar avaliação de pegamento").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EstablishmentAssessmentResponse>(201).Produces<EstablishmentAssessmentResponse>(200).Produces<EstablishmentAssessmentResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
