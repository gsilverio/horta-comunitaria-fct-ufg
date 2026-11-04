using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Activities.ListParticipants;

internal static class ListParticipantsEndpoints
{
    internal static RouteGroupBuilder MapListParticipantsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/participants", async (Guid gardenId,
            ListParticipantsQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
        {
            var result = await handler.HandleAsync(gardenId, new PaginationRequest(page, pageSize),
                cancellationToken);
            return result.ToHttpResult(context);
        })
        .WithName("Activities.ListParticipants").WithTags("Atividades")
        .WithSummary("Selecionar responsáveis demonstrativos")
        .WithDescription("Consulta paginada em ordem estável, com somente cadastros ativos.")
        .Produces<Page<ListParticipantsResponse>>()
        .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
