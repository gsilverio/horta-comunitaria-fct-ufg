namespace HortaComunitaria.Api.Infrastructure.Observability;

internal enum ConsoleLogFormat { Text, Json }

internal sealed class ObservabilityOptions
{
    public ConsoleLogFormat ConsoleFormat { get; set; } = ConsoleLogFormat.Json;
}
