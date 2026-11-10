using HortaComunitaria.Api.Infrastructure.Endpoints;
using Microsoft.Net.Http.Headers;

namespace HortaComunitaria.Api.Features.Evidence.GetEvidenceContent;

internal static class GetEvidenceContentEndpoints
{
    internal static RouteGroupBuilder MapGetEvidenceContentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/evidences/{id:guid}/content", async (Guid gardenId, Guid id, GetEvidenceContentQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context, value =>
            {
                context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
                context.Response.Headers[HeaderNames.CacheControl] = "private, max-age=86400";
                return Results.Stream(value.Content, value.ContentType, fileDownloadName: null, entityTag: new EntityTagHeaderValue($"\"{value.Sha256}\""), enableRangeProcessing: true);
            }))
            .WithName("Evidence.GetEvidenceContent").WithTags("Fotografias")
            .WithSummary("Obter conteúdo da fotografia")
            .WithDescription("Retorna a imagem armazenada com o tipo conferido no envio. Fotografias invalidadas continuam consultáveis para rastreabilidade.")
            .Produces(200, contentType: "image/jpeg").ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
