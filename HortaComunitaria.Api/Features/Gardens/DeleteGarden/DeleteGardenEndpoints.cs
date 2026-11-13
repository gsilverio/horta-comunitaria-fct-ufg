using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Gardens.DeleteGarden;

internal static class DeleteGardenEndpoints
{
    internal static RouteGroupBuilder MapDeleteGardenEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/deletion", async (Guid id, DeleteGardenRequest request, DeleteGardenCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context, _ => Results.NoContent()))
            .RequireAdministrator().WithName("Gardens.DeleteGarden").WithTags("Organização")
            .WithSummary("Excluir horta sem registros")
            .WithDescription("Exclusão definitiva permitida somente quando a horta não tem fatos de campo (ciclos, lotes, irrigações, atividades, ocorrências, custos, secagens, medições ou fotografias). Com fatos, responde 409 garden.has_records e a horta só pode ser inativada.")
            .Produces(204).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
