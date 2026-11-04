using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.CorrectEnvironmentalMeasurement;

internal static class CorrectEnvironmentalMeasurementEndpoints
{
    internal static RouteGroupBuilder MapCorrectEnvironmentalMeasurementEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/environmental-measurements/{id:guid}", async (Guid gardenId, Guid id, CorrectEnvironmentalMeasurementRequest request, CorrectEnvironmentalMeasurementCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.CorrectEnvironmentalMeasurement").WithTags("Ambiente")
            .WithSummary("Corrigir medição ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalMeasurementResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
