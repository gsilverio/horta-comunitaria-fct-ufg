using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationConcurrencyTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_operation_creates_one_fact_and_audit(bool differentPayload)
    {
        var barrier = new ConcurrentSaveBarrier();
        using var factory = await postgres.CreateFactoryAsync(customize: services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(barrier)));
        using var client = factory.CreateClient();
        var request = Request();
        var responses = await Task.WhenAll(PostAsync(client, request),
            PostAsync(client, differentPayload ? request with { VolumeLiters = 130m } : request));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, x => x.StatusCode == (differentPayload ? HttpStatusCode.Conflict : HttpStatusCode.OK));
        if (!differentPayload)
        {
            var details = await Task.WhenAll(responses.Select(x => x.Content.ReadFromJsonAsync<IrrigationDetailResponse>(Json)));
            Assert.Equal(details[0]!.Id, details[1]!.Id);
        }
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Irrigations.CountAsync());
            Assert.Equal(4, await db.IrrigationBeds.CountAsync());
            Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.Operation == AuditOperation.Created));
        });
        Assert.DoesNotContain(factory.Logs.Events, x => x.Exception is not null);
    }

    [Fact]
    public async Task Consumption_summary_and_sector_page_share_a_snapshot_during_concurrent_creation()
    {
        var gate = new ConsumptionReadGate();
        using var factory = await postgres.CreateFactoryAsync(customize: services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(gate)));
        using var client = factory.CreateClient();
        await CreateAsync(client, Request());
        var pending = client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json);
        await gate.SummaryReached.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try { await CreateAsync(client, Request(20m) with { SectorId = DemoIds.SectorTwo, BedIds = [] }); }
        finally { gate.Release.TrySetResult(); }
        var snapshot = (await pending)!;
        Assert.Equal(120m, snapshot.Summary.KnownVolumeLiters);
        Assert.Equal(1, snapshot.Summary.EventCount);
        Assert.Equal(ConsumptionAvailability.NoData, snapshot.BySector.Items.Single(x => x.SectorId == DemoIds.SectorTwo).Availability);
        var next = (await client.GetFromJsonAsync<WaterConsumptionResponse>(ConsumptionPath, Json))!;
        Assert.Equal(140m, next.Summary.KnownVolumeLiters);
        Assert.Equal(2, next.Summary.EventCount);
        Assert.Equal(20m, next.BySector.Items.Single(x => x.SectorId == DemoIds.SectorTwo).KnownVolumeLiters);
    }

    private sealed class ConcurrentSaveBarrier : SaveChangesInterceptor
    {
        private int arrived;
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<HortaComunitaria.Api.Domain.Irrigation.Irrigation>()
                .Any(x => x.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref arrived) == 2) release.TrySetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    private sealed class ConsumptionReadGate : DbCommandInterceptor
    {
        private int intercepted;
        public TaskCompletionSource SummaryReached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("WaterConsumption.Summary", StringComparison.Ordinal) &&
                Interlocked.Increment(ref intercepted) == 1)
            {
                SummaryReached.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }
}
