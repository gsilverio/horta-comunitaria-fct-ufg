using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.UpdateBed;

internal static class UpdateBedEndpoints
{
    internal static RouteGroupBuilder MapUpdateBedEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/beds/{id:guid}", async (Guid gardenId, Guid id, UpdateBedRequest request, UpdateBedCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.UpdateBed").WithTags("Organização")
            .WithSummary("Atualizar Bed")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<BedResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
