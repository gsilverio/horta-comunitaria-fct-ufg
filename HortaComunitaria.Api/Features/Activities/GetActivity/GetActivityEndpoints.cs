using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.GetActivity;

internal static class GetActivityEndpoints
{
    internal static RouteGroupBuilder MapGetActivityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/activities/{id:guid}", async (Guid gardenId, Guid id, GetActivityQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.GetActivity").WithTags("Atividades")
            .WithSummary("Consultar atividade").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<ActivityResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
