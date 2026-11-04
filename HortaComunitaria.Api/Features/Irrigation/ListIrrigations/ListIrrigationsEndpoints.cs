using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace HortaComunitaria.Api.Features.Irrigation.ListIrrigations;

internal static class ListIrrigationsEndpoints
{
    internal static RouteGroupBuilder MapListIrrigationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/irrigations", async (Guid gardenId, string? from, string? to, Guid? sectorId, string? validity,
            ListIrrigationsQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, sectorId, validity, page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Irrigation.ListIrrigations").WithTags("Irrigação")
            .WithSummary("Listar irrigações por período local")
            .WithDescription("from/to obrigatórios em YYYY-MM-DD e inclusivos no fuso America/Sao_Paulo. Validade padrão Valid.")
            .Produces<Page<IrrigationSummaryResponse>>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                foreach (var parameter in operation.Parameters ?? [])
                {
                    if (parameter is not OpenApiParameter query) continue;
                    if (query.Name is "from" or "to")
                    {
                        query.Required = true;
                        query.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
                    }
                    if (query.Name == "validity")
                        query.Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Default = JsonValue.Create(nameof(RecordValidity.Valid)),
                            Enum = Enum.GetNames<RecordValidity>().Select(x => (JsonNode)JsonValue.Create(x)!).ToList()
                        };
                }
                return Task.CompletedTask;
            });
        return group;
    }
}
