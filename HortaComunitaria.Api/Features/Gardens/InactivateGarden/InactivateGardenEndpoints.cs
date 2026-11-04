using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.InactivateGarden;

internal static class InactivateGardenEndpoints
{
    internal static RouteGroupBuilder MapInactivateGardenEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/inactivation", async (Guid id, InactivateGardenRequest request, InactivateGardenCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.InactivateGarden").WithTags("Organização")
            .WithSummary("Inativar Garden")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<GardenResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
