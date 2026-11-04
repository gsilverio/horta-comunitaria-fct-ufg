using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Occurrences.ListOccurrences;

internal static class ListOccurrencesEndpoints
{
    internal static RouteGroupBuilder MapListOccurrencesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/occurrences", async (Guid gardenId, ListOccurrencesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, string? category = null, string? status = null, Guid? bedId = null, Guid? sectorId = null, Guid? cycleId = null, Guid? irrigationId = null) => (await handler.HandleAsync(gardenId, new ListOccurrencesRequest(from, to, page, pageSize, validity, category, status, bedId, sectorId, cycleId, irrigationId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Occurrences.ListOccurrences").WithTags("Ocorrências")
            .WithSummary("Listar ocorrência").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<OccurrenceResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
