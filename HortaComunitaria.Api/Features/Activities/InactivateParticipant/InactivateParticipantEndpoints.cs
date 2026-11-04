using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.InactivateParticipant;

internal static class InactivateParticipantEndpoints
{
    internal static RouteGroupBuilder MapInactivateParticipantEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/participants/{id:guid}/inactivation", async (Guid gardenId, Guid id, InactivateParticipantRequest request, InactivateParticipantCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.InactivateParticipant").WithTags("Atividades")
            .WithSummary("Inativar Participant")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<ParticipantResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
