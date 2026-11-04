using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.InactivateBed;

internal static class InactivateBedEndpoints
{
    internal static RouteGroupBuilder MapInactivateBedEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/beds/{id:guid}/inactivation", async (Guid gardenId, Guid id, InactivateBedRequest request, InactivateBedCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.InactivateBed").WithTags("Organização")
            .WithSummary("Inativar Bed")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<BedResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
