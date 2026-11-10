using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Evidence.ListEvidences;

internal static class ListEvidencesEndpoints
{
    internal static RouteGroupBuilder MapListEvidencesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/evidences", async (Guid gardenId, ListEvidencesQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
                string? targetType = null, Guid? targetId = null, string? validity = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(targetType, targetId, validity, page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Evidence.ListEvidences").WithTags("Fotografias")
            .WithSummary("Listar fotografias")
            .WithDescription("Galeria paginada da horta, mais recentes primeiro. Filtre por targetType e, opcionalmente, targetId; validity aceita Valid ou Invalid.")
            .Produces<Page<EvidenceResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
