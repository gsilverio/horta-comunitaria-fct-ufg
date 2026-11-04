using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.GetBed;

internal static class GetBedEndpoints
{
    internal static RouteGroupBuilder MapGetBedEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/beds/{id:guid}", async (Guid gardenId, Guid id, GetBedQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? on = null) =>
            (await handler.HandleAsync(gardenId, id, on, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.GetBed").WithTags("Organização")
            .WithSummary("Consultar Bed")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<BedResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
