using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.RegisterPlanting;

internal static class RegisterPlantingEndpoints
{
    internal static RouteGroupBuilder MapRegisterPlantingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/plantings", async (Guid gardenId, RegisterPlantingRequest request, RegisterPlantingCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/plantings/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Cultivation.RegisterPlanting").WithTags("Cultivo e produção")
            .WithSummary("Registrar plantio").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PlantingResponse>(201).Produces<PlantingResponse>(200).Produces<PlantingResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
