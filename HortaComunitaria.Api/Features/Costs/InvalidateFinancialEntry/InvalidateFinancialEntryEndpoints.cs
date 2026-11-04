using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Costs.InvalidateFinancialEntry;

internal static class InvalidateFinancialEntryEndpoints
{
    internal static RouteGroupBuilder MapInvalidateFinancialEntryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/financial-entries/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateFinancialEntryRequest request, InvalidateFinancialEntryCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Costs.InvalidateFinancialEntry").WithTags("Custos")
            .WithSummary("Invalidar lançamento financeiro").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<FinancialEntryResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
