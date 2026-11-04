using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static HortaComunitaria.Api.IntegrationTests.MvpWorkflowTests;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class MvpIntegrityTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static string Root => IrrigationTestData.BasePath;

    [Fact]
    public async Task Establishment_weights_quantities_selects_latest_and_keeps_replacements_and_unknown_material_separate()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        async Task<Guid> Plant(int quantity, string kind = "Initial", Guid? lot = null) => Id(await CreateAsync(client, Root + "/plantings", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, propagationLotId = lot, plantedOn = "2026-10-02", kind, plantedQuantity = quantity }));
        async Task Assess(Guid plant, int quantity, string date) => await CreateAsync(client, Root + "/establishment-assessments", new { operationId = Guid.CreateVersion7(), plantingId = plant, assessedOn = date, survivingQuantity = quantity, method = "Contagem" });
        var first = await Plant(40, lot: context.Lot);
        var second = await Plant(10, lot: context.Lot);
        await Assess(first, 36, "2026-10-03");
        await Assess(second, 5, "2026-10-03");
        var initial = await IndicatorAsync(client, "seedling-establishment");
        Assert.Equal(82m, initial.GetProperty("groups").GetProperty("items")[0].GetProperty("percentage").GetDecimal());
        await Assess(first, 32, "2026-10-04");
        await Plant(8, lot: context.Lot); // Known material, no assessment.
        await Plant(4, "Replacement", context.Lot);
        await Plant(6); // Unknown material.
        var final = await IndicatorAsync(client, "seedling-establishment");
        var group = final.GetProperty("groups").GetProperty("items").EnumerateArray().Single(x => x.GetProperty("kind").GetString() == "Initial" && x.GetProperty("method").GetString() == "Contagem");
        Assert.Equal(74m, group.GetProperty("percentage").GetDecimal());
        Assert.Equal(50, group.GetProperty("assessedPlantedQuantity").GetInt64());
        Assert.Equal(2, final.GetProperty("withoutAssessmentCount").GetInt32());
        Assert.Equal(1, final.GetProperty("ineligibleMaterialCount").GetInt32());
        Assert.Equal("Partial", final.GetProperty("availability").GetString());
        var invalid = await client.PostAsJsonAsync(Root + "/establishment-assessments", new { operationId = Guid.CreateVersion7(), plantingId = first, assessedOn = "2026-10-03", survivingQuantity = 41, method = "Contagem" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/plantings/{first}/invalidation", new { expectedVersion = 1, reason = "Revisão" })).StatusCode);
    }

    [Fact]
    public async Task Harvest_area_is_not_multiplied_and_unknown_usable_mass_is_not_zero_loss()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-05", freshMassKg = 3m, usableMassKg = 2.4m });
        var unknown = await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-06", freshMassKg = 2m });
        var productivity = await IndicatorAsync(client, "productivity", "&pageSize=1");
        Assert.Equal(5m, productivity.GetProperty("freshMassKg").GetDecimal());
        Assert.Equal(1m, productivity.GetProperty("origins").GetProperty("items")[0].GetProperty("productivityKgPerM2").GetDecimal());
        var losses = await IndicatorAsync(client, "harvest-losses", "&pageSize=1");
        Assert.Equal(0.6m, losses.GetProperty("knownLossKg").GetDecimal());
        Assert.Equal(20m, losses.GetProperty("percentage").GetDecimal());
        Assert.Equal("Partial", losses.GetProperty("availability").GetString());
        var detail = await ReadAsync(await client.GetAsync($"{Root}/harvests/{Id(unknown)}"));
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("remainingUsableMassKg").ValueKind);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root + "/drying-lots", new { operationId = Guid.CreateVersion7(), code = "UNKNOWN", startedOn = "2026-10-06", items = new[] { new { harvestId = Id(unknown), allocatedMassKg = 1m } } })).StatusCode);
    }

    [Fact]
    public async Task Parallel_drying_allocations_cannot_exceed_balance_and_invalidation_releases_it()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        var harvest = await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-05", freshMassKg = 5m, usableMassKg = 4m });
        object Lot(string code) => new { operationId = Guid.CreateVersion7(), code, startedOn = "2026-10-06", items = new[] { new { harvestId = Id(harvest), allocatedMassKg = 3m } } };
        var results = await Task.WhenAll(client.PostAsJsonAsync(Root + "/drying-lots", Lot("A")), client.PostAsJsonAsync(Root + "/drying-lots", Lot("B")));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created);
        var failure = Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("drying.insufficient_balance", (await ReadAsync(failure)).GetProperty("code").GetString());
        var accepted = await ReadAsync(results.Single(x => x.StatusCode == HttpStatusCode.Created));
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.DryingLots.CountAsync());
            Assert.Equal(1, await db.DryingItems.CountAsync());
            Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.EntityType == "DryingLot"));
        });
        var balance = await ReadAsync(await client.GetAsync($"{Root}/harvests/{Id(harvest)}"));
        Assert.Equal(1m, balance.GetProperty("remainingUsableMassKg").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/drying-lots/{Id(accepted)}/invalidation", new { expectedVersion = 1, reason = "Destinação desfeita" })).StatusCode);
        await CreateAsync(client, Root + "/drying-lots", Lot("C"));
    }

    [Fact]
    public async Task Activities_do_not_multiply_hours_or_audience_and_audit_failure_rolls_back_children()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var person2 = Id(await CreateAsync(client, Root + "/participants", new { displayName = "Pessoa 2" }));
        var person3 = Id(await CreateAsync(client, Root + "/participants", new { displayName = "Pessoa 3" }));
        var input = Id(await CreateAsync(client, "/api/v1/inputs", new { name = "Adubo", referenceUnit = "kg" }));
        var institution = Id(await CreateAsync(client, "/api/v1/institutions", new { name = "Universidade" }));
        object Activity(string title) => new
        {
            operationId = Guid.CreateVersion7(),
            title,
            category = "Mutirão",
            performedOn = "2026-10-08",
            isSocial = true,
            isEducational = true,
            reportedAudienceTotal = 20,
            audienceListComplete = false,
            participants = new[] { new { participantId = DemoIds.Participant, workHours = 2m, countsAsAudience = true }, new { participantId = person2, workHours = 2m, countsAsAudience = true }, new { participantId = person3, workHours = 2m, countsAsAudience = true } },
            inputUsages = new[] { new { inputId = input, quantity = 1m, unit = "kg" }, new { inputId = input, quantity = 2m, unit = "kg" } },
            institutions = new[] { new { institutionId = institution } }
        };
        await CreateAsync(client, Root + "/activities", Activity("Primeiro"));
        Assert.Equal(6m, (await IndicatorAsync(client, "work-hours")).GetProperty("knownHours").GetDecimal());
        var audience = await IndicatorAsync(client, "social-educational-participation");
        Assert.Equal(20, audience.GetProperty("knownAudience").GetInt32());
        Assert.Equal(3, audience.GetProperty("identifiedPeople").GetInt32());
        await CreateAsync(client, Root + "/activities", Activity("Segundo"));
        audience = await IndicatorAsync(client, "social-educational-participation", "&pageSize=1");
        Assert.Equal(40, audience.GetProperty("knownAudience").GetInt32());
        Assert.Equal(6, audience.GetProperty("nominalPresences").GetInt32());
        Assert.Equal(3, audience.GetProperty("identifiedPeople").GetInt32());
        await factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION fail_activity_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Controlled audit failure'; END; $$;
            CREATE TRIGGER fail_activity_audit BEFORE INSERT ON audit_entries FOR EACH ROW EXECUTE FUNCTION fail_activity_audit();
            """));
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync(Root + "/activities", Activity("Falha"))).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(2, await db.Activities.CountAsync());
            Assert.Equal(6, await db.ActivityParticipants.CountAsync());
            Assert.Equal(4, await db.InputUsages.CountAsync());
            Assert.Equal(2, await db.ActivityInstitutions.CountAsync());
        });
    }

    [Fact]
    public async Task Cost_retry_version_invalidation_and_validation_preserve_known_zero_and_history()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var input = new { operationId = Guid.CreateVersion7(), entryDate = "2026-10-08", description = "Custo conhecido", category = "Manutenção", amountBrl = 0m, nature = "Actual" };
        var record = await CreateAsync(client, Root + "/financial-entries", input);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Root + "/financial-entries", input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Root + "/financial-entries", new { input.operationId, input.entryDate, input.description, input.category, amountBrl = 1m, input.nature })).StatusCode);
        var costs = await IndicatorAsync(client, "basic-costs");
        Assert.Equal(0m, costs.GetProperty("actualBrl").GetDecimal());
        Assert.Equal(JsonValueKind.Null, costs.GetProperty("plannedBrl").ValueKind);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root + "/financial-entries", new { operationId = Guid.CreateVersion7(), input.entryDate, input.description, input.category, amountBrl = 0.001m, input.nature })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root + "/financial-entries", new { operationId = Guid.CreateVersion7(), input.entryDate, input.description, input.category, input.amountBrl, nature = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/financial-entries/{Id(record)}/invalidation", new { expectedVersion = 2, reason = "Revisão" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/financial-entries/{Id(record)}/invalidation", new { expectedVersion = 1, reason = "Revisão" })).StatusCode);
        Assert.Equal("NoData", (await IndicatorAsync(client, "basic-costs")).GetProperty("availability").GetString());
        var history = await ReadAsync(await client.GetAsync($"{Root}/financial-entries/{Id(record)}/history"));
        Assert.Equal(2, history.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Empty_indicators_and_all_fact_lists_are_queryable_and_reject_numeric_enum_filters()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        foreach (var indicator in new[] { "soil-moisture", "seedling-establishment", "productivity", "harvest-losses", "drying-yield", "work-hours", "operational-occurrences", "social-educational-participation", "basic-costs" })
            Assert.Equal("NoData", (await IndicatorAsync(client, indicator)).GetProperty("availability").GetString());
        foreach (var resource in new[] { "environmental-measurements", "cultivation-cycles", "plantings", "establishment-assessments", "agronomic-observations", "harvests", "drying-lots", "activities", "occurrences", "financial-entries" })
        {
            var response = await client.GetAsync($"{Root}/{resource}?from=2026-10-01&to=2026-10-31");
            Assert.True(response.StatusCode == HttpStatusCode.OK, resource + ": " + await response.Content.ReadAsStringAsync());
            Assert.Equal(0, (await ReadAsync(response)).GetProperty("totalCount").GetInt32());
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Root}/{resource}?from=2026-10-01&to=2026-10-31&validity=0")).StatusCode);
        }
    }

    [Fact]
    public async Task Cross_garden_cycle_is_rejected_by_api_and_composite_foreign_key()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        var other = Id(await CreateAsync(client, "/api/v1/gardens", new { code = "OTHER", name = "Outra horta" }));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/gardens/{other}/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-05", freshMassKg = 1m })).StatusCode);
        var harvest = await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-05", freshMassKg = 1m });
        await factory.WithDbAsync(async db =>
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE harvests SET \"GardenId\" = {other} WHERE \"Id\" = {Id(harvest)}"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        });
    }

    [Fact]
    public async Task Activity_correction_replaces_children_atomically_noop_keeps_version_and_stale_changes_conflict()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var person = Id(await CreateAsync(client, Root + "/participants", new { displayName = "Pessoa nova" }));
        var input = Id(await CreateAsync(client, "/api/v1/inputs", new { name = "Adubo", referenceUnit = "kg" }));
        var record = await CreateAsync(client, Root + "/activities", new { operationId = Guid.CreateVersion7(), title = "Trabalho", category = "Mutirão", performedOn = "2026-10-08", participants = new[] { new { participantId = DemoIds.Participant, workHours = 2m } }, inputUsages = Array.Empty<object>(), institutions = Array.Empty<object>() });
        object Correction(int version) => new
        {
            title = "Trabalho",
            category = "Mutirão",
            performedOn = "2026-10-08",
            description = (string?)null,
            bedId = (Guid?)null,
            sectorId = (Guid?)null,
            cycleId = (Guid?)null,
            occurrenceId = (Guid?)null,
            isOperational = true,
            isSocial = false,
            isEducational = false,
            objective = (string?)null,
            modality = (string?)null,
            reportedAudienceTotal = (int?)null,
            audienceListComplete = (bool?)null,
            participants = new[] { new { participantId = person, role = (string?)null, workHours = 3m, countsAsAudience = false } },
            inputUsages = new[] { new { inputId = input, quantity = 1m, unit = "kg" } },
            institutions = Array.Empty<object>(),
            expectedVersion = version,
            reason = "Conferência dos executantes"
        };
        var path = $"{Root}/activities/{Id(record)}";
        var response = await client.PutAsJsonAsync(path, Correction(1));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Assert.Equal(2, (await ReadAsync(response)).GetProperty("version").GetInt32());
        response = await client.PutAsJsonAsync(path, Correction(2));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await ReadAsync(response)).GetProperty("version").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path, Correction(1))).StatusCode);
        Assert.Equal(3m, (await IndicatorAsync(client, "work-hours")).GetProperty("knownHours").GetDecimal());
        Assert.Equal(2, (await ReadAsync(await client.GetAsync(path + "/history"))).GetProperty("totalCount").GetInt32());
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(person, await db.ActivityParticipants.Select(x => x.ParticipantId).SingleAsync());
            Assert.Equal(1, await db.InputUsages.CountAsync());
        });
    }

    [Fact]
    public async Task Bed_successions_cannot_contradict_existing_future_measurement_or_frozen_cycle_area()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        await CreateAsync(client, Root + "/cultivation-cycles", new { operationId = Guid.CreateVersion7(), bedId = DemoIds.Beds[0], speciesId = context.Species, startedOn = "2026-11-10", occupiedAreaM2 = 4m });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/beds/{DemoIds.Beds[0]}/configurations", new { validFrom = "2026-11-09", cultivableAreaM2 = 3m, expectedBedVersion = 2, reason = "Reconfigurar área" })).StatusCode);
        var variable = Id(await CreateAsync(client, "/api/v1/environmental-variables", new { code = "soil_moisture", name = "Umidade", quantity = "Umidade", referenceUnit = "%" }));
        var point = Id(await CreateAsync(client, Root + "/sampling-points", new { code = "P1", description = "Ponto", bedId = DemoIds.Beds[0] }));
        await CreateAsync(client, Root + "/environmental-measurements", new { operationId = Guid.CreateVersion7(), variableId = variable, samplingPointId = point, collectedAt = "2026-11-10T08:00:00-03:00", value = 30m, unit = "%", method = "Manual", quality = "Normal" });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Root}/beds/{DemoIds.Beds[0]}/sector-assignments", new { sectorId = DemoIds.SectorTwo, validFrom = "2026-11-09", expectedBedVersion = 2, reason = "Novo setor" })).StatusCode);
        await factory.WithDbAsync(async db => Assert.Null(await db.BedConfigurations.Select(x => x.ValidUntil).SingleAsync()));
    }

    [Fact]
    public async Task Mixed_drying_lot_has_whole_lot_yield_but_no_species_attribution()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var context = await CultivationAsync(client);
        var species = Id(await CreateAsync(client, "/api/v1/species", new { commonName = "Outra espécie" }));
        var cycle = Id(await CreateAsync(client, Root + "/cultivation-cycles", new { operationId = Guid.CreateVersion7(), bedId = DemoIds.Beds[0], speciesId = species, startedOn = "2026-10-01", occupiedAreaM2 = 5m }));
        var h1 = Id(await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = context.Cycle, harvestedOn = "2026-10-05", freshMassKg = 3m, usableMassKg = 3m }));
        var h2 = Id(await CreateAsync(client, Root + "/harvests", new { operationId = Guid.CreateVersion7(), cycleId = cycle, harvestedOn = "2026-10-05", freshMassKg = 2m, usableMassKg = 2m }));
        var lot = Id(await CreateAsync(client, Root + "/drying-lots", new { operationId = Guid.CreateVersion7(), code = "MIX", startedOn = "2026-10-06", items = new[] { new { harvestId = h1, allocatedMassKg = 3m }, new { harvestId = h2, allocatedMassKg = 2m } } }));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Root}/drying-lots/{lot}/completion", new { endedOn = "2026-10-08", totalDryMassKg = 1m, expectedVersion = 1, reason = "Pesagem" })).StatusCode);
        Assert.Equal(20m, (await IndicatorAsync(client, "drying-yield")).GetProperty("percentage").GetDecimal());
        var filtered = await IndicatorAsync(client, "drying-yield", $"&speciesId={context.Species}");
        Assert.Equal(JsonValueKind.Null, filtered.GetProperty("percentage").ValueKind);
        Assert.Equal(1, filtered.GetProperty("mixedSpeciesCount").GetInt32());
        Assert.Equal("Unavailable", filtered.GetProperty("availability").GetString());
    }

    [Fact]
    public async Task New_openapi_operations_describe_required_periods_and_actual_creation_response()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var root = await ReadAsync(await client.GetAsync("/openapi/v1.json"));
        var paths = root.GetProperty("paths");
        var indicatorPaths = paths.EnumerateObject().Where(x => x.Name.Contains("/indicators/", StringComparison.Ordinal)).ToArray();
        Assert.Equal(10, indicatorPaths.Length);
        foreach (var path in indicatorPaths)
            foreach (var name in new[] { "from", "to" })
            {
                var parameter = path.Value.GetProperty("get").GetProperty("parameters").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name);
                Assert.True(parameter.GetProperty("required").GetBoolean());
                Assert.Equal("date", parameter.GetProperty("schema").GetProperty("format").GetString());
            }
        var post = paths.GetProperty("/api/v1/gardens/{gardenId}/harvests").GetProperty("post");
        Assert.True(post.GetProperty("responses").TryGetProperty("200", out _));
        var schema = post.GetProperty("responses").GetProperty("201").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.EndsWith("/HarvestResponse", schema.GetProperty("$ref").GetString());
    }
}
