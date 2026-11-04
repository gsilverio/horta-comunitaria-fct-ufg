using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.GetInput;

internal static class GetInputEndpoints
{
    internal static RouteGroupBuilder MapGetInputEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/inputs/{id:guid}", async (Guid id, GetInputQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.GetInput").WithTags("Atividades")
            .WithSummary("Consultar insumo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgriculturalInputResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
