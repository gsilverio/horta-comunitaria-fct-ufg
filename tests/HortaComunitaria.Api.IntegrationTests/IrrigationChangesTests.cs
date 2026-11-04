using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;
using HortaComunitaria.Api.Features.Irrigation.ListIrrigationHistory;
using HortaComunitaria.Api.Features.Irrigation.ListIrrigations;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;
using static HortaComunitaria.Api.IntegrationTests.IrrigationChangesTestData;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationChangesTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Creation_correction_invalidation_preserve_fact_and_history_and_refresh_consumption()
    {
        var clock = new AdjustableClock();
        using var factory = await postgres.CreateFactoryAsync(customize: services => services.AddSingleton<TimeProvider>(clock));
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        clock.Now = FixedTimeProvider.Now.AddHours(1);
        var corrected = await DetailAsync(await PutAsync(client, created.Id, Correction(original) with { Reason = " Conferência da coleta " }));
        Assert.Equal(100m, corrected.VolumeLiters);
        Assert.Equal(2, corrected.Version);
        Assert.Equal(clock.Now, corrected.UpdatedAt);
        Assert.Equal(created.RecordedAt, corrected.RecordedAt);
        Assert.Equal(created.RecordedBy, corrected.RecordedBy);
        Assert.Equal(100m, (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!.Summary.KnownVolumeLiters);
        var replay = await DetailAsync(await PostAsync(client, original));
        Assert.Equal(corrected.Id, replay.Id);
        Assert.Equal(100m, replay.VolumeLiters);
        Assert.Equal(2, replay.Version);
        clock.Now = clock.Now.AddHours(1);
        var invalid = await DetailAsync(await InvalidateAsync(client, created.Id, 2));
        Assert.Equal(3, invalid.Version);
        Assert.Equal(RecordValidity.Invalid, invalid.Validity);
        Assert.Equal(100m, invalid.VolumeLiters);
        Assert.Equal(4, invalid.Beds.Count);
        Assert.Equal(created.RecordedAt, invalid.RecordedAt);
        Assert.Equal(created.RecordedBy, invalid.RecordedBy);
        Assert.Equal(clock.Now, invalid.UpdatedAt);
        var report = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(new WaterCoverage(null, 0, 0, 0, ConsumptionAvailability.NoData, false), report.Summary);
        Assert.Empty((await client.GetFromJsonAsync<Page<IrrigationSummaryResponse>>(ListPath, Json))!.Items);
        Assert.Equal(created.Id, Assert.Single((await client.GetFromJsonAsync<Page<IrrigationSummaryResponse>>(ListPath + "&validity=Invalid", Json))!.Items).Id);
        var history = (await client.GetFromJsonAsync<Page<IrrigationHistoryResponse>>(Path(created.Id) + "/history", Json))!;
        Assert.Equal(new[] { AuditOperation.Invalidated, AuditOperation.Corrected, AuditOperation.Created }, history.Items.Select(x => x.Operation));
        Assert.Equal(120m, history.Items[1].Before!.VolumeLiters);
        Assert.Equal(100m, history.Items[1].After.VolumeLiters);
        Assert.Equal("Conferência da coleta", history.Items[1].Reason);
        Assert.Equal(2, history.Items[0].Before!.Version);
        Assert.Equal(3, history.Items[0].After.Version);
        Assert.Equal(RecordValidity.Invalid, history.Items[0].After.Validity);
        Assert.Null(history.Items[2].Before);
        Assert.Null(history.Items[2].Reason);
        Assert.All(history.Items, x => { Assert.True(x.Actor.IsDemonstration); Assert.Equal(DemoIds.Operator, x.Actor.Id); });
        var invalidReplay = await DetailAsync(await PostAsync(client, original));
        Assert.Equal(3, invalidReplay.Version);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Irrigations.CountAsync());
            Assert.Equal(4, await db.IrrigationBeds.CountAsync());
            Assert.Equal(3, await db.AuditEntries.CountAsync());
            Assert.Equal(original.OperationId, (await db.Irrigations.SingleAsync()).OperationId);
            using var initial = JsonDocument.Parse((await db.Irrigations.SingleAsync()).CreationPayload);
            Assert.Equal(120m, initial.RootElement.GetProperty("volumeLiters").GetDecimal());
        });
    }

    [Fact]
    public async Task Submicrosecond_input_and_clock_do_not_create_false_corrections_or_break_creation_replay()
    {
        var clock = new AdjustableClock { Now = FixedTimeProvider.Now.AddTicks(9) };
        using var factory = await postgres.CreateFactoryAsync(customize: services => services.AddSingleton<TimeProvider>(clock));
        using var client = factory.CreateClient();
        var baseline = Request();
        var original = baseline with { StartedAt = baseline.StartedAt.AddTicks(7), EndedAt = baseline.EndedAt?.AddTicks(7) };
        var created = await CreateAsync(client, original);
        Assert.Equal(FixedTimeProvider.Now, created.RecordedAt);
        var unchanged = await DetailAsync(await PutAsync(client, created.Id, Correction(original, 120m)));
        Assert.Equal(1, unchanged.Version);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original)));
        var replayed = await DetailAsync(await PostAsync(client, original));
        Assert.Equal(2, replayed.Version);
        var history = (await client.GetFromJsonAsync<Page<IrrigationHistoryResponse>>(Path(created.Id) + "/history", Json))!;
        Assert.Equal(2, history.TotalCount);
        Assert.Equal(created.StartedAt, history.Items[1].After.StartedAt);
        Assert.Equal(replayed.UpdatedAt, history.Items[0].After.UpdatedAt);
    }

    [Fact]
    public async Task Normalized_no_change_does_not_advance_version_time_or_history_but_stale_version_conflicts()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        var same = Correction(original, 120.000m) with
        {
            BedIds = original.BedIds.Reverse().ToArray(),
            StartedAt = original.StartedAt.ToUniversalTime(),
            EndedAt = original.EndedAt?.ToUniversalTime(),
            VolumeMethod = " Recipiente graduado ",
            Notes = " ",
            Reason = "Sem mudança"
        };
        var unchanged = await DetailAsync(await PutAsync(client, created.Id, same));
        Assert.Equal(1, unchanged.Version);
        Assert.Null(unchanged.UpdatedAt);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original)));
        var stale = await PutAsync(client, created.Id, Correction(original));
        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "irrigation.version_conflict");
        Assert.Equal(2, (await DetailAsync(await PutAsync(client, created.Id, Correction(original, version: 2)))).Version);
        await factory.WithDbAsync(async db => Assert.Equal(2, await db.AuditEntries.CountAsync()));
    }

    [Fact]
    public async Task Corrections_change_unknown_known_zero_and_back_without_inventing_volume()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original, null)));
        Assert.Equal(ConsumptionAvailability.Unavailable, (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!.Summary.Availability);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original, 20m, 2) with { Status = IrrigationStatus.Interrupted, VolumeSource = VolumeSource.Estimated }));
        var estimated = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(new WaterCoverage(20m, 1, 1, 0, ConsumptionAvailability.Available, true), estimated.Summary);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original, 0m, 3)));
        Assert.Equal(0m, (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!.Summary.KnownVolumeLiters);
        await DetailAsync(await PutAsync(client, created.Id, Correction(original, null, 4)));
        var unknown = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!.Summary;
        Assert.Null(unknown.KnownVolumeLiters);
        Assert.False(unknown.IncludesEstimatedVolume);
        Assert.Equal(1, unknown.EventsWithoutVolume);
    }

    [Fact]
    public async Task Correction_removes_and_adds_beds_and_moves_sector_date_with_audited_historical_context()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        Assert.Equal(2, (await DetailAsync(await PutAsync(client, created.Id, Correction(original) with { BedIds = DemoIds.Beds.Take(2).ToArray() }))).Beds.Count);
        Assert.Equal(4, (await DetailAsync(await PutAsync(client, created.Id, Correction(original, version: 2)))).Beds.Count);
        var assignments = new List<Guid>();
        await factory.WithDbAsync(async db =>
        {
            var cutoff = new DateOnly(2026, 10, 9);
            await db.Database.ExecuteSqlAsync($"UPDATE bed_sector_assignments SET \"ValidUntil\" = {cutoff}");
            var sector = await db.IrrigationSectors.SingleAsync(x => x.Id == DemoIds.SectorTwo);
            var existing = await db.BedSectorAssignments.ToListAsync();
            foreach (var bed in await db.Beds.ToListAsync())
            {
                var link = BedSectorAssignment.Create(bed, sector, cutoff, null, existing).Value;
                assignments.Add(link.Id);
                db.Add(link);
            }
            await db.SaveChangesAsync();
        });
        var moved = await DetailAsync(await PutAsync(client, created.Id, Correction(original, version: 3) with
        { SectorId = DemoIds.SectorTwo, StartedAt = original.StartedAt.AddDays(1), EndedAt = original.EndedAt?.AddDays(1) }));
        Assert.Equal(4, moved.Version);
        Assert.Equal(assignments.Order(), moved.Beds.Select(x => x.AssignmentId).Order());
        Assert.Equal(ConsumptionAvailability.NoData, (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!.Summary.Availability);
        var next = (await client.GetFromJsonAsync<WaterConsumptionResponse>(BasePath + "/indicators/water-consumption?from=2026-10-09&to=2026-10-09", Json))!;
        Assert.Equal(100m, next.BySector.Items.Single(x => x.SectorId == DemoIds.SectorTwo).KnownVolumeLiters);
        var history = (await client.GetFromJsonAsync<Page<IrrigationHistoryResponse>>(Path(created.Id) + "/history", Json))!;
        Assert.Equal(DemoIds.SectorOne, history.Items[0].Before!.SectorId);
        Assert.Equal(DemoIds.Assignments.Order(), history.Items[0].Before!.Beds.Select(x => x.AssignmentId).Order());
        Assert.Equal(DemoIds.SectorTwo, history.Items[0].After.SectorId);
        Assert.Equal(assignments.Order(), history.Items[0].After.Beds.Select(x => x.AssignmentId).Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Audit_failure_rolls_back_correction_or_invalidation_and_children(bool invalidation)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        await factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION test_fail_changes() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW."Operation" <> 'created'::audit_operation THEN RAISE EXCEPTION 'Controlled alteration audit failure'; END IF; RETURN NEW; END;
            $$;
            CREATE TRIGGER test_change_audit_failure BEFORE INSERT ON audit_entries FOR EACH ROW EXECUTE FUNCTION test_fail_changes();
            """));
        var response = invalidation ? await InvalidateAsync(client, created.Id, 1) :
            await PutAsync(client, created.Id, Correction(original) with { BedIds = DemoIds.Beds.Take(2).ToArray() });
        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "server.unexpected_error");
        await factory.WithDbAsync(async db =>
        {
            var persisted = await db.Irrigations.SingleAsync();
            Assert.Equal(1, persisted.Version);
            Assert.Equal(RecordValidity.Valid, persisted.Validity);
            Assert.Equal(120m, persisted.VolumeLiters);
            Assert.Null(persisted.UpdatedAt);
            Assert.Equal(4, await db.IrrigationBeds.CountAsync());
            Assert.Equal(1, await db.AuditEntries.CountAsync());
        });
        var trace = response.Headers.GetValues("X-Trace-Id").Single();
        var failure = Assert.Single(factory.Logs.Events, x => x.Properties.TryGetValue("TraceId", out var value) && value.ToString().Trim('"') == trace && x.Exception is not null);
        Assert.Contains("Controlled alteration audit failure", failure.Exception!.ToString());
    }

    [Theory]
    [InlineData("reason", "null")]
    [InlineData("reason", "\" \"")]
    [InlineData("expectedVersion", "0")]
    [InlineData("expectedVersion", "-1")]
    [InlineData("status", "0")]
    [InlineData("status", "\"Unknown\"")]
    [InlineData("volumeSource", "0")]
    [InlineData("volumeLiters", "-1")]
    [InlineData("volumeLiters", "100.0001")]
    [InlineData("volumeMethod", "null")]
    [InlineData("startedAt", "\"2026-10-08T08:00:00\"")]
    [InlineData("endedAt", "\"2026-10-08T07:00:00-03:00\"")]
    [InlineData("bedIds", "null")]
    public async Task Invalid_correction_does_not_change_fact_or_audit(string property, string value)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        var body = JsonNode.Parse(JsonSerializer.Serialize(Correction(original), Json))!;
        body[property] = JsonNode.Parse(value);
        var response = await client.PutAsync(Path(created.Id), new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await factory.WithDbAsync(async db => { Assert.Equal(1, (await db.Irrigations.SingleAsync()).Version); Assert.Equal(1, await db.AuditEntries.CountAsync()); });
    }

    [Theory]
    [InlineData("expectedVersion")]
    [InlineData("reason")]
    [InlineData("bedIds")]
    [InlineData("startedAt")]
    [InlineData("volumeLiters")]
    [InlineData("notes")]
    public async Task Correction_requires_complete_form_even_when_information_can_be_null(string property)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        var body = JsonNode.Parse(JsonSerializer.Serialize(Correction(original), Json))!.AsObject();
        body.Remove(property);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync(Path(created.Id), new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"))).StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(1, await db.AuditEntries.CountAsync()));
    }

    [Fact]
    public async Task Invalidated_fact_rejects_correction_and_repeated_invalidation_by_state_or_version()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        await DetailAsync(await InvalidateAsync(client, created.Id, 1));
        await AssertProblemAsync(await InvalidateAsync(client, created.Id, 1), HttpStatusCode.Conflict, "irrigation.version_conflict");
        await AssertProblemAsync(await InvalidateAsync(client, created.Id, 2), HttpStatusCode.Conflict, "irrigation.record_invalid");
        await AssertProblemAsync(await PutAsync(client, created.Id, Correction(original, version: 2)), HttpStatusCode.Conflict, "irrigation.record_invalid");
        await factory.WithDbAsync(async db => Assert.Equal(2, await db.AuditEntries.CountAsync()));
    }

    [Fact]
    public async Task Other_gardens_missing_ids_and_invalid_links_cannot_be_changed_or_expose_history()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        var other = Garden.Create("OTHER", "Outra horta").Value;
        var sector = IrrigationSector.Create(other.Id, "S1", "Setor externo").Value;
        await factory.WithDbAsync(async db => { db.AddRange(other, sector); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await PutAsync(client, created.Id, Correction(original) with { SectorId = sector.Id })).StatusCode);
        foreach (var path in new[] { $"/api/v1/gardens/{other.Id}/irrigations/{created.Id}", Path(Guid.CreateVersion7()) })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, Correction(original), Json)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(path + "/invalidation", new { expectedVersion = 1, reason = "Teste" }, Json)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path + "/history")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await PutAsync(client, created.Id, Correction(original) with { SectorId = DemoIds.SectorTwo })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PutAsync(client, created.Id, Correction(original) with { BedIds = [DemoIds.Beds[0], DemoIds.Beds[0]] })).StatusCode);
        await factory.WithDbAsync(async db => { Assert.Equal(1, (await db.Irrigations.SingleAsync()).Version); Assert.Equal(1, await db.AuditEntries.CountAsync()); });
    }

    internal static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(response.Headers.GetValues("X-Trace-Id").Single(), json.RootElement.GetProperty("traceId").GetString());
    }

    private sealed class AdjustableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = FixedTimeProvider.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
