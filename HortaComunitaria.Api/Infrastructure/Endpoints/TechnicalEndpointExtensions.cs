using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal static class TechnicalEndpointExtensions
{
    internal static IServiceCollection AddTechnicalEndpoints(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<PostgresReadinessCheck>("postgresql", tags: ["ready"]);
        // Evita o cache XML gerado com chaves duplicadas para Page<T> aninhado.
        // XML continua gerado; resumos/descrições HTTP são definidos explicitamente nos slices.
        // Documentado em Execucao_Local.md; a variável impede a interceptação XML automática.
        var documentName = "v1";
        services.AddOpenApi(documentName);
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false));
            options.SerializerOptions.Converters.Add(new OffsetDateTimeJsonConverter());
            options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict;
        });
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] =
                context.HttpContext.Items["TraceId"] ?? context.HttpContext.TraceIdentifier);
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        return services;
    }

    internal static WebApplication MapTechnicalEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteHealth })
            .WithName("Liveness").WithSummary("Verificar se o processo está ativo").WithTags("Operação");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealth })
            .WithName("Readiness").WithSummary("Verificar disponibilidade do banco").WithTags("Operação");
        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Horta Comunitária — v1"));
        return app;
    }

    private static Task WriteHealth(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy" },
            context.RequestAborted);
}
