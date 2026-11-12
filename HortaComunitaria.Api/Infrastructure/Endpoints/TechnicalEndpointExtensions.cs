using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal static class TechnicalEndpointExtensions
{
    internal static IServiceCollection AddTechnicalEndpoints(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<PostgresReadinessCheck>("postgresql", tags: ["ready"]);
        // OpenAPI 10.0.12+: o cache XML gerado não duplica mais chaves de Page<T> (ver Execucao_Local.md).
        // Resumos/descrições HTTP continuam definidos explicitamente nos slices.
        var documentName = "v1";
        services.AddOpenApi(documentName, options => options.AddDocumentTransformer((document, _, _) =>
        {
            // Botão Authorize do Swagger: access token do POST /api/v1/auth/login, sem persistência no navegador.
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Description = "Access token devolvido por POST /api/v1/auth/login (válido por poucos minutos)."
                }
            };
            document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
            return Task.CompletedTask;
        }));
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
            .AllowAnonymous().WithName("Liveness").WithSummary("Verificar se o processo está ativo").WithTags("Operação");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealth })
            .AllowAnonymous().WithName("Readiness").WithSummary("Verificar disponibilidade do banco").WithTags("Operação");
        app.MapOpenApi().AllowAnonymous();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Horta Comunitária — v1"));
        return app;
    }

    private static Task WriteHealth(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy" },
            context.RequestAborted);
}
