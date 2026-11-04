using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Cultivation.GetSpeciesImage;

internal static class GetSpeciesImageEndpoints
{
    internal static RouteGroupBuilder MapGetSpeciesImageEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species/{id:guid}/image", async (Guid id, GetSpeciesImageQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.GetSpeciesImage").WithTags("Cultivo e produção")
            .WithSummary("Consultar imagem ilustrativa da espécie")
            .WithDescription("JPEG em Base64 com autor, licença, procedência e alterações. Exibir créditos e licença no catálogo. A foto não identifica uma cultivar específica. Retorna 404 quando ausente.")
            .Produces<SpeciesImageResponse>().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
