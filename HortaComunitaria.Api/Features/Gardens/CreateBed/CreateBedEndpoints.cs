using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.CreateBed;

internal static class CreateBedEndpoints
{
    internal static RouteGroupBuilder MapCreateBedEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/beds", async (Guid gardenId, CreateBedRequest request, CreateBedCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/beds/{value.Id}", value)))
            .RequireAdministrator().WithName("Gardens.CreateBed").WithTags("Organização")
            .WithSummary("Criar Bed")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<BedResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
