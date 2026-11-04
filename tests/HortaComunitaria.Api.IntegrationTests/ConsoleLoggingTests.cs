using System.Globalization;
using System.Text.Json;
using HortaComunitaria.Api.Infrastructure.Observability;
using Serilog;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class ConsoleLoggingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Text_and_json_outputs_keep_the_message_source_and_structured_properties(bool json)
    {
        var original = Console.Out;
        using var captured = new StringWriter(CultureInfo.InvariantCulture);
        var resource = Guid.CreateVersion7();
        try
        {
            Console.SetOut(captured);
            using var logger = new LoggerConfiguration().ConfigureConsole("Testing",
                json ? ConsoleLogFormat.Json : ConsoleLogFormat.Text).CreateLogger();
            logger.ForContext("SourceContext", "Tests.Console")
                .ForContext("ProcessName", "Irrigation.RegisterIrrigation")
                .ForContext("ProcessId", Guid.CreateVersion7())
                .ForContext("EventName", "IrrigationRegistered")
                .ForContext("StepName", "Persisting")
                .Information("Irrigação {IrrigationId} confirmada", resource);
        }
        finally { Console.SetOut(original); }

        var output = captured.ToString();
        if (json)
        {
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            Assert.Equal(resource, root.GetProperty("IrrigationId").GetGuid());
            Assert.Equal("Irrigação {IrrigationId} confirmada", root.GetProperty("@mt").GetString());
            Assert.Equal("Tests.Console", root.GetProperty("SourceContext").GetString());
            Assert.Equal("Testing", root.GetProperty("Environment").GetString());
            Assert.True(root.TryGetProperty("MachineName", out _));
        }
        else
        {
            Assert.DoesNotContain("[Tests.Console]", output);
            Assert.Contains("[Irrigation.RegisterIrrigation/Persisting]", output);
            Assert.True(output.Contains($"Irrigação {resource} confirmada", StringComparison.Ordinal), output);
            Assert.DoesNotContain("ProcessId=", output);
            Assert.DoesNotContain("TraceId=", output);
            Assert.DoesNotContain("Testing", output);
            Assert.DoesNotContain("\"@mt\"", output);
        }
    }

    [Fact]
    public void Framework_events_do_not_render_empty_process_or_trace_fields()
    {
        var original = Console.Out;
        using var captured = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            Console.SetOut(captured);
            using var logger = new LoggerConfiguration().ConfigureConsole("Testing").CreateLogger();
            logger.ForContext("SourceContext", "Microsoft.Hosting.Lifetime").Information("Host encerrado");
        }
        finally { Console.SetOut(original); }
        var output = captured.ToString();
        Assert.Contains("INF] Host encerrado", output);
        Assert.DoesNotContain("ProcessId=", output);
        Assert.DoesNotContain("TraceId=", output);
        Assert.DoesNotContain("[/]", output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_console_colors_values_and_preserves_full_correlation_in_the_summary(bool json)
    {
        var original = Console.Out;
        using var captured = new StringWriter(CultureInfo.InvariantCulture);
        var processId = Guid.CreateVersion7();
        const string traceId = "aabbccddeeff00112233445566778899";
        try
        {
            Console.SetOut(captured);
            using var logger = new LoggerConfiguration().ConfigureConsole("Testing",
                json ? ConsoleLogFormat.Json : ConsoleLogFormat.Text,
                applyThemeToRedirectedOutput: true).CreateLogger();
            logger.ForContext("ProcessName", "Gardens.ListGardens")
                .ForContext("ProcessId", processId).ForContext("TraceId", traceId)
                .ForContext("EventName", "HttpRequestCompleted")
                .Information("HTTP {Method} respondeu {StatusCode}", "GET", 200);
        }
        finally { Console.SetOut(original); }

        var output = captured.ToString();
        if (json)
        {
            Assert.DoesNotContain("\u001b[", output);
            using var document = JsonDocument.Parse(output);
            Assert.Equal(processId, document.RootElement.GetProperty("ProcessId").GetGuid());
            Assert.Equal(traceId, document.RootElement.GetProperty("TraceId").GetString());
            Assert.False(document.RootElement.TryGetProperty("ConsoleProcess", out _));
        }
        else
        {
            Assert.Contains("\u001b[", output);
            var plain = System.Text.RegularExpressions.Regex.Replace(output, "\u001b\\[[0-9;]*m", "");
            Assert.Contains("[Gardens.ListGardens] HTTP GET respondeu 200", plain);
            Assert.Contains($"{Environment.NewLine}    ProcessId={processId} TraceId={traceId}", plain);
        }
    }
}
