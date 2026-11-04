using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.UpdateParticipant;

internal static class UpdateParticipantEndpoints
{
    internal static RouteGroupBuilder MapUpdateParticipantEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/participants/{id:guid}", async (Guid gardenId, Guid id, UpdateParticipantRequest request, UpdateParticipantCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.UpdateParticipant").WithTags("Atividades")
            .WithSummary("Atualizar Participant")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<ParticipantResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
