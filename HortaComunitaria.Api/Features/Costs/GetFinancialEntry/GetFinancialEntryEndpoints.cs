using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Costs.GetFinancialEntry;

internal static class GetFinancialEntryEndpoints
{
    internal static RouteGroupBuilder MapGetFinancialEntryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/financial-entries/{id:guid}", async (Guid gardenId, Guid id, GetFinancialEntryQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Costs.GetFinancialEntry").WithTags("Custos")
            .WithSummary("Consultar lançamento financeiro").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<FinancialEntryResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
