using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Observability;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

internal sealed class DatabaseInitializationService(IServiceScopeFactory scopes,
    IOptions<PersistenceOptions> persistence,
    ILogger<DatabaseInitializationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!persistence.Value.ApplyMigrationsOnStartup) return;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        using var process = new ProcessLog(logger, "Persistence.ApplyMigrations");
        process.Step("ApplyingMigrations", "Conferindo e aplicando migrations pendentes do banco");
        await db.Database.MigrateAsync(cancellationToken);
        process.Event("MigrationsCompleted", "Succeeded", "Esquema do banco atualizado; nenhum cadastro é inserido automaticamente");
        process.Complete(Result<Unit>.Success(new Unit()));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
