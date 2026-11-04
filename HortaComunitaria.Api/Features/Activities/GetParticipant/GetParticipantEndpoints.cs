using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.GetParticipant;

internal static class GetParticipantEndpoints
{
    internal static RouteGroupBuilder MapGetParticipantEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/participants/{id:guid}", async (Guid gardenId, Guid id, GetParticipantQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.GetParticipant").WithTags("Atividades")
            .WithSummary("Consultar Participant")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<ParticipantResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
