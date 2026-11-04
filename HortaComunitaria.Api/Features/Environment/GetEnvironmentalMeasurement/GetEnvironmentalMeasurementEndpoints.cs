using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.GetEnvironmentalMeasurement;

internal static class GetEnvironmentalMeasurementEndpoints
{
    internal static RouteGroupBuilder MapGetEnvironmentalMeasurementEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/environmental-measurements/{id:guid}", async (Guid gardenId, Guid id, GetEnvironmentalMeasurementQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.GetEnvironmentalMeasurement").WithTags("Ambiente")
            .WithSummary("Consultar medição ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalMeasurementResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
