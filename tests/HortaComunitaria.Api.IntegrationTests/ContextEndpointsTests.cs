using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Features.Gardens.ListGardens;
using HortaComunitaria.Api.Features.Gardens.ListIrrigationSectors;
using HortaComunitaria.Api.Features.Gardens.ListBedsForIrrigation;
using HortaComunitaria.Api.Features.Activities.ListParticipants;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class ContextEndpointsTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static string GardenPath => $"/api/v1/gardens/{DemoIds.Garden}";

    [Fact]
    public async Task Lists_explicit_test_context_and_keeps_authors_out_of_participants()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var page = await client.GetFromJsonAsync<Page<ListGardensResponse>>("/api/v1/gardens");
        Assert.NotNull(page);
        Assert.Equal(DemoIds.Garden, Assert.Single(page.Items).Id);
        await factory.WithDbAsync(async db =>
        {
            var ids = await db.BedSectorAssignments.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
            Assert.Equal(1, await db.Gardens.CountAsync());
            Assert.Equal(2, await db.IrrigationSectors.CountAsync());
            Assert.Equal(4, await db.Beds.CountAsync());
            Assert.Equal(ids, await db.BedSectorAssignments.OrderBy(x => x.Id).Select(x => x.Id).ToListAsync());
            Assert.Equal(TestAccounts.All.Select(x => x.Id).Order(), (await db.Users.Select(x => x.Id).ToListAsync()).Order());
        });
        var participants = await client.GetFromJsonAsync<Page<ListParticipantsResponse>>(GardenPath + "/participants");
        Assert.Equal(DemoIds.Participant, Assert.Single(participants!.Items).Id);
        var raw = await client.GetStringAsync(GardenPath + "/participants");
        Assert.DoesNotContain("email", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(DemoIds.Operator.ToString(), raw);
        Assert.DoesNotContain(DemoIds.Administrator.ToString(), raw);
    }

    [Fact]
    public async Task Startup_does_not_populate_an_empty_database()
    {
        using var factory = await postgres.CreateFactoryAsync(withContext: false);
        using var client = factory.CreateClient();
        var page = await client.GetFromJsonAsync<Page<ListGardensResponse>>("/api/v1/gardens");
        Assert.Empty(page!.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNextPage);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(0, await db.Gardens.CountAsync());
            Assert.Equal(0, await db.IrrigationSectors.CountAsync());
            Assert.Equal(0, await db.Beds.CountAsync());
            Assert.Equal(0, await db.BedSectorAssignments.CountAsync());
            Assert.Equal(0, await db.Participants.CountAsync());
            // Só as contas criadas pela fixture de autenticação; o startup não cria usuários nem dados.
            Assert.Equal(TestAccounts.All.Count, await db.Users.CountAsync());
        });
    }

    [Fact]
    public async Task Active_gardens_are_sorted_and_paginated_without_duplicates()
    {
        using var factory = await postgres.CreateFactoryAsync(withContext: false);
        using var client = factory.CreateClient();
        var first = Garden.Create("A", "Primeira").Value;
        var last = Garden.Create("Z", "Última").Value;
        await factory.WithDbAsync(async db =>
        {
            db.Gardens.AddRange(last, Garden.Create("B", "Inativa", false).Value, first);
            await db.SaveChangesAsync();
        });
        var one = await client.GetFromJsonAsync<Page<ListGardensResponse>>("/api/v1/gardens?pageSize=1");
        var two = await client.GetFromJsonAsync<Page<ListGardensResponse>>("/api/v1/gardens?pageSize=1&page=2");
        Assert.Equal(first.Id, Assert.Single(one!.Items).Id);
        Assert.Equal(last.Id, Assert.Single(two!.Items).Id);
        Assert.Equal(2, one.TotalCount);
        Assert.Equal(2, one.TotalPages);
        Assert.True(one.HasNextPage);
        Assert.True(two.HasPreviousPage);
        Assert.False(two.HasNextPage);
        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/v1/gardens"));
        Assert.Equal(1, json.RootElement.GetProperty("page").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("pageNumber", out _));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?pageSize=0")]
    [InlineData("?page=2147483647&pageSize=100")]
    [InlineData("?page=abc")]
    public async Task Invalid_pagination_returns_structured_problem(string query)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/gardens" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation.invalid_request", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(response.Headers.GetValues("X-Trace-Id").Single(), problem.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Eligibility_respects_start_date_sector_and_page_boundaries()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var path = GardenPath + $"/beds?sectorId={DemoIds.SectorOne}";
        var before = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(path + "&on=2026-09-30");
        Assert.Empty(before!.Items);
        var eligible = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(path + "&on=2026-10-08");
        Assert.Equal(4, eligible!.TotalCount);
        Assert.Equal(new[] { "B1", "B2", "B3", "B4" }, eligible.Items.Select(x => x.Code));
        Assert.Equal(DemoIds.Assignments.Order(), eligible.Items.Select(x => x.AssignmentId).Order());
        var pageOne = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(path + "&on=2026-10-08&pageSize=2");
        var pageTwo = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(path + "&on=2026-10-08&pageSize=2&page=2");
        Assert.Empty(pageOne!.Items.Select(x => x.Id).Intersect(pageTwo!.Items.Select(x => x.Id)));
        Assert.Equal(4, pageTwo.TotalCount);
        var other = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(
            GardenPath + $"/beds?sectorId={DemoIds.SectorTwo}&on=2026-10-08");
        Assert.Empty(other!.Items);
    }

    [Fact]
    public async Task Historical_transition_uses_exclusive_end_and_inactive_beds_are_omitted()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            var s1 = await db.IrrigationSectors.SingleAsync(x => x.Id == DemoIds.SectorOne);
            var s2 = await db.IrrigationSectors.SingleAsync(x => x.Id == DemoIds.SectorTwo);
            var bed = Bed.Create(DemoIds.Garden, "B5", "Transição").Value;
            var inactive = Bed.Create(DemoIds.Garden, "B6", "Inativo", false).Value;
            var old = BedSectorAssignment.Create(bed, s1, new(2026, 10, 1), new(2026, 10, 9), []).Value;
            var next = BedSectorAssignment.Create(bed, s2, new(2026, 10, 9), null, [old]).Value;
            db.Beds.AddRange(bed, inactive);
            db.BedSectorAssignments.AddRange(old, next,
                BedSectorAssignment.Create(inactive, s1, new(2026, 10, 1), null, []).Value);
            await db.SaveChangesAsync();
        });
        var oldDay = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(
            GardenPath + $"/beds?sectorId={DemoIds.SectorOne}&on=2026-10-08");
        Assert.Contains(oldDay!.Items, x => x.Code == "B5");
        Assert.DoesNotContain(oldDay.Items, x => x.Code == "B6");
        var transitionOld = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(
            GardenPath + $"/beds?sectorId={DemoIds.SectorOne}&on=2026-10-09");
        Assert.DoesNotContain(transitionOld!.Items, x => x.Code == "B5");
        var transitionNew = await client.GetFromJsonAsync<Page<ListBedsForIrrigationResponse>>(
            GardenPath + $"/beds?sectorId={DemoIds.SectorTwo}&on=2026-10-09");
        Assert.Equal("B5", Assert.Single(transitionNew!.Items).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("&on=invalid")]
    [InlineData("&on=08/10/2026")]
    [InlineData("&on=2026-02-30")]
    public async Task Missing_or_invalid_date_is_rejected(string date)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var response = await client.GetAsync(GardenPath + $"/beds?sectorId={DemoIds.SectorOne}" + date);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("on", out _));
    }

    [Fact]
    public async Task Other_garden_is_filtered_and_foreign_sector_returns_not_found()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var other = Garden.Create("OTHER", "Outra horta").Value;
        var sector = IrrigationSector.Create(other.Id, "S1", "Outro setor").Value;
        await factory.WithDbAsync(async db =>
        {
            db.Gardens.Add(other);
            db.IrrigationSectors.AddRange(sector, IrrigationSector.Create(DemoIds.Garden, "S3", "Inativo", false).Value);
            db.Participants.AddRange(Participant.Create(other.Id, "Outro participante").Value,
                Participant.Create(DemoIds.Garden, "Inativo", false).Value);
            await db.SaveChangesAsync();
        });
        var sectors = await client.GetFromJsonAsync<Page<ListIrrigationSectorsResponse>>(GardenPath + "/sectors");
        Assert.Equal(new[] { DemoIds.SectorOne, DemoIds.SectorTwo }.Order(), sectors!.Items.Select(x => x.Id).Order());
        var participants = await client.GetFromJsonAsync<Page<ListParticipantsResponse>>(GardenPath + "/participants");
        Assert.Equal(DemoIds.Participant, Assert.Single(participants!.Items).Id);
        var response = await client.GetAsync(GardenPath + $"/beds?sectorId={sector.Id}&on=2026-10-08");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("resource.not_found", json.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("sectors")]
    [InlineData("participants")]
    public async Task Missing_garden_returns_not_found_and_existing_empty_context_is_success(string resource)
    {
        using var factory = await postgres.CreateFactoryAsync(withContext: false);
        using var client = factory.CreateClient();
        var garden = Garden.Create("EMPTY", "Vazia").Value;
        await factory.WithDbAsync(async db => { db.Gardens.Add(garden); await db.SaveChangesAsync(); });
        var missing = await client.GetAsync($"/api/v1/gardens/{Guid.CreateVersion7()}/{resource}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var existing = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/gardens/{garden.Id}/{resource}"));
        Assert.Equal(0, existing.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, existing.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task OpenApi_describes_actual_contract_and_swagger_is_available()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.True(response.IsSuccessStatusCode, string.Join("\n", factory.Logs.Events.Where(x => x.Exception is not null).Select(x => x.Exception!.ToString())));
        using var openapi = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = openapi.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/gardens", out var gardens));
        Assert.True(gardens.GetProperty("get").TryGetProperty("summary", out _));
        var beds = paths.GetProperty("/api/v1/gardens/{gardenId}/beds").GetProperty("get");
        var on = Assert.Single(beds.GetProperty("parameters").EnumerateArray(), x => x.GetProperty("name").GetString() == "on");
        Assert.True(on.GetProperty("required").GetBoolean());
        Assert.Equal("date", on.GetProperty("schema").GetProperty("format").GetString());
        Assert.Contains("assignmentId", openapi.RootElement.GetRawText());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }
}
