using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.ListBedConfigurations;

internal static class ListBedConfigurationsEndpoints
{
    internal static RouteGroupBuilder MapListBedConfigurationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/beds/{bedId:guid}/configurations", async (Guid gardenId, Guid bedId, ListBedConfigurationsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, bedId, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.ListBedConfigurations").WithTags("Canteiros")
            .WithSummary("Consultar vigências do canteiro")
            .WithDescription("Intervalos diários [início, fim), versões e histórico preservados. Sucessão sem alteração de fatos anteriores.")
            .Produces<Page<BedConfigurationResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
