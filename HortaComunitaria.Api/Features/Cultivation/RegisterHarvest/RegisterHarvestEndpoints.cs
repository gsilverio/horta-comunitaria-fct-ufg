using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.RegisterHarvest;

internal static class RegisterHarvestEndpoints
{
    internal static RouteGroupBuilder MapRegisterHarvestEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/harvests", async (Guid gardenId, RegisterHarvestRequest request, RegisterHarvestCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/harvests/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Cultivation.RegisterHarvest").WithTags("Cultivo e produção")
            .WithSummary("Registrar colheita").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<HarvestResponse>(201).Produces<HarvestResponse>(200).Produces<HarvestResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
