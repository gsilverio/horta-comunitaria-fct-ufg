using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.UpdateInput;

internal static class UpdateInputEndpoints
{
    internal static RouteGroupBuilder MapUpdateInputEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/inputs/{id:guid}", async (Guid id, UpdateInputRequest request, UpdateInputCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Activities.UpdateInput").WithTags("Atividades")
            .WithSummary("Editar insumo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgriculturalInputResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
