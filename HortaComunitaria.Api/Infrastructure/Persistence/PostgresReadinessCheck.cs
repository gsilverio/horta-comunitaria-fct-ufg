using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

internal sealed class PostgresReadinessCheck(IOptions<PersistenceOptions> options,
    ILogger<PostgresReadinessCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(options.Value.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            logger.LogWarning("PostgreSQL indisponível na verificação de prontidão; evento {EventName}, processo {ProcessName}, resultado {Outcome}, tipo {FailureType}",
                "DependencyUnavailable", "Health.DatabaseReadiness", "Unavailable", exception.GetType().Name);
            return HealthCheckResult.Unhealthy();
        }
    }
}
