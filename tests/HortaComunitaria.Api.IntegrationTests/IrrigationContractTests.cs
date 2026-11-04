using System.Net;
using System.Text.Json;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationContractTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Openapi_describes_creation_enums_periods_and_consumption_contract()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var root = json.RootElement;
        var paths = root.GetProperty("paths");
        var irrigationPath = paths.GetProperty("/api/v1/gardens/{gardenId}/irrigations");
        var post = irrigationPath.GetProperty("post");
        foreach (var status in new[] { "200", "201", "400", "404", "409", "500" })
            Assert.True(post.GetProperty("responses").TryGetProperty(status, out _));
        var request = Resolve(root, post.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        var required = request.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var name in new[] { "operationId", "sectorId", "bedIds", "startedAt", "status" }) Assert.Contains(name, required);
        var properties = request.GetProperty("properties");
        Assert.Equal("date-time", Resolve(root, properties.GetProperty("startedAt")).GetProperty("format").GetString());
        AssertEnum(root, properties.GetProperty("status"), "Completed", "Interrupted");
        AssertEnum(root, properties.GetProperty("volumeSource"), "Measured", "Estimated");
        Assert.False(properties.TryGetProperty("recordedByUserId", out _));
        var consumption = paths.GetProperty("/api/v1/gardens/{gardenId}/indicators/water-consumption").GetProperty("get");
        foreach (var operation in new[] { irrigationPath.GetProperty("get"), consumption })
            foreach (var name in new[] { "from", "to" })
            {
                var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray(), x => x.GetProperty("name").GetString() == name);
                Assert.True(parameter.GetProperty("required").GetBoolean());
                Assert.Equal("date", parameter.GetProperty("schema").GetProperty("format").GetString());
            }
        var schema = Resolve(root, consumption.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        var summary = Resolve(root, schema.GetProperty("properties").GetProperty("summary"));
        AssertEnum(root, summary.GetProperty("properties").GetProperty("availability"), "NoData", "Unavailable", "Partial", "Available");
        var detail = Resolve(root, post.GetProperty("responses").GetProperty("201").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        AssertEnum(root, detail.GetProperty("properties").GetProperty("validity"), "Valid", "Invalid");
        Assert.True(paths.TryGetProperty("/api/v1/gardens/{gardenId}/irrigations/{id}", out _));
        var detailPath = paths.GetProperty("/api/v1/gardens/{gardenId}/irrigations/{id}");
        Assert.True(detailPath.TryGetProperty("put", out _));
        Assert.True(paths.TryGetProperty("/api/v1/gardens/{gardenId}/irrigations/{id}/history", out _));
    }

    [Fact]
    public async Task Persistence_uses_native_enums_jsonb_and_fixed_precision()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            var enumTypes = await db.Database.SqlQueryRaw<string>("""
                SELECT typname AS "Value" FROM pg_type WHERE typtype = 'e' ORDER BY typname
                """).ToListAsync();
            Assert.Equal(new[] { "audit_operation", "cultivation_status", "drying_status", "financial_nature", "irrigation_status", "material_type", "measurement_quality", "occurrence_status", "planting_kind", "record_validity", "volume_source" }, enumTypes);
            var types = await db.Database.SqlQueryRaw<string>("""
                SELECT column_name || ':' || udt_name AS "Value" FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'irrigations'
                    AND column_name IN ('Status', 'Validity', 'VolumeSource', 'CreationPayload') ORDER BY column_name
                """).ToListAsync();
            Assert.Equal(new[] { "CreationPayload:jsonb", "Status:irrigation_status", "Validity:record_validity", "VolumeSource:volume_source" }, types);
            var measures = await db.Database.SqlQueryRaw<string>("""
                SELECT column_name || ':' || numeric_precision || ':' || numeric_scale AS "Value"
                FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'irrigations'
                    AND data_type = 'numeric' ORDER BY column_name
                """).ToListAsync();
            Assert.Equal(new[] { "ActiveDurationMinutes:14:3", "FlowRateLitersPerMinute:14:3", "VolumeLiters:14:3" }, measures);
        });
    }

    [Fact]
    public async Task Write_and_read_logs_identify_process_resources_and_hide_form_values()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var response = await PostAsync(client, Request() with { Notes = "private-notes-sentinel", VolumeMethod = "private-method-sentinel" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var trace = response.Headers.GetValues("X-Trace-Id").Single();
        var events = factory.Logs.Events.Where(x => Property(x, "TraceId") == trace).ToArray();
        var registered = Assert.Single(events, x => Property(x, "EventName") == "IrrigationRegistered");
        Assert.Equal("Irrigation.RegisterIrrigation", Property(registered, "ProcessName"));
        Assert.Equal(DemoIds.Garden.ToString(), Property(registered, "GardenId"));
        Assert.NotNull(Property(registered, "IrrigationId"));
        Assert.Equal(7, Guid.Parse(Property(registered, "ProcessId")!).Version);
        Assert.Single(events, x => Property(x, "EventName") == "ProcessCompleted");
        Assert.Single(events, x => Property(x, "EventName") == "HttpRequestCompleted");
        var read = await client.GetAsync(ConsumptionPath);
        var readTrace = read.Headers.GetValues("X-Trace-Id").Single();
        var consumed = Assert.Single(factory.Logs.Events, x => Property(x, "TraceId") == readTrace && Property(x, "EventName") == "ProcessCompleted");
        Assert.Equal("Indicators.GetWaterConsumption", Property(consumed, "ProcessName"));
        Assert.True(consumed.Properties.ContainsKey("ElapsedMs"));
        var serialized = string.Join("\n", factory.Logs.Events.Select(x => x.RenderMessage() + " " + string.Join(" ", x.Properties)));
        foreach (var sentinel in new[] { "private-notes-sentinel", "private-method-sentinel", "CreationPayload", "INSERT INTO" })
            Assert.DoesNotContain(sentinel, serialized);
    }

    private static string? Property(LogEvent log, string key) =>
        log.Properties.TryGetValue(key, out var value) ? (value as ScalarValue)?.Value?.ToString() : null;
    private static JsonElement Resolve(JsonElement root, JsonElement schema) => schema.TryGetProperty("$ref", out var reference)
        ? root.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!.Split('/')[^1]) : schema;
    private static void AssertEnum(JsonElement root, JsonElement schema, params string[] expected)
    {
        schema = Resolve(root, schema);
        if (schema.TryGetProperty("anyOf", out var variants) || schema.TryGetProperty("oneOf", out variants))
            schema = Resolve(root, variants.EnumerateArray().First(x => !x.TryGetProperty("type", out var type) || type.GetString() != "null"));
        Assert.True(schema.TryGetProperty("enum", out var values), schema.GetRawText());
        Assert.Equal(expected.Order(), values.EnumerateArray().Where(x => x.ValueKind != JsonValueKind.Null).Select(x => x.GetString()).Order());
    }
}
