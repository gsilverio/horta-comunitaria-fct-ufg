using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.GetIrrigationSector;

internal static class GetIrrigationSectorEndpoints
{
    internal static RouteGroupBuilder MapGetIrrigationSectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/sectors/{id:guid}", async (Guid gardenId, Guid id, GetIrrigationSectorQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.GetIrrigationSector").WithTags("Organização")
            .WithSummary("Consultar IrrigationSector")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<IrrigationSectorResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
