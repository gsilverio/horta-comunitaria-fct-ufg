using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.InvalidateActivity;

internal static class InvalidateActivityEndpoints
{
    internal static RouteGroupBuilder MapInvalidateActivityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/activities/{id:guid}/invalidation", async (Guid gardenId, Guid id, InvalidateActivityRequest request, InvalidateActivityCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.InvalidateActivity").WithTags("Atividades")
            .WithSummary("Invalidar atividade").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<ActivityResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
