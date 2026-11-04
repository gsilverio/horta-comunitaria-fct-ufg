using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.GetGarden;

internal static class GetGardenEndpoints
{
    internal static RouteGroupBuilder MapGetGardenEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetGardenQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.GetGarden").WithTags("Organização")
            .WithSummary("Consultar Garden")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<GardenResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
