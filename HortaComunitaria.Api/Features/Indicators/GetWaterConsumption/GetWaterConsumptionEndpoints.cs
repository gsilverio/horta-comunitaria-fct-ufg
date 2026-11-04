using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using Microsoft.OpenApi;

namespace HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;

internal static class GetWaterConsumptionEndpoints
{
    internal static RouteGroupBuilder MapGetWaterConsumptionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/water-consumption", async (Guid gardenId, string? from, string? to, Guid? sectorId,
            GetWaterConsumptionQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, sectorId, page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Indicators.GetWaterConsumption").WithTags("Indicadores")
            .WithSummary("Consultar consumo conhecido de água")
            .WithDescription("Litros dos eventos válidos, contados uma vez. Resumo cobre todo o recorte; paginação afeta somente os setores. Fórmula do protótipo sujeita à PO.")
            .Produces<WaterConsumptionResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                foreach (var parameter in operation.Parameters ?? [])
                    if (parameter is OpenApiParameter query && query.Name is "from" or "to")
                    {
                        query.Required = true;
                        query.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
                    }
                return Task.CompletedTask;
            });
        return group;
    }
}
