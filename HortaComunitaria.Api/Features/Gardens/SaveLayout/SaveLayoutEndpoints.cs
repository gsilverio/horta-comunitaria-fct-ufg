using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.SaveLayout;

internal static class SaveLayoutEndpoints
{
    internal static RouteGroupBuilder MapSaveLayoutEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/layout", async (Guid gardenId, SaveLayoutRequest request, SaveLayoutCommandHandler handler, HttpContext context, CancellationToken token) => (await handler.HandleAsync(gardenId, request, token)).ToHttpResult(context))
            .RequireAdministrator().WithName("Gardens.SaveLayout").WithTags("Organização").WithSummary("Salvar configuração do mapa da horta")
            .WithDescription("Persiste geometria livre, novos setores/canteiros e vínculos iniciais atomicamente. expectedVersion evita sobrescrita; operationId protege repetição. Medidas do grid não são medidas reais. A remoção de formas não exclui cadastros históricos.")
            .Produces<LayoutResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409);
        return group;
    }
}
