using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;
using static HortaComunitaria.Api.IntegrationTests.IrrigationChangesTestData;
using static HortaComunitaria.Api.IntegrationTests.IrrigationChangesTests;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class IrrigationChangeConcurrencyTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_corrections_or_correction_invalidation_confirm_only_one_intention(bool mixed)
    {
        var barrier = new ChangeSaveBarrier();
        using var factory = await postgres.CreateFactoryAsync(customize: services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(barrier)));
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        var responses = await Task.WhenAll(PutAsync(client, created.Id, Correction(original) with { BedIds = DemoIds.Beds.Take(2).ToArray() }),
            mixed ? InvalidateAsync(client, created.Id, 1) : PutAsync(client, created.Id, Correction(original, 80m) with { BedIds = [] }));
        var winner = await DetailAsync(Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK));
        await AssertProblemAsync(Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict, "irrigation.version_conflict");
        Assert.Equal(2, winner.Version);
        await factory.WithDbAsync(async db =>
        {
            var persisted = await db.Irrigations.Include(x => x.Beds).SingleAsync();
            Assert.Equal(winner.Version, persisted.Version);
            Assert.Equal(winner.Validity, persisted.Validity);
            Assert.Equal(winner.VolumeLiters, persisted.VolumeLiters);
            Assert.Equal(winner.BedIds.Order(), persisted.Beds.Select(x => x.BedId).Order());
            Assert.Equal(2, await db.AuditEntries.CountAsync());
            var change = await db.AuditEntries.SingleAsync(x => x.Operation != AuditOperation.Created);
            Assert.Equal(winner.Validity == RecordValidity.Invalid ? AuditOperation.Invalidated : AuditOperation.Corrected, change.Operation);
            Assert.Equal(winner.Version, IrrigationSnapshots.Read(change.After).Version);
            Assert.Equal(winner.BedIds.Order(), IrrigationSnapshots.Read(change.After).Beds.Select(x => x.BedId).Order());
        });
        Assert.DoesNotContain(factory.Logs.Events, x => x.Exception is not null);
    }

    [Fact]
    public async Task No_change_detects_a_concurrent_version_change_without_an_artificial_write()
    {
        var gate = new CommandGate("CorrectIrrigation.NoChange", afterExecution: false);
        using var factory = await postgres.CreateFactoryAsync(customize: services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(gate)));
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        gate.Armed = true;
        var pending = PutAsync(client, created.Id, Correction(original, 120m));
        await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try { await DetailAsync(await PutAsync(client, created.Id, Correction(original))); }
        finally { gate.Release.TrySetResult(); }
        await AssertProblemAsync(await pending, HttpStatusCode.Conflict, "irrigation.version_conflict");
        await factory.WithDbAsync(async db => { Assert.Equal(2, (await db.Irrigations.SingleAsync()).Version); Assert.Equal(2, await db.AuditEntries.CountAsync()); });
    }

    [Fact]
    public async Task Detail_keeps_fact_and_children_from_same_snapshot_during_correction()
    {
        var gate = new CommandGate("Irrigation.Detail", afterExecution: true);
        using var factory = await postgres.CreateFactoryAsync(customize: services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(gate)));
        using var client = factory.CreateClient();
        var original = Request();
        var created = await CreateAsync(client, original);
        gate.Armed = true;
        var pending = client.GetFromJsonAsync<IrrigationDetailResponse>(Path(created.Id), Json);
        await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try { await DetailAsync(await PutAsync(client, created.Id, Correction(original) with { BedIds = [DemoIds.Beds[0]] })); }
        finally { gate.Release.TrySetResult(); }
        var snapshot = (await pending)!;
        Assert.Equal(120m, snapshot.VolumeLiters);
        Assert.Equal(1, snapshot.Version);
        Assert.Equal(4, snapshot.Beds.Count);
        var next = (await client.GetFromJsonAsync<IrrigationDetailResponse>(Path(created.Id), Json))!;
        Assert.Equal(100m, next.VolumeLiters);
        Assert.Equal(2, next.Version);
        Assert.Single(next.Beds);
    }

    private sealed class ChangeSaveBarrier : SaveChangesInterceptor
    {
        private int arrived;
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<HortaComunitaria.Api.Domain.Irrigation.Irrigation>().Any(x => x.State == EntityState.Modified))
            {
                if (Interlocked.Increment(ref arrived) == 2) release.TrySetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }

    private sealed class CommandGate(string tag, bool afterExecution) : DbCommandInterceptor
    {
        private int intercepted;
        public bool Armed { get; set; }
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private async Task PauseAsync(DbCommand command, CancellationToken cancellationToken)
        {
            if (Armed && command.CommandText.Contains(tag, StringComparison.Ordinal) && Interlocked.Increment(ref intercepted) == 1)
            {
                Reached.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
        }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (!afterExecution) await PauseAsync(command, cancellationToken);
            return result;
        }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (afterExecution) await PauseAsync(command, cancellationToken);
            return result;
        }
    }
}
