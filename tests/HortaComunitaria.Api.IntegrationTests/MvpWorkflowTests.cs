using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class MvpWorkflowTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static string Root => IrrigationTestData.BasePath;
    internal static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    internal static async Task<JsonElement> CreateAsync(HttpClient client, string path, object data)
    {
        var response = await client.PostAsJsonAsync(path, data);
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        Assert.NotNull(response.Headers.Location);
        return await ReadAsync(response);
    }
    internal static Guid Id(JsonElement record) => record.GetProperty("id").GetGuid();
    internal static async Task<JsonElement> IndicatorAsync(HttpClient client, string name, string filters = "")
    {
        var response = await client.GetAsync($"{Root}/indicators/{name}?from=2026-10-01&to=2026-10-31{filters}");
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadAsync(response);
    }
    internal static async Task<(Guid Species, Guid Lot, Guid Cycle)> CultivationAsync(HttpClient client)
    {
        await CreateAsync(client, $"{Root}/beds/{DemoIds.Beds[0]}/configurations", new { validFrom = "2026-10-01", cultivableAreaM2 = 5m, expectedBedVersion = 1 });
        var species = await CreateAsync(client, "/api/v1/species", new { commonName = "Alface" });
        var cultivar = await CreateAsync(client, $"/api/v1/species/{Id(species)}/cultivars", new { name = "Crespa" });
        var lot = await CreateAsync(client, Root + "/propagation-lots", new { code = "L1", speciesId = Id(species), cultivarId = Id(cultivar), materialType = "Seedling" });
        var cycle = await CreateAsync(client, Root + "/cultivation-cycles", new { operationId = Guid.CreateVersion7(), bedId = DemoIds.Beds[0], speciesId = Id(species), cultivarId = Id(cultivar), startedOn = "2026-10-01", occupiedAreaM2 = 5m });
        Assert.Equal(5m, cycle.GetProperty("referenceAreaM2").GetDecimal());
        return (Id(species), Id(lot), Id(cycle));
    }

    [Fact]
    public async Task Cultivation_planting_assessment_observation_harvest_and_drying_are_persisted_with_history()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        var planting = await CreateAsync(client, Root + "/plantings", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, propagationLotId = context.Lot, plantedOn = "2026-10-02", kind = "Initial", plantedQuantity = 40 });
        await CreateAsync(client, Root + "/establishment-assessments", new { operationId = Guid.CreateVersion7(), plantingId = Id(planting), assessedOn = "2026-10-03", survivingQuantity = 36, method = "Contagem" });
        await CreateAsync(client, Root + "/agronomic-observations", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, observedOn = "2026-10-04", category = "Praga", description = "Suspeita sem diagnóstico" });
        var harvest = await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-05", freshMassKg = 10m, usableMassKg = 8m });
        var drying = await CreateAsync(client, Root + "/drying-lots", new { operationId = Guid.CreateVersion7(), code = "SEC1", startedOn = "2026-10-06", items = new[] { new { harvestId = Id(harvest), allocatedMassKg = 5m } } });
        var completed = await client.PostAsJsonAsync($"{Root}/drying-lots/{Id(drying)}/completion", new { endedOn = "2026-10-08", totalDryMassKg = 1m, expectedVersion = 1, reason = "Pesagem final" });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal("Completed", (await ReadAsync(completed)).GetProperty("status").GetString());
        var conflict = await client.PostAsJsonAsync($"{Root}/harvests/{Id(harvest)}/invalidation", new { expectedVersion = 1, reason = "Revisão" });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("record.has_dependents", (await ReadAsync(conflict)).GetProperty("code").GetString());
        var closure = await client.PostAsJsonAsync($"{Root}/cultivation-cycles/{context.Cycle}/closure", new { endedOn = "2026-10-04", expectedVersion = 1, reason = "Encerramento" });
        Assert.Equal(HttpStatusCode.Conflict, closure.StatusCode);
        var detail = await ReadAsync(await client.GetAsync($"{Root}/drying-lots/{Id(drying)}"));
        Assert.Single(detail.GetProperty("items").EnumerateArray());
        Assert.Equal(2, detail.GetProperty("version").GetInt32());
        Assert.Equal(2, (await ReadAsync(await client.GetAsync($"{Root}/drying-lots/{Id(drying)}/history"))).GetProperty("totalCount").GetInt32());
        var establishment = await IndicatorAsync(client, "seedling-establishment");
        Assert.Equal(90m, establishment.GetProperty("groups").GetProperty("items")[0].GetProperty("percentage").GetDecimal());
        var productivity = await IndicatorAsync(client, "productivity");
        Assert.Equal(2m, productivity.GetProperty("origins").GetProperty("items")[0].GetProperty("productivityKgPerM2").GetDecimal());
        Assert.Equal(20m, (await IndicatorAsync(client, "harvest-losses")).GetProperty("percentage").GetDecimal());
        Assert.Equal(20m, (await IndicatorAsync(client, "drying-yield")).GetProperty("percentage").GetDecimal());
        Assert.Equal(20m, (await IndicatorAsync(client, "drying-yield", $"&speciesId={context.Species}")).GetProperty("percentage").GetDecimal());
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Plantings.CountAsync());
            Assert.Equal(1, await db.EstablishmentAssessments.CountAsync());
            Assert.Equal(5m, await db.DryingItems.Select(x => x.AllocatedMassKg).SingleAsync());
            Assert.Equal(DemoIds.Operator, await db.Harvests.Select(x => x.RecordedByUserId).SingleAsync());
        });
    }

    [Fact]
    public async Task Manual_environmental_measurement_preserves_zero_quality_context_retry_and_correction()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var variable = await CreateAsync(client, "/api/v1/environmental-variables", new { code = "soil_moisture", name = "Umidade", quantity = "Umidade do solo", referenceUnit = "%" });
        var point = await CreateAsync(client, Root + "/sampling-points", new { code = "P1", description = "Ponto no canteiro", bedId = DemoIds.Beds[0] });
        var input = new { operationId = Guid.CreateVersion7(), samplingPointId = Id(point), variableId = Id(variable), collectedAt = "2026-10-08T08:00:00-03:00", value = 0m, unit = "%", method = "Capacitivo", quality = "Questionable" };
        var measurement = await CreateAsync(client, Root + "/environmental-measurements", input);
        Assert.Equal(0m, measurement.GetProperty("value").GetDecimal());
        Assert.Equal("Questionable", measurement.GetProperty("quality").GetString());
        Assert.Equal(DemoIds.SectorOne, measurement.GetProperty("sectorId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Root + "/environmental-measurements", input)).StatusCode);
        var correction = await client.PutAsJsonAsync($"{Root}/environmental-measurements/{Id(measurement)}", new { input.samplingPointId, input.variableId, input.collectedAt, value = 10m, input.unit, input.method, input.quality, responsibleParticipantId = (Guid?)null, notes = (string?)null, expectedVersion = 1, reason = "Leitura corrigida" });
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        Assert.Equal(2, (await ReadAsync(correction)).GetProperty("version").GetInt32());
        var displaced = await client.PutAsJsonAsync($"{Root}/sampling-points/{Id(point)}", new { code = "P1", description = "Ponto no canteiro", location = (string?)null, bedId = (Guid?)null, sectorId = DemoIds.SectorTwo, expectedVersion = 1, reason = "Deslocar" });
        Assert.Equal(HttpStatusCode.Conflict, displaced.StatusCode);
        var history = await ReadAsync(await client.GetAsync($"{Root}/environmental-measurements/{Id(measurement)}/history"));
        Assert.Equal(2, history.GetProperty("totalCount").GetInt32());
        var moisture = await IndicatorAsync(client, "soil-moisture");
        Assert.Equal(1, moisture.GetProperty("questionableCount").GetInt32());
        Assert.Equal(10m, moisture.GetProperty("groups").GetProperty("items")[0].GetProperty("lastObservation").GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task Activities_institutions_inputs_occurrence_resolution_and_costs_use_real_write_endpoints()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var input = await CreateAsync(client, "/api/v1/inputs", new { name = "Adubo", referenceUnit = "kg" });
        var institution = await CreateAsync(client, "/api/v1/institutions", new { name = "UFG" });
        var person = await CreateAsync(client, Root + "/participants", new { displayName = "Outra pessoa" });
        var occurrence = await CreateAsync(client, Root + "/occurrences", new { operationId = Guid.CreateVersion7(), openedAt = "2026-10-08T07:00:00-03:00", category = "Irrigação", description = "Falha no setor", sectorId = DemoIds.SectorOne });
        var activityInput = new
        {
            operationId = Guid.CreateVersion7(),
            title = "Inspeção",
            category = "Manutenção",
            performedOn = "2026-10-08",
            sectorId = DemoIds.SectorOne,
            occurrenceId = Id(occurrence),
            isSocial = true,
            isEducational = true,
            reportedAudienceTotal = 20,
            audienceListComplete = false,
            participants = new[] { new { participantId = DemoIds.Participant, workHours = (decimal?)2m, countsAsAudience = true }, new { participantId = Id(person), workHours = (decimal?)null, countsAsAudience = true } },
            inputUsages = new[] { new { inputId = Id(input), quantity = 1m, unit = "kg" }, new { inputId = Id(input), quantity = 2m, unit = "kg" } },
            institutions = new[] { new { institutionId = Id(institution), role = "Apoio" } }
        };
        var activity = await CreateAsync(client, Root + "/activities", activityInput);
        Assert.Equal(2, activity.GetProperty("inputUsages").GetArrayLength());
        Assert.True(activity.GetProperty("isOperational").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Root + "/activities", activityInput)).StatusCode);
        var resolve = await client.PostAsJsonAsync($"{Root}/occurrences/{Id(occurrence)}/resolution", new { resolvedAt = "2026-10-08T09:00:00-03:00", resolution = "Reparo concluído", expectedVersion = 1, reason = "Concluir acompanhamento" });
        Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);
        Assert.Equal("Resolved", (await ReadAsync(resolve)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/occurrences/{Id(occurrence)}/invalidation", new { expectedVersion = 2, reason = "Invalidar" })).StatusCode);
        await CreateAsync(client, Root + "/financial-entries", new { operationId = Guid.CreateVersion7(), entryDate = "2026-10-08", description = "Previsão", category = "Manutenção", amountBrl = 90m, nature = "Planned" });
        var actual = await CreateAsync(client, Root + "/financial-entries", new { operationId = Guid.CreateVersion7(), entryDate = "2026-10-08", description = "Compra", category = "Manutenção", amountBrl = 100m, nature = "Actual" });
        var correction = await client.PutAsJsonAsync($"{Root}/financial-entries/{Id(actual)}", new { entryDate = "2026-10-08", description = "Compra", category = "Manutenção", amountBrl = 95m, nature = "Actual", bedId = (Guid?)null, cycleId = (Guid?)null, expectedVersion = 1, reason = "Conferência da nota" });
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Activities.CountAsync());
            Assert.Equal(2, await db.ActivityParticipants.CountAsync());
            Assert.Equal(2, await db.InputUsages.CountAsync());
            Assert.Equal(1, await db.ActivityInstitutions.CountAsync());
            Assert.Equal(185m, await db.FinancialEntries.SumAsync(x => x.AmountBrl));
        });
        Assert.Equal(2, (await ReadAsync(await client.GetAsync($"{Root}/financial-entries/{Id(actual)}/history"))).GetProperty("totalCount").GetInt32());
        var hours = await IndicatorAsync(client, "work-hours");
        Assert.Equal(2m, hours.GetProperty("knownHours").GetDecimal());
        Assert.Equal("Partial", hours.GetProperty("availability").GetString());
        var audience = await IndicatorAsync(client, "social-educational-participation");
        Assert.Equal(1, audience.GetProperty("activityCount").GetInt32());
        Assert.Equal(1, audience.GetProperty("socialCount").GetInt32());
        Assert.Equal(1, audience.GetProperty("educationalCount").GetInt32());
        Assert.Equal(20, audience.GetProperty("knownAudience").GetInt32());
        Assert.Equal(2, audience.GetProperty("identifiedPeople").GetInt32());
        var occurrences = await IndicatorAsync(client, "operational-occurrences");
        Assert.Equal(1, occurrences.GetProperty("count").GetInt32());
        Assert.Equal(1, occurrences.GetProperty("resolvedCount").GetInt32());
        var costs = await IndicatorAsync(client, "basic-costs", "&pageSize=1");
        Assert.Equal(90m, costs.GetProperty("plannedBrl").GetDecimal());
        Assert.Equal(95m, costs.GetProperty("actualBrl").GetDecimal());
        Assert.Equal(5m, costs.GetProperty("differenceBrl").GetDecimal());
    }
}
