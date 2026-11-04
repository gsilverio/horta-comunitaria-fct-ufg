using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Costs.CorrectFinancialEntry;

internal static class CorrectFinancialEntryEndpoints
{
    internal static RouteGroupBuilder MapCorrectFinancialEntryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/financial-entries/{id:guid}", async (Guid gardenId, Guid id, CorrectFinancialEntryRequest request, CorrectFinancialEntryCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Costs.CorrectFinancialEntry").WithTags("Custos")
            .WithSummary("Corrigir lançamento financeiro").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<FinancialEntryResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
