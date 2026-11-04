using System.Net;
using System.Net.Http.Json;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;
using HortaComunitaria.Api.Features.Irrigation.ListIrrigations;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationReadTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Local_midnight_boundaries_crossing_day_and_pagination_follow_start_time()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var midnight = new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.FromHours(-3));
        var baseline = Request(10m) with { BedIds = [], EndedAt = null, ActiveDurationMinutes = null };
        await CreateAsync(client, baseline with { OperationId = Guid.CreateVersion7(), StartedAt = midnight.AddTicks(-10) });
        var first = await CreateAsync(client, baseline with { OperationId = Guid.CreateVersion7(), StartedAt = midnight });
        var crossing = await CreateAsync(client, baseline with
        { OperationId = Guid.CreateVersion7(), StartedAt = midnight.AddDays(1).AddSeconds(-1), EndedAt = midnight.AddDays(1).AddHours(1) });
        var tied = await CreateAsync(client, baseline with
        { OperationId = Guid.CreateVersion7(), StartedAt = crossing.StartedAt });
        await CreateAsync(client, baseline with { OperationId = Guid.CreateVersion7(), StartedAt = midnight.AddDays(1) });
        var pageResponse = await client.GetAsync(ListPath + "&pageSize=2");
        Assert.True(pageResponse.IsSuccessStatusCode, string.Join("\n", factory.Logs.Events.Select(x => x.Exception?.ToString())));
        var page1 = (await pageResponse.Content.ReadFromJsonAsync<Page<IrrigationSummaryResponse>>(Json))!;
        var page2 = (await client.GetFromJsonAsync<Page<IrrigationSummaryResponse>>(ListPath + "&pageSize=2&page=2", Json))!;
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(new[] { crossing.Id, tied.Id }.OrderDescending(), page1.Items.Select(x => x.Id));
        Assert.Equal(first.Id, Assert.Single(page2.Items).Id);
        var consumption = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(30m, consumption.Summary.KnownVolumeLiters);
        Assert.Equal(3, consumption.Summary.EventCount);
    }

    [Fact]
    public async Task Historical_assignments_survive_a_subsequent_sector_change()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var old = await CreateAsync(client, Request());
        var newAssignmentIds = new List<Guid>();
        await factory.WithDbAsync(async db =>
        {
            var cutoff = new DateOnly(2026, 10, 9);
            await db.Database.ExecuteSqlAsync($"UPDATE bed_sector_assignments SET \"ValidUntil\" = {cutoff}");
            var sector = await db.IrrigationSectors.SingleAsync(x => x.Id == DemoIds.SectorTwo);
            var beds = await db.Beds.ToListAsync();
            var existing = await db.BedSectorAssignments.AsNoTracking().ToListAsync();
            foreach (var bed in beds)
            {
                var assignment = BedSectorAssignment.Create(bed, sector, cutoff, null, existing).Value;
                db.BedSectorAssignments.Add(assignment);
                newAssignmentIds.Add(assignment.Id);
            }
            await db.SaveChangesAsync();
        });
        var nextRequest = Request(20m);
        var next = await CreateAsync(client, nextRequest with
        { SectorId = DemoIds.SectorTwo, StartedAt = nextRequest.StartedAt.AddDays(1), EndedAt = nextRequest.EndedAt?.AddDays(1) });
        Assert.Equal(newAssignmentIds.Order(), next.Beds.Select(x => x.AssignmentId).Order());
        var oldDetail = (await client.GetFromJsonAsync<IrrigationDetailResponse>(BasePath + "/irrigations/" + old.Id, Json))!;
        Assert.Equal(DemoIds.SectorOne, oldDetail.SectorId);
        Assert.Equal(DemoIds.Assignments.Order(), oldDetail.Beds.Select(x => x.AssignmentId).Order());
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, Request() with { SectorId = DemoIds.SectorTwo })).StatusCode);
        var report = (await client.GetFromJsonAsync<WaterConsumptionResponse>(
            BasePath + "/indicators/water-consumption?from=2026-10-08&to=2026-10-09", Json))!;
        Assert.Equal(140m, report.Summary.KnownVolumeLiters);
        Assert.Equal(120m, report.BySector.Items.Single(x => x.SectorId == DemoIds.SectorOne).KnownVolumeLiters);
        Assert.Equal(20m, report.BySector.Items.Single(x => x.SectorId == DemoIds.SectorTwo).KnownVolumeLiters);
    }

    [Fact]
    public async Task Invalid_records_remain_accessible_but_default_list_excludes_them()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var invalid = await CreateAsync(client, Request());
        await factory.WithDbAsync(db => db.Database.ExecuteSqlAsync(
            $"UPDATE irrigations SET \"Validity\" = 'invalid'::record_validity WHERE \"Id\" = {invalid.Id}"));
        var list = (await client.GetFromJsonAsync<Page<IrrigationSummaryResponse>>(ListPath, Json))!;
        Assert.Empty(list.Items);
        var invalidList = (await client.GetFromJsonAsync<Page<IrrigationSummaryResponse>>(ListPath + "&validity=Invalid", Json))!;
        Assert.Equal(invalid.Id, Assert.Single(invalidList.Items).Id);
        var detail = (await client.GetFromJsonAsync<IrrigationDetailResponse>(BasePath + "/irrigations/" + invalid.Id, Json))!;
        Assert.Equal(RecordValidity.Invalid, detail.Validity);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(BasePath + "/irrigations/" + Guid.CreateVersion7())).StatusCode);
        var report = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(new WaterCoverage(null, 0, 0, 0, ConsumptionAvailability.NoData, false), report.Summary);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("Unknown")]
    [InlineData("valid")]
    public async Task Validity_filter_requires_documented_enum_name(string value)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(ListPath + "&validity=" + value)).StatusCode);
    }

    [Fact]
    public async Task Read_filters_and_fact_foreign_keys_enforce_garden_context()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var other = Garden.Create("OTHER", "Outra horta").Value;
        var otherSector = IrrigationSector.Create(other.Id, "S1", "Outro setor").Value;
        await factory.WithDbAsync(async db => { db.AddRange(other, otherSector); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(ConsumptionPath + "&sectorId=" + otherSector.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(ListPath + "&sectorId=" + otherSector.Id)).StatusCode);
        var created = await CreateAsync(client, Request());
        await factory.WithDbAsync(async db =>
        {
            var crossGarden = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
                $"UPDATE irrigations SET \"IrrigationSectorId\" = {otherSector.Id} WHERE \"Id\" = {created.Id}"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, crossGarden.SqlState);
            var crossAssignment = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
                $"UPDATE irrigation_beds SET \"BedSectorAssignmentId\" = {DemoIds.Assignments[1]} WHERE \"BedId\" = {DemoIds.Beds[0]}"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, crossAssignment.SqlState);
            var delete = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
                $"DELETE FROM irrigations WHERE \"Id\" = {created.Id}"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, delete.SqlState);
        });
    }
}
