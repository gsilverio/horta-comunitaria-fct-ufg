using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.GetLayout;

internal static class GetLayoutEndpoints
{
    internal static RouteGroupBuilder MapGetLayoutEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/layout", async (Guid gardenId, GetLayoutQueryHandler handler, HttpContext context, CancellationToken token) => (await handler.HandleAsync(gardenId, token)).ToHttpResult(context))
            .WithName("Gardens.GetLayout").WithTags("Organização").WithSummary("Consultar configuração salva do mapa")
            .WithDescription("Retorna a última configuração confirmada; version=0 e layout=null indicam que nenhum mapa foi salvo.")
            .Produces<LayoutResponse>().ProducesProblem(404);
        return group;
    }
}
