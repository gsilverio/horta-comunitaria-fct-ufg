using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HortaComunitaria.Api.Domain.History;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class CatalogWriteTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static string Root => IrrigationTestData.BasePath;
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();

    [Fact]
    public async Task Soil_moisture_classification_is_explicit_and_survives_legacy_migration()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
            await migrator.MigrateAsync("20261008231513_MvpIntegrityChecks");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO environmental_variables (\"Id\", \"Code\", \"Name\", \"Quantity\", \"ReferenceUnit\", \"IsActive\", \"Version\") VALUES ('01a11c0e-0d43-77e9-83f0-25404edc4cc6', 'soil_moisture', 'Umidade antiga', 'Umidade', '%', TRUE, 1)");
            await migrator.MigrateAsync();
            Assert.True((await db.EnvironmentalVariables.SingleAsync()).IsSoilMoisture);
        });
        var response = await client.PostAsJsonAsync("/api/v1/environmental-variables", new { name = "Leitura do solo", quantity = "Umidade", referenceUnit = "%", isSoilMoisture = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var variable = await Body(response);
        Assert.True(variable.GetProperty("isSoilMoisture").GetBoolean());
        Assert.NotEqual("soil_moisture", variable.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Catalogs_accept_names_without_codes_and_preserve_identity_on_correction()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/gardens", new { name = "Horta sem código" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var garden = await Body(created);
        var id = garden.GetProperty("id").GetGuid();
        Assert.Equal(id.ToString("D"), garden.GetProperty("code").GetString());
        var path = $"/api/v1/gardens/{id}";
        foreach (var resource in new[] { "sectors", "beds" })
        {
            var response = await client.PostAsJsonAsync(path + "/" + resource, new { name = "Nome do cadastro" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var item = await Body(response);
            Assert.Equal(item.GetProperty("id").GetString(), item.GetProperty("code").GetString());
        }
        var corrected = await client.PutAsJsonAsync(path, new { name = "Nome corrigido", location = (string?)null,
            institutionalAreaM2 = (decimal?)null, operationalAreaM2 = (decimal?)null, experimentalAreaM2 = (decimal?)null,
            expectedVersion = 1, reason = "Correção do nome" });
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        var updated = await Body(corrected);
        Assert.Equal(id, updated.GetProperty("id").GetGuid());
        Assert.Equal(id.ToString("D"), updated.GetProperty("code").GetString());
        Assert.Equal(2, updated.GetProperty("version").GetInt32());
        Assert.Equal(2, (await Body(await client.GetAsync(path + "/history"))).GetProperty("totalCount").GetInt32());
        // Nomes não são chaves técnicas: dois registros homônimos têm identidades distintas.
        var other = await Body(await client.PostAsJsonAsync("/api/v1/gardens", new { name = "Nome corrigido" }));
        Assert.NotEqual(id, other.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Garden_sector_bed_and_participant_are_created_through_http_with_history()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var garden = await client.PostAsJsonAsync("/api/v1/gardens", new { code = " NEW ", name = " Horta nova " });
        Assert.Equal(HttpStatusCode.Created, garden.StatusCode);
        var g = await Body(garden);
        Assert.Equal("NEW", g.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, g.GetProperty("location").ValueKind);
        var path = $"/api/v1/gardens/{g.GetProperty("id").GetGuid()}";
        var sector = await client.PostAsJsonAsync(path + "/sectors", new { code = "S", name = "Setor" });
        var bed = await client.PostAsJsonAsync(path + "/beds", new { code = "B", name = "Canteiro" });
        var person = await client.PostAsJsonAsync(path + "/participants", new { displayName = "Participante" });
        foreach (var response in new[] { sector, bed, person })
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(response.Headers.Location)).StatusCode);
        }
        var history = await client.GetAsync(path + "/history");
        Assert.Equal(1, (await Body(history)).GetProperty("totalCount").GetInt32());
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(4, await db.AuditEntries.CountAsync());
            Assert.All(await db.AuditEntries.ToListAsync(), x => Assert.Equal(DemoIds.Administrator, x.UserId));
        });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/beds", new { code = "B", name = "Outro" })).StatusCode);
    }

    [Fact]
    public async Task Updates_inactivation_and_no_change_preserve_versions_and_history()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var created = await Body(await client.PostAsJsonAsync(Root + "/beds", new { code = "B5", name = "Canteiro novo" }));
        var path = Root + "/beds/" + created.GetProperty("id").GetGuid();
        var updated = await client.PutAsJsonAsync(path, new { code = "B5", name = " Corrigido ", location = (string?)null, expectedVersion = 1, reason = "Ajuste" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(2, (await Body(updated)).GetProperty("version").GetInt32());
        var unchanged = await client.PutAsJsonAsync(path, new { code = "B5", name = "Corrigido", location = (string?)null, expectedVersion = 2, reason = "Conferência" });
        Assert.Equal(2, (await Body(unchanged)).GetProperty("version").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/inactivation", new { expectedVersion = 1, reason = "Versão antiga" })).StatusCode);
        var inactive = await client.PostAsJsonAsync(path + "/inactivation", new { expectedVersion = 2, reason = "Fora de uso" });
        Assert.False((await Body(inactive)).GetProperty("isActive").GetBoolean());
        Assert.Equal(3, (await Body(await client.GetAsync(path + "/history"))).GetProperty("totalCount").GetInt32());
        Assert.Equal(1, (await Body(await client.GetAsync(Root + "/beds/catalog?isActive=false"))).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Daily_successions_preserve_old_context_and_require_current_bed_version()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var created = await Body(await client.PostAsJsonAsync(Root + "/beds", new { code = "B5", name = "Novo" }));
        var bedId = created.GetProperty("id").GetGuid();
        var path = Root + "/beds/" + bedId;
        var configuration = await client.PostAsJsonAsync(path + "/configurations", new { cultivableAreaM2 = 5m, validFrom = "2026-10-01", expectedBedVersion = 1 });
        Assert.Equal(HttpStatusCode.Created, configuration.StatusCode);
        var initial = await client.PostAsJsonAsync(path + "/sector-assignments", new { sectorId = DemoIds.SectorOne, validFrom = "2026-10-01", expectedBedVersion = 2 });
        Assert.Equal(HttpStatusCode.Created, initial.StatusCode);
        var next = await client.PostAsJsonAsync(path + "/sector-assignments", new { sectorId = DemoIds.SectorTwo, validFrom = "2026-11-10", expectedBedVersion = 3, reason = "Nova organização" });
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
        var old = await Body(await client.GetAsync(path + "?on=2026-11-09"));
        var current = await Body(await client.GetAsync(path + "?on=2026-11-10"));
        Assert.Equal(DemoIds.SectorOne, old.GetProperty("sectorAssignment").GetProperty("sectorId").GetGuid());
        Assert.Equal(DemoIds.SectorTwo, current.GetProperty("sectorAssignment").GetProperty("sectorId").GetGuid());
        Assert.Equal(5m, current.GetProperty("configuration").GetProperty("cultivableAreaM2").GetDecimal());
        var stale = await client.PostAsJsonAsync(path + "/configurations", new { cultivableAreaM2 = 6m, validFrom = "2026-11-11", expectedBedVersion = 3, reason = "Alterar" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Assignments_cannot_contradict_recorded_irrigations_and_database_blocks_overlap()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await IrrigationTestData.CreateAsync(client, IrrigationTestData.Request());
        var path = Root + "/beds/" + DemoIds.Beds[0] + "/sector-assignments";
        var move = await client.PostAsJsonAsync(path, new { sectorId = DemoIds.SectorTwo, validFrom = "2026-10-08", expectedBedVersion = 1, reason = "Mudança" });
        Assert.Equal(HttpStatusCode.Conflict, move.StatusCode);
        Assert.Equal("record.has_dependents", (await Body(move)).GetProperty("code").GetString());
        await factory.WithDbAsync(async db =>
        {
            var overlap = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"""
                INSERT INTO bed_sector_assignments ("Id", "GardenId", "BedId", "IrrigationSectorId", "ValidFrom")
                VALUES ({Guid.CreateVersion7()}, {DemoIds.Garden}, {DemoIds.Beds[0]}, {DemoIds.SectorTwo}, {new DateOnly(2026, 11, 1)})
                """));
            Assert.Equal(PostgresErrorCodes.ExclusionViolation, overlap.SqlState);
            Assert.Equal(1, await db.AuditEntries.CountAsync());
        });
    }

    [Fact]
    public async Task Concurrent_successions_confirm_one_new_context_and_one_history_entry()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var path = Root + "/beds/" + DemoIds.Beds[0] + "/sector-assignments";
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(path, new { sectorId = DemoIds.SectorTwo, validFrom = "2026-11-10", expectedBedVersion = 1, reason = "Mudança A" }),
            client.PostAsJsonAsync(path, new { sectorId = DemoIds.SectorTwo, validFrom = "2026-11-11", expectedBedVersion = 1, reason = "Mudança B" }));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict || x.StatusCode == HttpStatusCode.BadRequest);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(2, await db.BedSectorAssignments.CountAsync(x => x.BedId == DemoIds.Beds[0]));
            Assert.Equal(1, await db.AuditEntries.CountAsync());
        });
    }

    [Fact]
    public async Task Cross_garden_context_invalid_area_and_audit_failure_leave_no_partial_catalog()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var garden = await Body(await client.PostAsJsonAsync("/api/v1/gardens", new { code = "OTHER", name = "Outra" }));
        var path = $"/api/v1/gardens/{garden.GetProperty("id").GetGuid()}/beds/{DemoIds.Beds[0]}/sector-assignments";
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(path, new { sectorId = DemoIds.SectorOne, validFrom = "2026-11-01", expectedBedVersion = 1 })).StatusCode);
        var invalid = await client.PostAsJsonAsync($"{Root}/beds/{DemoIds.Beds[0]}/configurations", new { cultivableAreaM2 = 0m, validFrom = "2026-10-01", expectedBedVersion = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        await factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION fail_catalog_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Controlled audit failure'; END; $$;
            CREATE TRIGGER fail_catalog_audit BEFORE INSERT ON audit_entries FOR EACH ROW EXECUTE FUNCTION fail_catalog_audit();
            """));
        var failed = await client.PostAsJsonAsync(Root + "/beds", new { code = "FAIL", name = "Falha" });
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        await factory.WithDbAsync(async db => Assert.False(await db.Beds.AnyAsync(x => x.Code == "FAIL")));
    }

    [Fact]
    public async Task Water_consumption_exposes_duration_coverage_and_operational_counts_without_multiplication()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await IrrigationTestData.CreateAsync(client, IrrigationTestData.Request());
        await IrrigationTestData.CreateAsync(client, IrrigationTestData.Request(20m) with { Status = HortaComunitaria.Api.Domain.Irrigation.IrrigationStatus.Interrupted, ActiveDurationMinutes = null });
        var result = await Body(await client.GetAsync(IrrigationTestData.ConsumptionPath));
        Assert.Equal(140m, result.GetProperty("summary").GetProperty("knownVolumeLiters").GetDecimal());
        var summary = result.GetProperty("executionSummary");
        Assert.Equal(1, summary.GetProperty("completedCount").GetInt32());
        Assert.Equal(1, summary.GetProperty("interruptedCount").GetInt32());
        Assert.Equal(30m, summary.GetProperty("knownActiveDurationMinutes").GetDecimal());
        Assert.Equal("Partial", summary.GetProperty("durationAvailability").GetString());
    }
}
