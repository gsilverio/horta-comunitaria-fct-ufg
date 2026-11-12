using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.CreateGarden;

internal static class CreateGardenEndpoints
{
    internal static RouteGroupBuilder MapCreateGardenEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", async (CreateGardenRequest request, CreateGardenCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{value.Id}", value)))
            .RequireAdministrator().WithName("Gardens.CreateGarden").WithTags("Organização")
            .WithSummary("Criar Garden")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<GardenResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
