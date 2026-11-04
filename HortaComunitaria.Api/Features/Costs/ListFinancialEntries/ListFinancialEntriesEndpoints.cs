using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Costs.ListFinancialEntries;

internal static class ListFinancialEntriesEndpoints
{
    internal static RouteGroupBuilder MapListFinancialEntriesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/financial-entries", async (Guid gardenId, ListFinancialEntriesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, string? category = null, string? nature = null, Guid? bedId = null, Guid? cycleId = null) => (await handler.HandleAsync(gardenId, new ListFinancialEntriesRequest(from, to, page, pageSize, validity, category, nature, bedId, cycleId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Costs.ListFinancialEntries").WithTags("Custos")
            .WithSummary("Listar FinancialEntries").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<FinancialEntryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
