namespace HortaComunitaria.Api.Infrastructure.Persistence;

internal sealed class PersistenceOptions
{
    public string ConnectionString { get; set; } = "";
    public bool ApplyMigrationsOnStartup { get; set; }
}
