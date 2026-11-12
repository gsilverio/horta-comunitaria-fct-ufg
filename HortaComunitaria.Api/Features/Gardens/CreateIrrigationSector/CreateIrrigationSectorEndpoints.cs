using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.CreateIrrigationSector;

internal static class CreateIrrigationSectorEndpoints
{
    internal static RouteGroupBuilder MapCreateIrrigationSectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/sectors", async (Guid gardenId, CreateIrrigationSectorRequest request, CreateIrrigationSectorCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/sectors/{value.Id}", value)))
            .RequireAdministrator().WithName("Gardens.CreateIrrigationSector").WithTags("Organização")
            .WithSummary("Criar IrrigationSector")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<IrrigationSectorResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
