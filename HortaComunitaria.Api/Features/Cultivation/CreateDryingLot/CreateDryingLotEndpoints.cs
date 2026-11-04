using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.CreateDryingLot;

internal static class CreateDryingLotEndpoints
{
    internal static RouteGroupBuilder MapCreateDryingLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/drying-lots", async (Guid gardenId, CreateDryingLotRequest request, CreateDryingLotCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/drying-lots/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Cultivation.CreateDryingLot").WithTags("Cultivo e produção")
            .WithSummary("Cadastrar lote de secagem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<DryingLotResponse>(201).Produces<DryingLotResponse>(200).Produces<DryingLotResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
