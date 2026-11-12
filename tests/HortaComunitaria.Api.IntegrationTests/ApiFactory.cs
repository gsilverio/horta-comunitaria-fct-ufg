using System.Collections.Concurrent;
using System.Net.Http.Headers;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class CapturedLogs : ILogEventSink
{
    public ConcurrentQueue<LogEvent> Events { get; } = new();
    public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
}

/// <summary>
/// Host da API para os testes. <see cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/> devolve um cliente já
/// autenticado como Adm quando <see cref="InitializeUsersAsync"/> foi executado; use <see cref="CreateClient(UserProfile)"/>
/// para o Operador e <see cref="CreateAnonymousClient"/> para requisições sem credencial.
/// </summary>
public sealed class ApiFactory(string connectionString, string environment = "Development",
    IReadOnlyDictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    // Chaves do Data Protection descartáveis, fora da árvore do projeto.
    private readonly string keysPath = Path.Combine(Path.GetTempPath(), "horta-test-keys", Guid.CreateVersion7().ToString("N"));
    private readonly Dictionary<UserProfile, string> accessTokens = [];

    public CapturedLogs Logs { get; } = new();
    public Action<IServiceCollection>? CustomizeServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:ConnectionString"] = connectionString,
                ["Persistence:ApplyMigrationsOnStartup"] = "false",
                ["Authentication:KeysPath"] = keysPath,
                // Os testes fazem vários logins seguidos a partir do mesmo cliente em memória.
                ["Authentication:RateLimitPerMinute"] = "1000"
            });
            if (settings is not null) configuration.AddInMemoryCollection(settings);
        });
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddSingleton<TimeProvider>(new FixedTimeProvider());
            // Alguns testes avançam o relógio em horas; o token da fixture não deve expirar no meio do caso.
            services.PostConfigure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
                options.BearerTokenExpiration = TimeSpan.FromDays(1));
            CustomizeServices?.Invoke(services);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        if (accessTokens.TryGetValue(UserProfile.Administrator, out var token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Cliente autenticado com o usuário de teste do perfil informado.</summary>
    public HttpClient CreateClient(UserProfile profile)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken(profile));
        return client;
    }

    /// <summary>Cliente sem cabeçalho <c>Authorization</c>.</summary>
    public HttpClient CreateAnonymousClient(WebApplicationFactoryClientOptions? options = null)
    {
        var client = options is null ? CreateClient() : CreateClient(options);
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }

    /// <summary>Access token emitido para o usuário de teste do perfil informado.</summary>
    public string AccessToken(UserProfile profile) => accessTokens.TryGetValue(profile, out var token)
        ? token : throw new InvalidOperationException("Usuários de teste não criados; use CreateFactoryAsync(withUsers: true).");

    /// <summary>
    /// Cria Adm e Operador com senha já trocada pelo <see cref="UserManager{TUser}"/> no banco descartável e emite
    /// os access tokens pelo mesmo serviço do login.
    /// </summary>
    internal async Task InitializeUsersAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        TestContext.EnsureDisposableDatabase(db);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var tokens = scope.ServiceProvider.GetRequiredService<AuthTokenService>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        foreach (var account in TestAccounts.All)
        {
            var user = AppUser.Create(account.Email, account.DisplayName, account.Profile, now);
            user.Id = account.Id;
            user.CompletePasswordChange();
            var created = await users.CreateAsync(user, TestAccounts.Password);
            if (!created.Succeeded)
                throw new InvalidOperationException("Usuário de teste recusado: " + string.Join(" ", created.Errors.Select(x => x.Description)));
            accessTokens[account.Profile] = (await tokens.IssueAsync(user)).AccessToken;
        }
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (Directory.Exists(keysPath)) Directory.Delete(keysPath, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

/// <summary>Contas fictícias criadas pela fixture em cada banco descartável.</summary>
public sealed record TestAccount(Guid Id, string Email, string DisplayName, UserProfile Profile);

/// <summary>Credenciais fictícias exclusivas dos testes; nunca usar fora do banco <c>horta_test_</c>.</summary>
public static class TestAccounts
{
    /// <summary>Senha comum das contas de teste, já trocada (sem troca pendente).</summary>
    public const string Password = "Regador-Azul-Folha-73";
    public static TestAccount Administrator { get; } = new(DemoIds.Administrator, "adm@horta.test", "Adm fictício", UserProfile.Administrator);
    public static TestAccount Operator { get; } = new(DemoIds.Operator, "operador@horta.test", "Operador fictício", UserProfile.Operator);
    public static IReadOnlyList<TestAccount> All { get; } = [Administrator, Operator];
}
