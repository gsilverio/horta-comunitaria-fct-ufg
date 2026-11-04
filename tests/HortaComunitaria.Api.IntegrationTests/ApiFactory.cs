using System.Collections.Concurrent;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
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

public sealed class ApiFactory(string connectionString,
    string environment = "Development", bool demoEnabled = true) : WebApplicationFactory<Program>
{
    public CapturedLogs Logs { get; } = new();
    public Action<IServiceCollection>? CustomizeServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Persistence:ConnectionString"] = connectionString,
                ["Persistence:ApplyMigrationsOnStartup"] = "false",
                ["DemoMode:Enabled"] = demoEnabled.ToString()
            }));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddSingleton<TimeProvider>(new FixedTimeProvider());
            CustomizeServices?.Invoke(services);
        });
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
