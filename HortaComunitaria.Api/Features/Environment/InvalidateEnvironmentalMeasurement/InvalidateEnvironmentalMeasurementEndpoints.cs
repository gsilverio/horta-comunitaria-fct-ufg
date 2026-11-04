using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.InvalidateEnvironmentalMeasurement;

internal static class InvalidateEnvironmentalMeasurementEndpoints
{
    internal static RouteGroupBuilder MapInvalidateEnvironmentalMeasurementEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/environmental-measurements/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateEnvironmentalMeasurementRequest request, InvalidateEnvironmentalMeasurementCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.InvalidateEnvironmentalMeasurement").WithTags("Ambiente")
            .WithSummary("Invalidar medição ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalMeasurementResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
