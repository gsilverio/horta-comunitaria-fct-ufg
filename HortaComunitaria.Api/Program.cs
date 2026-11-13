using HortaComunitaria.Api.Infrastructure.DependencyInjection;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Files;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Serilog;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
await using var bootstrapLogger = new LoggerConfiguration().ConfigureConsole(environmentName)
    .Enrich.WithProperty("SourceContext", typeof(Program).FullName).CreateLogger();
WebApplication? app = null;
Microsoft.Extensions.Logging.ILogger? applicationLogger = null;
var startupProcess = new ProcessContext("Host.Startup");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddObservability(builder.Configuration, builder.Environment);
    builder.Services.AddPersistence(builder.Configuration, builder.Environment);
    builder.Services.AddFileStorage(builder.Configuration);
    builder.Services.AddAppIdentity(builder.Configuration, builder.Environment);
    builder.Services.AddApplication();
    builder.Services.AddTechnicalEndpoints();

    app = builder.Build();
    applicationLogger = app.Logger;
    if (args.Contains("--seed-embrapa", StringComparer.Ordinal))
    {
        // Comando administrativo explícito: não inicia servidor nem aplica migrations.
        using var seedScope = app.Services.CreateScope();
        var seed = ActivatorUtilities.CreateInstance<HortaComunitaria.Api.Infrastructure.Persistence.Seeds.EmbrapaCatalogSeed>(seedScope.ServiceProvider);
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        await seed.RunAsync(cancellation.Token);
        return;
    }
    if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
    {
        // Comando administrativo explícito: cria o primeiro Adm sem iniciar servidor nem aplicar migrations.
        using var bootstrapScope = app.Services.CreateScope();
        Environment.ExitCode = await bootstrapScope.ServiceProvider.GetRequiredService<AdministratorBootstrap>().RunAsync(CancellationToken.None);
        return;
    }
    if (args.Contains("--seed-test-admins", StringComparer.Ordinal))
    {
        // Comando administrativo explícito: contas Adm de teste do ambiente demonstrativo, lidas do ambiente.
        using var testAdminsScope = app.Services.CreateScope();
        Environment.ExitCode = await testAdminsScope.ServiceProvider.GetRequiredService<TestAdministratorsSeed>().RunAsync(CancellationToken.None);
        return;
    }
    if (args.Contains("--seed-demo", StringComparer.Ordinal))
    {
        // Comando administrativo explícito: cenário fictício da reunião de validação, sem servidor nem migrations.
        using var demoScope = app.Services.CreateScope();
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Environment.ExitCode = await demoScope.ServiceProvider
            .GetRequiredService<HortaComunitaria.Api.Features.Demonstration.SeedValidationScenario.SeedValidationScenarioCommandHandler>()
            .RunAsync(cancellation.Token);
        // Segunda horta: mapa com canteiros de várias formas e cultivos em fases distintas para o panorama.
        if (Environment.ExitCode == 0)
        {
            using var panoramaScope = app.Services.CreateScope();
            Environment.ExitCode = await panoramaScope.ServiceProvider
                .GetRequiredService<HortaComunitaria.Api.Features.Demonstration.SeedPanoramaScenario.SeedPanoramaScenarioCommandHandler>()
                .RunAsync(cancellation.Token);
        }
        return;
    }
    app.UseObservability();
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseAppIdentity();
    app.MapAllEndpoints();
    app.MapTechnicalEndpoints();
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        using var scope = app.Logger.BeginScope(startupProcess.Properties("Completed"));
        app.Logger.LogInformation("Aplicação iniciada; evento {EventName}, resultado {Outcome}, duração {ElapsedMs:0.000} ms",
            "ApplicationStarted", "Succeeded", startupProcess.ElapsedMs);
    });
    startupProcess.Advance("StartingHost");
    using (app.Logger.BeginScope(startupProcess.Properties("Started")))
        app.Logger.LogInformation("Iniciando aplicação; evento {EventName}", "ApplicationStarting");
    await app.StartAsync();
    await app.WaitForShutdownAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    if (applicationLogger is not null)
    {
        using var scope = applicationLogger.BeginScope(startupProcess.Properties("Failed"));
        applicationLogger.LogCritical(exception, "Falha na inicialização da aplicação; evento {EventName}, resultado {Outcome}, duração {ElapsedMs:0.000} ms",
            "ApplicationStartupFailed", "Failed", startupProcess.ElapsedMs);
    }
    else
        bootstrapLogger.ForContext("ProcessName", startupProcess.Name).ForContext("ProcessId", startupProcess.Id)
            .ForContext("Module", "Host").ForContext("Phase", "Failed").ForContext("StepName", "BuildingHost")
            .Fatal(exception, "Falha na inicialização da aplicação; evento {EventName}, resultado {Outcome}",
                "ApplicationStartupFailed", "Failed");
    throw;
}
finally
{
    if (app is not null) await app.DisposeAsync();
}

/// <summary>Ponto de entrada acessível ao host dos testes de integração.</summary>
public partial class Program;
