using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Costs.RegisterFinancialEntry;

internal static class RegisterFinancialEntryEndpoints
{
    internal static RouteGroupBuilder MapRegisterFinancialEntryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/financial-entries", async (Guid gardenId, RegisterFinancialEntryRequest request, RegisterFinancialEntryCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/financial-entries/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Costs.RegisterFinancialEntry").WithTags("Custos")
            .WithSummary("Registrar lançamento financeiro").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<FinancialEntryResponse>(201).Produces<FinancialEntryResponse>(200).Produces<FinancialEntryResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
