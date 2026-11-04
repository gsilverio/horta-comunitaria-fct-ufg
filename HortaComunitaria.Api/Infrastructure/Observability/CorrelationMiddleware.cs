using System.Diagnostics;

namespace HortaComunitaria.Api.Infrastructure.Observability;

internal sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = Activity.Current is null ? new Activity("HortaComunitaria.Http").Start() : null;
        var previous = ProcessContext.Current;
        var process = new ProcessContext(context.GetEndpoint()?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? "HttpRequest");
        process.AddResources(new Dictionary<string, object?>
        {
            ["GardenId"] = Guid.TryParse(context.Request.RouteValues["gardenId"]?.ToString(), out var gardenId) ? gardenId : null,
            ["ResourceId"] = Guid.TryParse(context.Request.RouteValues["id"]?.ToString(), out var resourceId) ? resourceId : null
        });
        ProcessContext.SetCurrent(process);
        var traceId = Activity.Current!.TraceId.ToString();
        context.Items["TraceId"] = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] = traceId;
            return Task.CompletedTask;
        });
        var properties = process.Properties("Received");
        properties["TraceId"] = traceId;
        properties["SpanId"] = Activity.Current.SpanId.ToString();
        properties["RequestId"] = context.TraceIdentifier;
        using var scope = logger.BeginScope(properties);
        try
        {
            await next(context);
        }
        finally { ProcessContext.SetCurrent(previous); }
    }
}
