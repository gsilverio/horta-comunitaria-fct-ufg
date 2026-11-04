using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.UpdateIrrigationSector;

internal static class UpdateIrrigationSectorEndpoints
{
    internal static RouteGroupBuilder MapUpdateIrrigationSectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/sectors/{id:guid}", async (Guid gardenId, Guid id, UpdateIrrigationSectorRequest request, UpdateIrrigationSectorCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.UpdateIrrigationSector").WithTags("Organização")
            .WithSummary("Atualizar IrrigationSector")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<IrrigationSectorResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
