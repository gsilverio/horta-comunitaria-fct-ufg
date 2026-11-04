using System.Net;
using System.Net.Http.Json;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class WaterConsumptionTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task No_events_is_no_data_and_unknown_volume_is_unavailable()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var empty = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(new WaterCoverage(null, 0, 0, 0, ConsumptionAvailability.NoData, false), empty.Summary);
        Assert.Equal(2, empty.BySector.TotalCount);
        Assert.All(empty.BySector.Items, x => Assert.Equal(ConsumptionAvailability.NoData, x.Availability));
        await CreateAsync(client, Request(null));
        var unknown = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(new WaterCoverage(null, 1, 0, 1, ConsumptionAvailability.Unavailable, false), unknown.Summary);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(120)]
    public async Task Known_volume_is_counted_once_for_four_beds_and_pagination_preserves_summary(int liters)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await CreateAsync(client, Request(liters));
        var first = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath + "&pageSize=1", Json))!;
        var second = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath + "&pageSize=1&page=2", Json))!;
        Assert.Equal(new WaterCoverage(liters, 1, 1, 0, ConsumptionAvailability.Available, false), first.Summary);
        Assert.Equal(first.Summary, second.Summary);
        Assert.Equal("America/Sao_Paulo", first.TimeZone);
        Assert.Equal("L", first.Unit);
        Assert.Equal(new DateOnly(2026, 10, 8), first.From);
        Assert.Equal(2, first.BySector.TotalCount);
        Assert.True(first.BySector.HasNextPage);
        Assert.Equal(liters, Assert.Single(first.BySector.Items).KnownVolumeLiters);
        Assert.Equal(ConsumptionAvailability.NoData, Assert.Single(second.BySector.Items).Availability);
        Assert.Null(Assert.Single(second.BySector.Items).KnownVolumeLiters);
    }

    [Fact]
    public async Task Partial_coverage_includes_interrupted_estimates_and_excludes_invalid_facts()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await CreateAsync(client, Request());
        await CreateAsync(client, Request(null));
        await CreateAsync(client, Request(20m) with
        { SectorId = DemoIds.SectorTwo, BedIds = [], Status = IrrigationStatus.Interrupted, VolumeSource = VolumeSource.Estimated });
        var excluded = await CreateAsync(client, Request(999m) with { VolumeSource = VolumeSource.Estimated });
        // Fixture de leitura: não implementa um endpoint de invalidação da etapa 3.
        await factory.WithDbAsync(db => db.Database.ExecuteSqlAsync(
            $"UPDATE irrigations SET \"Validity\" = 'invalid'::record_validity WHERE \"Id\" = {excluded.Id}"));
        var result = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(new WaterCoverage(140m, 3, 2, 1, ConsumptionAvailability.Partial, true), result.Summary);
        var s1 = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath + "&sectorId=" + DemoIds.SectorOne, Json))!;
        Assert.Equal(new WaterCoverage(120m, 2, 1, 1, ConsumptionAvailability.Partial, false), s1.Summary);
        Assert.Single(s1.BySector.Items);
        var s2 = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath + "&sectorId=" + DemoIds.SectorTwo, Json))!;
        Assert.Equal(new WaterCoverage(20m, 1, 1, 0, ConsumptionAvailability.Available, true), s2.Summary);
        await factory.WithDbAsync(db => db.Database.ExecuteSqlAsync(
            $"UPDATE irrigation_sectors SET \"IsActive\" = false WHERE \"Id\" = {DemoIds.SectorTwo}"));
        var historical = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Contains(historical.BySector.Items, x => x.SectorId == DemoIds.SectorTwo && x.KnownVolumeLiters == 20m);
    }

    [Theory]
    [InlineData("?to=2026-10-08")]
    [InlineData("?from=2026-10-08")]
    [InlineData("?from=2026-10-09&to=2026-10-08")]
    [InlineData("?from=08-10-2026&to=2026-10-08")]
    [InlineData("?from=2026-10-08&to=2026-10-08&page=0")]
    [InlineData("?from=2026-10-08&to=2026-10-08&pageSize=101")]
    public async Task Invalid_filters_are_rejected(string query)
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(BasePath + "/indicators/water-consumption" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(BasePath + "/irrigations" + query)).StatusCode);
    }
}
