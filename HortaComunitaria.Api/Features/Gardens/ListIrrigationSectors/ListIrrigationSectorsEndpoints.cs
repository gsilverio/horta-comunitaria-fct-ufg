using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.ListIrrigationSectors;

internal static class ListIrrigationSectorsEndpoints
{
    internal static RouteGroupBuilder MapListIrrigationSectorsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/sectors", async (Guid gardenId,
            ListIrrigationSectorsQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
        {
            var result = await handler.HandleAsync(gardenId, new PaginationRequest(page, pageSize),
                cancellationToken);
            return result.ToHttpResult(context);
        })
        .WithName("Gardens.ListIrrigationSectors").WithTags("Hortas")
        .WithSummary("Listar setores ativos da horta")
        .WithDescription("Consulta paginada em ordem estável, com somente cadastros ativos.")
        .Produces<Page<ListIrrigationSectorsResponse>>()
        .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
