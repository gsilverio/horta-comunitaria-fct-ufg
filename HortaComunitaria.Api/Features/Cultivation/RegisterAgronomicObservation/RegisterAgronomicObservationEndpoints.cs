using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.RegisterAgronomicObservation;

internal static class RegisterAgronomicObservationEndpoints
{
    internal static RouteGroupBuilder MapRegisterAgronomicObservationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/agronomic-observations", async (Guid gardenId, RegisterAgronomicObservationRequest request, RegisterAgronomicObservationCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/agronomic-observations/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Cultivation.RegisterAgronomicObservation").WithTags("Cultivo e produção")
            .WithSummary("Registrar observação agronômica").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgronomicObservationResponse>(201).Produces<AgronomicObservationResponse>(200).Produces<AgronomicObservationResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
