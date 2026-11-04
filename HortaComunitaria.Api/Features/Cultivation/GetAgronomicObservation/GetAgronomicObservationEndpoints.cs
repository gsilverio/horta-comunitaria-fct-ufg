using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.GetAgronomicObservation;

internal static class GetAgronomicObservationEndpoints
{
    internal static RouteGroupBuilder MapGetAgronomicObservationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/agronomic-observations/{id:guid}", async (Guid gardenId, Guid id, GetAgronomicObservationQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetAgronomicObservation").WithTags("Cultivo e produção")
            .WithSummary("Consultar observação agronômica").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgronomicObservationResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
