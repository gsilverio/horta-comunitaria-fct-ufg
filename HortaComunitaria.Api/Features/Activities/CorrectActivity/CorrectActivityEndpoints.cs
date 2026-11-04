using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.CorrectActivity;

internal static class CorrectActivityEndpoints
{
    internal static RouteGroupBuilder MapCorrectActivityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/activities/{id:guid}", async (Guid gardenId, Guid id, CorrectActivityRequest request, CorrectActivityCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.CorrectActivity").WithTags("Atividades")
            .WithSummary("Corrigir atividade").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<ActivityResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
