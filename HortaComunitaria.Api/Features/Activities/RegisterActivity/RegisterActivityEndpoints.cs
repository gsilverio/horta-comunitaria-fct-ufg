using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.RegisterActivity;

internal static class RegisterActivityEndpoints
{
    internal static RouteGroupBuilder MapRegisterActivityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/activities", async (Guid gardenId, RegisterActivityRequest request, RegisterActivityCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => value.Created ? Results.Created($"/api/v1/gardens/{gardenId}/activities/{value.Detail.Id}", value.Detail) : Results.Ok(value.Detail)))
            .WithName("Activities.RegisterActivity").WithTags("Atividades")
            .WithSummary("Registrar atividade").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<ActivityResponse>(201).Produces<ActivityResponse>(200).Produces<ActivityResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
