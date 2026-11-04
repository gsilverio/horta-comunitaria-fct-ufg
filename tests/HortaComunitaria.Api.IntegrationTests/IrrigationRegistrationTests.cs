using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationRegistrationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Creation_persists_event_historical_links_and_inclusion_atomically()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var request = Request();
        var body = JsonNode.Parse(JsonSerializer.Serialize(request, Json))!;
        body["recordedByUserId"] = Guid.CreateVersion7().ToString();
        var response = await client.PostAsync(BasePath + "/irrigations",
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = (await response.Content.ReadFromJsonAsync<IrrigationDetailResponse>(Json))!;
        Assert.Equal(120m, detail.VolumeLiters);
        Assert.Equal(4, detail.Beds.Count);
        Assert.Equal(DemoIds.Beds.Order(), detail.BedIds.Order());
        Assert.Equal(DemoIds.Assignments.Order(), detail.Beds.Select(x => x.AssignmentId).Order());
        Assert.Equal(FixedTimeProvider.Now, detail.RecordedAt);
        Assert.Equal(TimeSpan.Zero, detail.StartedAt.Offset);
        Assert.Equal(1, detail.Version);
        Assert.Equal(RecordValidity.Valid, detail.Validity);
        Assert.Equal(DemoIds.Operator, detail.RecordedBy.Id);
        Assert.True(detail.RecordedBy.IsDemonstration);
        Assert.Equal(DemoIds.Participant, detail.ResponsibleParticipantId);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("creationPayload", raw);
        Assert.DoesNotContain("operationId", raw);
        var queried = await client.GetFromJsonAsync<IrrigationDetailResponse>(response.Headers.Location, Json);
        Assert.Equal(detail.Id, queried!.Id);
        await factory.WithDbAsync(async db =>
        {
            var persisted = await db.Irrigations.SingleAsync();
            Assert.Equal(request.OperationId, persisted.OperationId);
            Assert.Equal(120m, persisted.VolumeLiters);
            Assert.Equal(4, await db.IrrigationBeds.CountAsync());
            var audit = await db.AuditEntries.SingleAsync();
            Assert.Equal(AuditOperation.Created, audit.Operation);
            Assert.Equal(persisted.Id, audit.EntityKey);
            Assert.Equal(persisted.RecordedAt, audit.OccurredAt);
            Assert.Null(audit.Before);
            using var snapshot = JsonDocument.Parse(audit.After);
            Assert.Equal(120m, snapshot.RootElement.GetProperty("volumeLiters").GetDecimal());
            Assert.Equal(4, snapshot.RootElement.GetProperty("beds").GetArrayLength());
        });
    }

    [Fact]
    public async Task Normalized_replay_returns_same_identity_without_revalidating_active_catalogs()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var request = Request() with { Notes = "Observação" };
        var first = await CreateAsync(client, request);
        await factory.WithDbAsync(db => db.Database.ExecuteSqlAsync(
            $"UPDATE irrigation_sectors SET \"IsActive\" = false WHERE \"Id\" = {DemoIds.SectorOne}"));
        var response = await PostAsync(client, request with
        {
            StartedAt = request.StartedAt.ToUniversalTime(),
            EndedAt = request.EndedAt?.ToUniversalTime(),
            BedIds = request.BedIds!.Reverse().ToArray(),
            VolumeLiters = 120.000m,
            VolumeMethod = " Recipiente graduado ",
            Notes = " Observação "
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(first.Id, (await response.Content.ReadFromJsonAsync<IrrigationDetailResponse>(Json))!.Id);
        Assert.EndsWith(first.Id.ToString(), response.Headers.Location!.ToString());
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Irrigations.CountAsync());
            Assert.Equal(1, await db.AuditEntries.CountAsync());
        });
        var conflict = await PostAsync(client, request with { VolumeLiters = 130m });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var problem = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        Assert.Equal("irrigation.operation_conflict", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task New_operation_allows_another_event_at_same_time_and_unknown_information_is_preserved()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await CreateAsync(client, Request());
        var unknown = await CreateAsync(client, Request(null) with
        {
            BedIds = [],
            EndedAt = null,
            ActiveDurationMinutes = null,
            ResponsibleParticipantId = null,
            FlowRateLitersPerMinute = 2m,
            Notes = " "
        });
        Assert.Empty(unknown.Beds);
        Assert.Null(unknown.VolumeLiters);
        Assert.Null(unknown.VolumeMethod);
        Assert.Null(unknown.VolumeSource);
        Assert.Null(unknown.ActiveDurationMinutes);
        Assert.Null(unknown.EndedAt);
        Assert.Null(unknown.Notes);
        Assert.Equal(2m, unknown.FlowRateLitersPerMinute);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(2, await db.Irrigations.CountAsync());
            Assert.Equal(2, await db.AuditEntries.CountAsync());
        });
    }

    [Theory]
    [InlineData("volumeLiters", "-1")]
    [InlineData("activeDurationMinutes", "-1")]
    [InlineData("flowRateLitersPerMinute", "-1")]
    [InlineData("volumeLiters", "120.0001")]
    [InlineData("volumeLiters", "120.0000")]
    [InlineData("volumeLiters", "100000000000")]
    [InlineData("activeDurationMinutes", "31")]
    [InlineData("endedAt", "\"2026-10-08T07:59:59-03:00\"")]
    [InlineData("startedAt", "\"2026-10-08T08:00:00\"")]
    [InlineData("endedAt", "\"2026-10-08T08:30:00\"")]
    [InlineData("status", "0")]
    [InlineData("status", "\"0\"")]
    [InlineData("status", "\"Unknown\"")]
    [InlineData("volumeSource", "0")]
    [InlineData("volumeSource", "\"Unknown\"")]
    [InlineData("volumeSource", "null")]
    [InlineData("volumeMethod", "\"  \"")]
    [InlineData("volumeLiters", "null")]
    [InlineData("bedIds", "null")]
    [InlineData("operationId", "\"00000000-0000-0000-0000-000000000000\"")]
    public async Task Invalid_requests_leave_no_partial_records(string property, string value)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var body = JsonNode.Parse(JsonSerializer.Serialize(Request(), Json))!;
        body[property] = JsonNode.Parse(value);
        var response = await client.PostAsync(BasePath + "/irrigations",
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(response.Headers.GetValues("X-Trace-Id").Single(), problem.RootElement.GetProperty("traceId").GetString());
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(0, await db.Irrigations.CountAsync());
            Assert.Equal(0, await db.IrrigationBeds.CountAsync());
            Assert.Equal(0, await db.AuditEntries.CountAsync());
        });
    }

    [Theory]
    [InlineData("operationId")]
    [InlineData("sectorId")]
    [InlineData("bedIds")]
    [InlineData("startedAt")]
    [InlineData("status")]
    public async Task Required_json_fields_are_not_silently_defaulted(string property)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var body = JsonNode.Parse(JsonSerializer.Serialize(Request(), Json))!.AsObject();
        body.Remove(property);
        var response = await client.PostAsync(BasePath + "/irrigations",
            new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(0, await db.Irrigations.CountAsync()));
    }

    [Fact]
    public async Task Duplicates_wrong_sector_and_out_of_period_links_are_rejected()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var duplicate = await PostAsync(client, Request() with { BedIds = [DemoIds.Beds[0], DemoIds.Beds[0]] });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var wrongSector = await PostAsync(client, Request() with { SectorId = DemoIds.SectorTwo });
        Assert.Equal(HttpStatusCode.BadRequest, wrongSector.StatusCode);
        using var problem = JsonDocument.Parse(await wrongSector.Content.ReadAsStringAsync());
        Assert.Equal("irrigation.invalid_bed_assignment", problem.RootElement.GetProperty("code").GetString());
        var early = Request();
        var before = await PostAsync(client, early with { StartedAt = early.StartedAt.AddDays(-8), EndedAt = early.EndedAt?.AddDays(-8) });
        Assert.Equal(HttpStatusCode.BadRequest, before.StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(0, await db.Irrigations.CountAsync()));
    }

    [Fact]
    public async Task Foreign_garden_references_and_detail_are_scoped()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var other = Garden.Create("OTHER", "Outra horta").Value;
        var sector = IrrigationSector.Create(other.Id, "S1", "Setor").Value;
        var bed = Bed.Create(other.Id, "B1", "Canteiro").Value;
        var participant = Participant.Create(other.Id, "Pessoa fictícia").Value;
        await factory.WithDbAsync(async db =>
        {
            db.AddRange(other, sector, bed, participant);
            await db.SaveChangesAsync();
        });
        foreach (var request in new[] { Request() with { SectorId = sector.Id }, Request() with { BedIds = [bed.Id] },
            Request() with { ResponsibleParticipantId = participant.Id } })
            Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(client, request)).StatusCode);
        var created = await CreateAsync(client, Request());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/gardens/{other.Id}/irrigations/{created.Id}")).StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(1, await db.Irrigations.CountAsync()));
    }

    [Fact]
    public async Task Audit_failure_rolls_back_the_fact_and_children_in_postgres()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION test_fail_audit() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'Controlled audit failure'; END;
            $$;
            CREATE TRIGGER test_audit_failure BEFORE INSERT ON audit_entries
            FOR EACH ROW EXECUTE FUNCTION test_fail_audit();
            """));
        var response = await PostAsync(client, Request());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(0, await db.Irrigations.CountAsync());
            Assert.Equal(0, await db.IrrigationBeds.CountAsync());
            Assert.Equal(0, await db.AuditEntries.CountAsync());
        });
    }
}

