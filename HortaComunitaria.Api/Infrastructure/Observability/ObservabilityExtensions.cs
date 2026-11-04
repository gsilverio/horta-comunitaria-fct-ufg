using System.Diagnostics;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace HortaComunitaria.Api.Infrastructure.Observability;

internal static class ObservabilityExtensions
{
    internal static LoggerConfiguration ConfigureConsole(this LoggerConfiguration logger, string environment,
        ConsoleLogFormat format = ConsoleLogFormat.Text, bool applyThemeToRedirectedOutput = false)
    {
        logger.Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "HortaComunitaria.Api")
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithProperty("MachineName", System.Environment.MachineName);
        return format == ConsoleLogFormat.Json
            ? logger.WriteTo.Console(new CompactJsonFormatter())
            : logger.WriteTo.Logger(console => console
                .Enrich.With<ConsolePresentationEnricher>()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {ConsoleProcess:l}{Message:lj}{ConsoleCorrelation:l}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Literate,
                    applyThemeToRedirectedOutput: applyThemeToRedirectedOutput));
    }

    internal static IServiceCollection AddObservability(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<ObservabilityOptions>().Bind(configuration.GetSection("Observability"))
            .Validate(options => Enum.IsDefined(options.ConsoleFormat), "Observability:ConsoleFormat deve ser Text ou Json.")
            .ValidateOnStart();
        services.AddSerilog((provider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(provider)
            .ConfigureConsole(environment.EnvironmentName,
                provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value.ConsoleFormat), preserveStaticLogger: true);
        return services;
    }

    internal static WebApplication UseObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>()
                .ForContext("SourceContext", "HortaComunitaria.Api.Infrastructure.Observability.HttpRequest");
            options.MessageTemplate = "HTTP {RequestMethod} {RoutePattern} respondeu {StatusCode} em {Elapsed:0.000} ms";
            options.GetLevel = (context, _, exception) =>
                context.RequestAborted.IsCancellationRequested || context.Response.StatusCode == 499 ? LogEventLevel.Debug :
                exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error :
                context.Response.StatusCode >= 400 ? LogEventLevel.Warning :
                context.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Debug : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostic, context) =>
            {
                diagnostic.Set("EventName", "HttpRequestCompleted");
                if (ProcessContext.Current is { } process)
                    foreach (var property in process.Properties("RequestCompleted"))
                        diagnostic.Set(property.Key, property.Value!);
                diagnostic.Set("ProcessName", context.GetEndpoint()?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? "HttpRequest");
                diagnostic.Set("RoutePattern", (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "Unmatched");
                diagnostic.Set("TraceId", context.Items["TraceId"] ?? Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
                diagnostic.Set("Outcome", context.RequestAborted.IsCancellationRequested || context.Response.StatusCode == 499 ? "Cancelled" :
                    context.Response.StatusCode >= 500 ? "Failed" :
                    context.Response.StatusCode >= 400 ? "Rejected" : "Succeeded");
                foreach (var key in new[] { "gardenId", "id" })
                    if (Guid.TryParse(context.Request.RouteValues[key]?.ToString(), out var id))
                        diagnostic.Set(key == "gardenId" ? "GardenId" : "ResourceId", id);
            };
        });
        return app;
    }
}
