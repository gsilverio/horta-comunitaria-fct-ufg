using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Gardens.AssignBedToSector;

internal static class AssignBedToSectorEndpoints
{
    internal static RouteGroupBuilder MapAssignBedToSectorEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/beds/{bedId:guid}/sector-assignments", async (Guid gardenId, Guid bedId, AssignBedToSectorRequest request, AssignBedToSectorCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, bedId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/beds/{bedId}/sector-assignments", value)))
            .RequireAdministrator().WithName("Gardens.AssignBedToSector").WithTags("Canteiros")
            .WithSummary("Configurar vigência do canteiro")
            .WithDescription("Intervalos diários [início, fim), versões e histórico preservados. Sucessão sem alteração de fatos anteriores.")
            .Produces<AssignBedToSectorResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
