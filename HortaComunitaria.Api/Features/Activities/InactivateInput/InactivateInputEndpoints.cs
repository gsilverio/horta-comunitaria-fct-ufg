using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.InactivateInput;

internal static class InactivateInputEndpoints
{
    internal static RouteGroupBuilder MapInactivateInputEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/inputs/{id:guid}/inactivation", async (Guid id, InactivateInputRequest request, InactivateInputCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Activities.InactivateInput").WithTags("Atividades")
            .WithSummary("Inativar insumo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgriculturalInputResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
