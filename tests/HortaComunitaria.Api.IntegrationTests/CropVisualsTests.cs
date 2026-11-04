using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using static HortaComunitaria.Api.IntegrationTests.MvpWorkflowTests;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class CropVisualsTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static string Root => IrrigationTestData.BasePath;
    private static async Task<JsonElement> Scene(HttpClient client, string on)
    {
        var response = await client.GetAsync($"{Root}/crop-visuals?on={on}");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await ReadAsync(response)).GetProperty("items")[0];
    }

    [Fact]
    public async Task Timings_are_audited_optional_inherited_and_overridden_by_cultivar()
    {
        await using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        Assert.Equal("AwaitingPlanting", (await Scene(client, "2026-10-02")).GetProperty("stage").GetString());
        await CreateAsync(client, Root + "/plantings", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, plantedOn = "2026-10-02", kind = "Initial", plantedQuantity = 40 });
        Assert.Equal("Planted", (await Scene(client, "2026-10-30")).GetProperty("stage").GetString());
        var path = $"/api/v1/species/{context.Species}/visual-growth";
        var invalid = await client.PutAsJsonAsync(path, new { visualGrowthDays = 0, visualHarvestDays = 20, expectedVersion = 1, reason = "Configuração de teste" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var updated = await client.PutAsJsonAsync(path, new { visualGrowthDays = 5, visualHarvestDays = 20, expectedVersion = 1, reason = "Configuração ilustrativa de teste" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(2, (await ReadAsync(updated)).GetProperty("version").GetInt32());
        var stale = await client.PutAsJsonAsync(path, new { visualGrowthDays = 6, visualHarvestDays = 21, expectedVersion = 1, reason = "Tentativa concorrente" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("Seedling", (await Scene(client, "2026-10-03")).GetProperty("stage").GetString());
        Assert.Equal("Growing", (await Scene(client, "2026-10-07")).GetProperty("stage").GetString());
        Assert.Equal("HarvestExpected", (await Scene(client, "2026-10-22")).GetProperty("stage").GetString());
        var cycle = await ReadAsync(await client.GetAsync($"{Root}/cultivation-cycles/{context.Cycle}"));
        var cultivarId = cycle.GetProperty("cultivarId").GetGuid();
        var cultivarPath = $"/api/v1/species/{context.Species}/cultivars/{cultivarId}/visual-growth";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(cultivarPath, new { visualGrowthDays = 2, visualHarvestDays = 7, expectedVersion = 1, reason = "Override de teste" })).StatusCode);
        Assert.Equal("HarvestExpected", (await Scene(client, "2026-10-09")).GetProperty("stage").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(cultivarPath, new { visualGrowthDays = (int?)null, visualHarvestDays = (int?)null, expectedVersion = 2, reason = "Herdar prazos da espécie" })).StatusCode);
        Assert.Equal("Growing", (await Scene(client, "2026-10-09")).GetProperty("stage").GetString());
        await factory.WithDbAsync(async db => Assert.Equal(3, await db.AuditEntries.CountAsync(x => x.Reason == "Configuração ilustrativa de teste" || x.Reason == "Override de teste" || x.Reason == "Herdar prazos da espécie")));
    }

    [Fact]
    public async Task Real_facts_override_estimates_without_double_counting_replacements_or_clearing_the_bed()
    {
        await using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        var initial = await CreateAsync(client, Root + "/plantings", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, plantedOn = "2026-10-02", kind = "Initial", plantedQuantity = 40 });
        await CreateAsync(client, Root + "/plantings", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, replacedPlantingId = Id(initial), plantedOn = "2026-10-05", kind = "Replacement", plantedQuantity = 10 });
        var mixed = await Scene(client, "2026-10-05");
        Assert.Equal(40, mixed.GetProperty("initialQuantity").GetInt64());
        Assert.True(mixed.GetProperty("hasReplacements").GetBoolean());
        Assert.Equal("Mixed", mixed.GetProperty("stage").GetString());
        Assert.Equal("Planted", (await Scene(client, "2026-10-04")).GetProperty("stage").GetString());
        var harvest = await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-06", freshMassKg = 1m });
        Assert.Equal("HarvestRecorded", (await Scene(client, "2026-10-06")).GetProperty("stage").GetString());
        Assert.Equal(40, (await Scene(client, "2026-10-06")).GetProperty("initialQuantity").GetInt64());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/harvests/{Id(harvest)}/invalidation", new { expectedVersion = 1, reason = "Colheita registrada por engano" })).StatusCode);
        Assert.Equal("Mixed", (await Scene(client, "2026-10-06")).GetProperty("stage").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Root}/crop-visuals?on=06-10-2026")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/gardens/{Guid.CreateVersion7()}/crop-visuals?on=2026-10-06")).StatusCode);
        var closure = await client.PostAsJsonAsync($"{Root}/cultivation-cycles/{context.Cycle}/closure", new { endedOn = "2026-10-07", expectedVersion = 1, reason = "Encerramento para novo cultivo" });
        Assert.Equal(HttpStatusCode.OK, closure.StatusCode);
        var closed = await ReadAsync(await client.GetAsync($"{Root}/crop-visuals?on=2026-10-07"));
        Assert.Equal(0, closed.GetProperty("totalCount").GetInt32());
    }
}
