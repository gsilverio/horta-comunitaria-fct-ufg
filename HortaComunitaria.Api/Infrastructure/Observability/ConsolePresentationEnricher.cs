using Serilog.Core;
using Serilog.Events;

namespace HortaComunitaria.Api.Infrastructure.Observability;

/// <summary>Seleciona o contexto visual do console; o sink nativo mantém a aplicação das cores.</summary>
internal sealed class ConsolePresentationEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var process = Read(logEvent, "ProcessName");
        var step = Read(logEvent, "StepName");
        var label = process is null ? "" : step is null ? $"[{process}] " : $"[{process}/{step}] ";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ConsoleProcess", label));

        // IDs completos aparecem no resumo HTTP e em rejeições/falhas, sem repetir em cada etapa.
        var correlation = new List<string>();
        if (Read(logEvent, "EventName") == "HttpRequestCompleted" || logEvent.Level >= LogEventLevel.Warning)
        {
            foreach (var name in new[] { "ProcessId", "TraceId" })
                if (Read(logEvent, name) is { } value) correlation.Add($"{name}={value}");
        }
        var details = correlation.Count == 0 ? "" : System.Environment.NewLine + "    " + string.Join(" ", correlation);
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ConsoleCorrelation", details));
    }

    private static string? Read(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: { } scalar }
            ? scalar.ToString() : null;
}
