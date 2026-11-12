using Microsoft.EntityFrameworkCore;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    private string adminConnection = "";
    private readonly List<string> databases = [];

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("HORTA_TEST_POSTGRES") ?? "";
        if (string.IsNullOrWhiteSpace(adminConnection))
        {
            container = new PostgreSqlBuilder("postgres:17.0").Build();
            await container.StartAsync();
            adminConnection = container.GetConnectionString();
        }
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SHOW server_version_num", connection);
        var version = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.InRange(version, 170000, 179999);
    }

    /// <summary>
    /// Cria um banco descartável com as migrations da aplicação. <paramref name="withContext"/> insere a horta fictícia;
    /// <paramref name="withUsers"/> cria Adm e Operador de teste e autentica <c>CreateClient()</c> como Adm.
    /// </summary>
    public async Task<ApiFactory> CreateFactoryAsync(bool withContext = true, Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? customize = null,
        bool withUsers = true, IReadOnlyDictionary<string, string?>? settings = null)
    {
        var database = "horta_test_" + Guid.CreateVersion7().ToString("N");
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
        await create.ExecuteNonQueryAsync();
        databases.Add(database);
        // Cada caso usa um banco descartável; pools por banco reteriam conexões até o fim da classe.
        var builder = new NpgsqlConnectionStringBuilder(adminConnection) { Database = database, Pooling = false };
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString, PostgresEnums.Configure).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        if (withContext) await TestContext.InsertAsync(db);
        var factory = new ApiFactory(builder.ConnectionString, settings: settings) { CustomizeServices = customize };
        if (withUsers) await factory.InitializeUsersAsync();
        return factory;
    }

    public async Task DisposeAsync()
    {
        if (databases.Count == 0)
        {
            if (container is not null) await container.DisposeAsync();
            return;
        }
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        foreach (var database in databases)
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
            // A remoção de arquivos no PostgreSQL local do Windows pode exceder o timeout padrão de 30 s.
            drop.CommandTimeout = 180;
            await drop.ExecuteNonQueryAsync();
        }
        if (container is not null) await container.DisposeAsync();
    }
}
