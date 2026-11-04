using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidateAgronomicObservation;

internal static class InvalidateAgronomicObservationEndpoints
{
    internal static RouteGroupBuilder MapInvalidateAgronomicObservationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/agronomic-observations/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateAgronomicObservationRequest request, InvalidateAgronomicObservationCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.InvalidateAgronomicObservation").WithTags("Cultivo e produção")
            .WithSummary("Invalidar observação agronômica").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgronomicObservationResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
