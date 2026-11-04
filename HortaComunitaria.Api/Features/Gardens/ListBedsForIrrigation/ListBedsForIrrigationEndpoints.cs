using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.ListBedsForIrrigation;

internal static class ListBedsForIrrigationEndpoints
{
    internal static RouteGroupBuilder MapListBedsForIrrigationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/beds", async (Guid gardenId, string? on, Guid? sectorId,
            ListBedsForIrrigationQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
        {
            var result = await handler.HandleAsync(gardenId, new ListBedsForIrrigationRequest(on, sectorId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(context);
        })
        .WithName("Gardens.ListBedsForIrrigation").WithTags("Hortas")
        .WithSummary("Selecionar canteiros elegíveis na data local")
        .WithDescription("Data on obrigatória em YYYY-MM-DD e setor da mesma horta. Vigência com início inclusivo e fim exclusivo.")
        .Produces<Page<ListBedsForIrrigationResponse>>()
        .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500)
        .AddOpenApiOperationTransformer((operation, _, _) =>
        {
            foreach (var parameter in operation.Parameters ?? [])
            {
                if (parameter is not Microsoft.OpenApi.OpenApiParameter query) continue;
                if (query.Name is "on" or "sectorId") query.Required = true;
                if (query.Name == "on")
                    query.Schema = new Microsoft.OpenApi.OpenApiSchema
                    { Type = Microsoft.OpenApi.JsonSchemaType.String, Format = "date" };
            }
            return Task.CompletedTask;
        });
        return group;
    }
}
