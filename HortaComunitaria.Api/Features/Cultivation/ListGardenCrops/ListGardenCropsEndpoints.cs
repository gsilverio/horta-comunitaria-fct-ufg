using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListGardenCrops;
internal static class ListGardenCropsEndpoints
{
    internal static RouteGroupBuilder MapListGardenCropsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/crop-visuals", async (Guid gardenId, string on, ListGardenCropsQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, on, new(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListGardenCrops").WithTags("Cultivo e produção")
            .WithSummary("Consultar cultivos para a cena panorâmica")
            .WithDescription("Data de calendário explícita, ciclos vigentes e fatos válidos até on. Quantidade inicial exclui reposições e não representa sobrevivência. Prazos da cultivar prevalecem sobre a espécie; previsão não confirma maturidade. Colheita registrada não esvazia o canteiro. Leitura consistente por página.")
            .Produces<Page<GardenCropResponse>>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
