using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.CreateParticipant;

internal static class CreateParticipantEndpoints
{
    internal static RouteGroupBuilder MapCreateParticipantEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/participants", async (Guid gardenId, CreateParticipantRequest request, CreateParticipantCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/participants/{value.Id}", value)))
            .WithName("Activities.CreateParticipant").WithTags("Atividades")
            .WithSummary("Criar Participant")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<ParticipantResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
