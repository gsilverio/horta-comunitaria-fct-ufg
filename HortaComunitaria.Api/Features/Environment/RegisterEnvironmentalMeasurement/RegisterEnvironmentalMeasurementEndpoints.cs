using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.RegisterEnvironmentalMeasurement;

internal static class RegisterEnvironmentalMeasurementEndpoints
{
    internal static RouteGroupBuilder MapRegisterEnvironmentalMeasurementEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/environmental-measurements", async (Guid gardenId, RegisterEnvironmentalMeasurementRequest request, RegisterEnvironmentalMeasurementCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/environmental-measurements/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Environment.RegisterEnvironmentalMeasurement").WithTags("Ambiente")
            .WithSummary("Registrar medição ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalMeasurementResponse>(201).Produces<EnvironmentalMeasurementResponse>(200).Produces<EnvironmentalMeasurementResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
