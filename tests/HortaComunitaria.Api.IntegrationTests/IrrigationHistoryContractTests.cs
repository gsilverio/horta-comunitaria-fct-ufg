using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Irrigation.ListIrrigationHistory;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;
using static HortaComunitaria.Api.IntegrationTests.IrrigationChangesTestData;
using static HortaComunitaria.Api.IntegrationTests.IrrigationChangesTests;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationHistoryContractTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task History_is_paginated_ordered_scoped_and_preserves_inactive_author()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        await CreateAsync(client, Request(10m));
        var other = Garden.Create("OTHER", "Outra horta").Value;
        var otherSector = IrrigationSector.Create(other.Id, "S1", "Outro setor").Value;
        await factory.WithDbAsync(async db => { db.AddRange(other, otherSector); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/v1/gardens/{other.Id}/irrigations",
            Request(30m) with { SectorId = otherSector.Id, BedIds = [], ResponsibleParticipantId = null }, Json)).StatusCode);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original)));
        await DetailAsync(await InvalidateAsync(client, created.Id, 2));
        await factory.WithDbAsync(db => db.Database.ExecuteSqlAsync($"UPDATE users SET \"IsActive\" = false WHERE \"Id\" = {DemoIds.Operator}"));
        var items = new List<IrrigationHistoryResponse>();
        for (var number = 1; number <= 3; number++)
        {
            var page = (await client.GetFromJsonAsync<Page<IrrigationHistoryResponse>>(Path(created.Id) + $"/history?pageSize=1&page={number}", Json))!;
            Assert.Equal(3, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(number > 1, page.HasPreviousPage);
            Assert.Equal(number < 3, page.HasNextPage);
            items.Add(Assert.Single(page.Items));
        }
        Assert.Equal(new[] { AuditOperation.Invalidated, AuditOperation.Corrected, AuditOperation.Created }, items.Select(x => x.Operation));
        Assert.Equal(items.Select(x => x.Id).OrderDescending(), items.Select(x => x.Id));
        Assert.All(items, x => { Assert.Equal(created.Id, x.After.Id); Assert.True(x.Actor.IsDemonstration); });
        var raw = await client.GetStringAsync(Path(created.Id) + "/history");
        Assert.DoesNotContain("creationPayload", raw);
        Assert.DoesNotContain("operationId", raw);
        using var json = JsonDocument.Parse(raw);
        Assert.Equal(JsonValueKind.Object, json.RootElement.GetProperty("items")[0].GetProperty("before").ValueKind);
        foreach (var query in new[] { "?page=0", "?pageSize=101", "?pageSize=0" })
            await AssertProblemAsync(await client.GetAsync(Path(created.Id) + "/history" + query), HttpStatusCode.BadRequest, "validation.invalid_request");
    }

    [Theory]
    [InlineData("reason", "null")]
    [InlineData("reason", "\" \"")]
    [InlineData("reason", "oversized")]
    [InlineData("expectedVersion", "0")]
    [InlineData("expectedVersion", "-1")]
    [InlineData("reason", "missing")]
    [InlineData("expectedVersion", "missing")]
    public async Task Invalidation_requires_reason_and_positive_explicit_version_without_mutation(string property, string value)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var created = await CreateAsync(client, Request());
        var body = JsonNode.Parse("""{"expectedVersion":1,"reason":"Teste"}""")!.AsObject();
        if (value == "missing") body.Remove(property);
        else body[property] = value == "oversized" ? JsonValue.Create(new string('x', 501)) : JsonNode.Parse(value);
        await AssertProblemAsync(await client.PostAsync(Path(created.Id) + "/invalidation",
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest, "validation.invalid_request");
        await factory.WithDbAsync(async db =>
        {
            var fact = await db.Irrigations.SingleAsync();
            Assert.Equal(1, fact.Version);
            Assert.Equal(RecordValidity.Valid, fact.Validity);
            Assert.Equal(1, await db.AuditEntries.CountAsync());
        });
    }

    [Fact]
    public async Task Change_endpoints_and_typed_history_are_documented_in_openapi()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var root = json.RootElement;
        var paths = root.GetProperty("paths");
        const string path = "/api/v1/gardens/{gardenId}/irrigations/{id}";
        var correct = paths.GetProperty(path).GetProperty("put");
        var invalidate = paths.GetProperty(path + "/invalidation").GetProperty("post");
        foreach (var operation in new[] { correct, invalidate })
        {
            foreach (var status in new[] { "200", "400", "404", "409", "500" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
            var schema = Resolve(root, operation.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
            Assert.Contains("expectedVersion", schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
            Assert.Contains("reason", schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
            Assert.False(schema.GetProperty("properties").TryGetProperty("recordedByUserId", out _));
            if (operation.GetProperty("operationId").GetString() == "Irrigation.CorrectIrrigation")
            {
                Assert.Equal(14, schema.GetProperty("required").GetArrayLength());
                Assert.Equal(new[] { "Completed", "Interrupted" }, Resolve(root, schema.GetProperty("properties").GetProperty("status")).GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
            }
        }
        var history = paths.GetProperty(path + "/history");
        Assert.Single(history.EnumerateObject());
        var page = Resolve(root, history.GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        var row = Resolve(root, page.GetProperty("properties").GetProperty("items").GetProperty("items"));
        var fields = row.GetProperty("properties");
        Assert.Equal(new[] { "Created", "Corrected", "Invalidated" }.Order(), Resolve(root, fields.GetProperty("operation")).GetProperty("enum").EnumerateArray().Select(x => x.GetString()).Order());
        var after = Resolve(root, fields.GetProperty("after"));
        Assert.True(after.GetProperty("properties").TryGetProperty("beds", out _));
        Assert.False(after.GetProperty("properties").TryGetProperty("creationPayload", out _));
        Assert.False(paths.GetProperty(path).TryGetProperty("delete", out _));
    }

    [Fact]
    public async Task Change_history_and_no_change_events_are_correlated_without_reasons_or_snapshots_in_logs()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request() with { Notes = "private-change-notes", VolumeMethod = "private-change-method" };
        var created = await CreateAsync(client, original);
        var corrected = await PutAsync(client, created.Id, Correction(original) with { Reason = "private-change-reason" });
        await DetailAsync(corrected);
        AssertEvent(factory, corrected, "IrrigationCorrected", "Irrigation.CorrectIrrigation");
        var same = await PutAsync(client, created.Id, Correction(original, version: 2));
        await DetailAsync(same);
        AssertEvent(factory, same, "IrrigationCorrectionUnchanged", "Irrigation.CorrectIrrigation");
        var invalid = await InvalidateAsync(client, created.Id, 2, "private-invalidation-reason");
        await DetailAsync(invalid);
        AssertEvent(factory, invalid, "IrrigationInvalidated", "Irrigation.InvalidateIrrigation");
        var history = await client.GetAsync(Path(created.Id) + "/history");
        AssertEvent(factory, history, "ProcessCompleted", "Irrigation.ListIrrigationHistory");
        var logs = string.Join("\n", factory.Logs.Events.Select(x => x.RenderMessage() + string.Join(" ", x.Properties)));
        foreach (var value in new[] { "private-change-notes", "private-change-method", "private-change-reason", "private-invalidation-reason", "CreationPayload", "INSERT INTO" })
            Assert.DoesNotContain(value, logs);
        Assert.DoesNotContain(factory.Logs.Events, x => x.Exception is not null);
    }

    private static JsonElement Resolve(JsonElement root, JsonElement schema) => schema.TryGetProperty("$ref", out var reference)
        ? root.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!.Split('/')[^1]) : schema;
    private static void AssertEvent(ApiFactory factory, HttpResponseMessage response, string name, string process)
    {
        var trace = response.Headers.GetValues("X-Trace-Id").Single();
        string? Property(LogEvent log, string key) => log.Properties.TryGetValue(key, out var value) ? (value as ScalarValue)?.Value?.ToString() : null;
        var events = factory.Logs.Events.Where(x => Property(x, "TraceId") == trace).ToArray();
        var specific = Assert.Single(events, x => Property(x, "EventName") == name);
        Assert.Equal(process, Property(specific, "ProcessName"));
        Assert.NotNull(Property(specific, "IrrigationId"));
        Assert.NotNull(Property(specific, "ProcessId"));
        var completed = Assert.Single(events, x => Property(x, "EventName") == "ProcessCompleted");
        Assert.True(completed.Properties.ContainsKey("ElapsedMs"));
        Assert.Single(events, x => Property(x, "EventName") == "HttpRequestCompleted");
    }
}
