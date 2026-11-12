using System.Net;
using System.Text.Json;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class OperationsTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static string? Property(LogEvent value, string key) =>
        value.Properties.TryGetValue(key, out var property) ? (property as ScalarValue)?.Value?.ToString() : null;

    [Fact]
    public async Task Request_and_process_events_share_trace_and_do_not_log_arbitrary_query()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        const string trace = "aabbccddeeff00112233445566778899";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/gardens?password=do-not-log-me");
        request.Headers.Add("traceparent", $"00-{trace}-1122334455667788-01");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(trace, response.Headers.GetValues("X-Trace-Id").Single());
        var events = factory.Logs.Events.Where(x => Property(x, "TraceId") == trace).ToArray();
        var process = Assert.Single(events, x => Property(x, "EventName") == "ProcessCompleted");
        var started = Assert.Single(events, x => Property(x, "EventName") == "ProcessStarted");
        Assert.Equal(LogEventLevel.Information, started.Level);
        var step = Assert.Single(events, x => Property(x, "EventName") == "ProcessStep");
        Assert.Equal("Querying", Property(step, "StepName"));
        Assert.Equal("Gardens", Property(step, "Module"));
        Assert.Contains("hortas ativas", step.RenderMessage());
        Assert.Equal("Gardens.ListGardens", Property(process, "ProcessName"));
        Assert.Equal("Succeeded", Property(process, "Outcome"));
        Assert.True(process.Properties.ContainsKey("ProcessId"));
        Assert.True(process.Properties.ContainsKey("ElapsedMs"));
        var http = Assert.Single(events, x => Property(x, "EventName") == "HttpRequestCompleted");
        Assert.True(http.Properties.ContainsKey("RoutePattern"));
        Assert.True(http.Properties.ContainsKey("StatusCode"));
        Assert.Equal("HortaComunitaria.Api", Property(http, "Application"));
        Assert.False(string.IsNullOrWhiteSpace(Property(http, "MachineName")));
        Assert.Equal(Property(started, "ProcessId"), Property(process, "ProcessId"));
        Assert.Equal(Property(started, "ProcessId"), Property(step, "ProcessId"));
        Assert.Equal(Property(started, "ProcessId"), Property(http, "ProcessId"));
        Assert.DoesNotContain("do-not-log-me", string.Join("\n", factory.Logs.Events.Select(x => x.ToString())));
    }

    [Fact]
    public async Task Expected_rejection_is_distinct_from_technical_failure()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/gardens?page=0");
        var trace = response.Headers.GetValues("X-Trace-Id").Single();
        var events = factory.Logs.Events.Where(x => Property(x, "TraceId") == trace).ToArray();
        var rejection = Assert.Single(events, x => Property(x, "EventName") == "ProcessRejected");
        Assert.Equal("Rejected", Property(rejection, "Outcome"));
        Assert.Equal("validation.invalid_request", Property(rejection, "ErrorCode"));
        Assert.Equal(LogEventLevel.Warning, rejection.Level);
        Assert.Equal("Rejected", Property(rejection, "Phase"));
        Assert.Equal(LogEventLevel.Warning, Assert.Single(events, x => Property(x, "EventName") == "HttpRequestCompleted").Level);
        Assert.All(events, x => Assert.Null(x.Exception));
    }

    [Fact]
    public async Task Database_failure_returns_safe_problem_and_logs_exception_once()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(db => db.Database.ExecuteSqlRawAsync("DROP TABLE gardens CASCADE"));
        var response = await client.GetAsync("/api/v1/gardens");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SELECT", content);
        Assert.DoesNotContain("Npgsql", content);
        using var json = JsonDocument.Parse(content);
        Assert.Equal("server.unexpected_error", json.RootElement.GetProperty("code").GetString());
        var trace = response.Headers.GetValues("X-Trace-Id").Single();
        var events = factory.Logs.Events.Where(x => Property(x, "TraceId") == trace).ToArray();
        var exception = Assert.Single(events, x => x.Exception is not null);
        Assert.Equal("UnhandledException", Property(exception, "EventName"));
        Assert.Equal("Gardens.ListGardens", Property(exception, "ProcessName"));
        Assert.Equal("Failed", Property(exception, "Phase"));
        Assert.Equal("Querying", Property(exception, "StepName"));
        Assert.NotNull(Property(exception, "ElapsedMs"));
        var started = Assert.Single(events, x => Property(x, "EventName") == "ProcessStarted");
        Assert.Equal(Property(started, "ProcessId"), Property(exception, "ProcessId"));
        Assert.DoesNotContain(events, x => Property(x, "EventName") == "ProcessCompleted");
        Assert.Single(events, x => Property(x, "EventName") == "HttpRequestCompleted");
    }

    [Fact]
    public async Task Parallel_requests_keep_distinct_processes_and_matching_http_summaries()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var responses = await Task.WhenAll(client.GetAsync("/api/v1/gardens"), client.GetAsync("/api/v1/gardens"));
        var ids = new List<string>();
        foreach (var response in responses)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var trace = response.Headers.GetValues("X-Trace-Id").Single();
                var events = factory.Logs.Events.Where(x => Property(x, "TraceId") == trace).ToArray();
                var started = Assert.Single(events, x => Property(x, "EventName") == "ProcessStarted");
                var id = Property(started, "ProcessId")!;
                Assert.Equal(7, Guid.Parse(id).Version);
                Assert.All(events, entry => Assert.Equal(id, Property(entry, "ProcessId")));
                ids.Add(id);
            }
        }
        Assert.Equal(2, ids.Distinct().Count());
    }

    [Fact]
    public async Task Client_cancellation_keeps_the_last_step_and_is_not_logged_as_failure()
    {
        var logs = new CapturedLogs();
        using var serilog = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(logs).CreateLogger();
        using var loggers = LoggerFactory.Create(builder => builder.AddSerilog(serilog, dispose: false));
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(canceled: true) };
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new EndpointNameMetadata("Tests.CancelledProcess")), "Cancellation"));
        using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var errors = new ErrorHandlingMiddleware(async _ =>
        {
            using var process = new ProcessLog(loggers.CreateLogger("CancellationHandler"), "Tests.CancelledProcess");
            process.Step("Querying", "Consultando dados antes do cancelamento");
            await Task.Yield();
            throw new OperationCanceledException(context.RequestAborted);
        }, loggers.CreateLogger<ErrorHandlingMiddleware>());
        var correlation = new CorrelationMiddleware(
            ctx => errors.InvokeAsync(ctx, services.GetRequiredService<IProblemDetailsService>()),
            loggers.CreateLogger<CorrelationMiddleware>());
        await correlation.InvokeAsync(context);
        Assert.Equal(499, context.Response.StatusCode);
        var cancelled = Assert.Single(logs.Events, x => Property(x, "EventName") == "HttpRequestCancelled");
        Assert.Equal(LogEventLevel.Debug, cancelled.Level);
        Assert.Equal("Cancelled", Property(cancelled, "Phase"));
        Assert.Equal("Querying", Property(cancelled, "StepName"));
        Assert.Equal(Property(Assert.Single(logs.Events, x => Property(x, "EventName") == "ProcessStarted"), "ProcessId"),
            Property(cancelled, "ProcessId"));
        Assert.DoesNotContain(logs.Events, x => x.Exception is not null || x.Level >= LogEventLevel.Error);
    }

    [Fact]
    public async Task Liveness_and_readiness_report_independent_database_availability()
    {
        using var healthy = await postgres.CreateFactoryAsync();
        using var client = healthy.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        using var unavailable = new ApiFactory("Host=127.0.0.1;Port=1;Database=missing;Username=none;Timeout=1");
        using var unavailableClient = unavailable.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await unavailableClient.GetAsync("/health/live")).StatusCode);
        var ready = await unavailableClient.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        using var json = JsonDocument.Parse(await ready.Content.ReadAsStringAsync());
        Assert.Equal("Unhealthy", json.RootElement.GetProperty("status").GetString());
        Assert.Single(json.RootElement.EnumerateObject());
    }

    [Theory]
    [InlineData("Authentication:AccessTokenLifetime", "02:00:00")]
    [InlineData("Authentication:RefreshTokenLifetime", "00:10:00")]
    [InlineData("Authentication:RateLimitPerMinute", "0")]
    public void Invalid_authentication_configuration_refuses_startup(string key, string value)
    {
        using var factory = new ApiFactory("Host=127.0.0.1;Port=1;Database=unused;Username=unused",
            settings: new Dictionary<string, string?> { [key] = value });
        // O adaptador WebApplicationFactory pode observar o provider já descartado após falha fatal.
        // A causa original deve permanecer registrada exatamente uma vez pelo host.
        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        var fatal = Assert.Single(factory.Logs.Events, x => x.Exception is not null);
        var exception = Assert.IsType<OptionsValidationException>(fatal.Exception);
        Assert.Contains(key, exception.Message);
        Assert.Equal(LogEventLevel.Fatal, fatal.Level);
    }
}
