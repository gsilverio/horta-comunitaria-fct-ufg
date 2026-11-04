using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectAgronomicObservation;

internal static class CorrectAgronomicObservationEndpoints
{
    internal static RouteGroupBuilder MapCorrectAgronomicObservationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/agronomic-observations/{id:guid}", async (Guid gardenId, Guid id, CorrectAgronomicObservationRequest request, CorrectAgronomicObservationCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.CorrectAgronomicObservation").WithTags("Cultivo e produção")
            .WithSummary("Corrigir observação agronômica").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgronomicObservationResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
