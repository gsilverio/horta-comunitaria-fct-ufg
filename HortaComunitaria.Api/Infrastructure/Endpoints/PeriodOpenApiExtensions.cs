using Microsoft.OpenApi;
namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal static class PeriodOpenApiExtensions
{
    internal static RouteHandlerBuilder RequireDatePeriod(this RouteHandlerBuilder builder) =>
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            foreach (var parameter in operation.Parameters ?? [])
                if (parameter is OpenApiParameter query && query.Name is "from" or "to")
                {
                    query.Required = true;
                    query.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
                }
            return Task.CompletedTask;
        });
}
