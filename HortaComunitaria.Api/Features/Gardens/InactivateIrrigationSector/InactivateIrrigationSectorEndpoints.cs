using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.InactivateIrrigationSector;

internal static class InactivateIrrigationSectorEndpoints
{
    internal static RouteGroupBuilder MapInactivateIrrigationSectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/sectors/{id:guid}/inactivation", async (Guid gardenId, Guid id, InactivateIrrigationSectorRequest request, InactivateIrrigationSectorCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.InactivateIrrigationSector").WithTags("Organização")
            .WithSummary("Inativar IrrigationSector")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<IrrigationSectorResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
