using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Gardens.ConfigureBed;

internal static class ConfigureBedEndpoints
{
    internal static RouteGroupBuilder MapConfigureBedEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/beds/{bedId:guid}/configurations", async (Guid gardenId, Guid bedId, ConfigureBedRequest request, ConfigureBedCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, bedId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/beds/{bedId}/configurations", value)))
            .RequireAdministrator().WithName("Gardens.ConfigureBed").WithTags("Canteiros")
            .WithSummary("Configurar vigência do canteiro")
            .WithDescription("Intervalos diários [início, fim), versões e histórico preservados. Sucessão sem alteração de fatos anteriores.")
            .Produces<ConfigureBedResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
