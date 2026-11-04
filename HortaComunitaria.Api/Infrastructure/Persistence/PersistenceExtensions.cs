using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

internal static class PersistenceExtensions
{
    internal static IServiceCollection AddPersistence(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<PersistenceOptions>().Bind(configuration.GetSection("Persistence"))
            .Validate(x => IsValidConnectionString(x.ConnectionString), "Configure Persistence:ConnectionString localmente.")
            .Validate(x => !x.ApplyMigrationsOnStartup || environment.IsDevelopment(),
                "Migrations automáticas são exclusivas de Development.").ValidateOnStart();
        services.AddOptions<DemoModeOptions>().Bind(configuration.GetSection("DemoMode"))
            .Validate(x => x.Enabled && environment.IsDevelopment(),
                "A API requer Development e DemoMode:Enabled=true enquanto não houver autenticação.").ValidateOnStart();
        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<PersistenceOptions>>().Value.ConnectionString, PostgresEnums.Configure));
        services.AddScoped<HortaComunitaria.Api.Infrastructure.Identity.DemoAuthorContext>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<DatabaseInitializationService>();
        return services;
    }

    private static bool IsValidConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(value);
            return !string.IsNullOrWhiteSpace(connection.Host) && !string.IsNullOrWhiteSpace(connection.Database);
        }
        catch (ArgumentException) { return false; }
    }
}
