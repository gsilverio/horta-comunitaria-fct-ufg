using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.RegisterPropagationLot;

internal static class RegisterPropagationLotEndpoints
{
    internal static RouteGroupBuilder MapRegisterPropagationLotEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/propagation-lots", async (Guid gardenId, RegisterPropagationLotRequest request, RegisterPropagationLotCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/propagation-lots/{value.Id}", value)))
            .WithName("Cultivation.RegisterPropagationLot").WithTags("Cultivo e produção")
            .WithSummary("Registrar lote de origem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<PropagationLotResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
