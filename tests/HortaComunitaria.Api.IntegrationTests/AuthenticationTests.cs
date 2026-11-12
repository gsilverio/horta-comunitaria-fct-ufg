using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HortaComunitaria.Api.IntegrationTests;

/// <summary>Sessão (login, bloqueio, renovação, saída e troca obrigatória de senha) com as contas fictícias da fixture.</summary>
public sealed class AuthenticationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private const string CookieName = "__Secure-horta.refresh";
    private static readonly WebApplicationFactoryClientOptions NoCookies = new() { HandleCookies = false };

    [Fact]
    public async Task Api_requires_authentication_except_technical_endpoints()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var anonymous = factory.CreateAnonymousClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/gardens")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/live")).StatusCode);
        using var invalid = factory.CreateAnonymousClient();
        invalid.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-adulterado");
        Assert.Equal(HttpStatusCode.Unauthorized, (await invalid.GetAsync("/api/v1/gardens")).StatusCode);
    }

    [Fact]
    public async Task Login_returns_access_token_in_body_and_refresh_token_only_in_restricted_cookie()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var anonymous = factory.CreateAnonymousClient(NoCookies);
        var response = await LoginAsync(anonymous, TestAccounts.Operator.Email, TestAccounts.Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var raw = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(raw);
        Assert.Equal("Bearer", json.RootElement.GetProperty("tokenType").GetString());
        Assert.True(json.RootElement.GetProperty("expiresIn").GetInt64() > 0);
        Assert.DoesNotContain("refresh", raw, StringComparison.OrdinalIgnoreCase);
        var cookie = SetCookie(response);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        using var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal(DemoIds.Operator, me.GetProperty("id").GetGuid());
        Assert.Equal("Operator", me.GetProperty("profile").GetString());
        Assert.False(me.GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_answer_and_five_failures_lock_the_account()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateAnonymousClient(NoCookies);
        await IrrigationChangesTests.AssertProblemAsync(await LoginAsync(client, "ninguem@horta.test", TestAccounts.Password),
            HttpStatusCode.Unauthorized, "auth.invalid_credentials");
        for (var attempt = 0; attempt < 5; attempt++)
            await IrrigationChangesTests.AssertProblemAsync(await LoginAsync(client, TestAccounts.Operator.Email, "Senha-errada-" + attempt),
                HttpStatusCode.Unauthorized, "auth.invalid_credentials");
        // A senha correta continua recusada, com a mesma resposta, enquanto durar o bloqueio.
        var locked = await LoginAsync(client, TestAccounts.Operator.Email, TestAccounts.Password);
        await IrrigationChangesTests.AssertProblemAsync(locked, HttpStatusCode.Unauthorized, "auth.invalid_credentials");
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Id == DemoIds.Operator);
            Assert.NotNull(user.LockoutEnd);
        });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, TestAccounts.Administrator.Email, TestAccounts.Password)).StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_cookie_and_logout_revokes_previous_refresh_tokens()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateAnonymousClient(NoCookies);
        var login = await LoginAsync(client, TestAccounts.Operator.Email, TestAccounts.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var first = CookieValue(login);

        var refreshed = await RefreshAsync(client, first);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var accessToken = (await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        var second = CookieValue(refreshed);
        Assert.False(string.IsNullOrEmpty(accessToken));
        Assert.False(string.IsNullOrEmpty(second));
        await IrrigationChangesTests.AssertProblemAsync(await RefreshAsync(client, "cookie-adulterado"), HttpStatusCode.Unauthorized, "auth.session_expired");
        await IrrigationChangesTests.AssertProblemAsync(await RefreshAsync(client, null), HttpStatusCode.Unauthorized, "auth.session_expired");

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("Cookie", $"{CookieName}={second}");
        var loggedOut = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);
        Assert.Contains(loggedOut.Headers.GetValues("Set-Cookie"), x => x.StartsWith(CookieName + "=;", StringComparison.Ordinal));
        // Logout renova o security stamp: nenhum refresh token emitido antes continua válido.
        await IrrigationChangesTests.AssertProblemAsync(await RefreshAsync(client, second), HttpStatusCode.Unauthorized, "auth.session_expired");
        await IrrigationChangesTests.AssertProblemAsync(await RefreshAsync(client, first), HttpStatusCode.Unauthorized, "auth.session_expired");
    }

    [Fact]
    public async Task Initial_password_blocks_other_routes_until_changed_and_change_revokes_other_sessions()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var admin = factory.CreateClient();
        const string initial = "Inicial-Semente-48";
        var created = await admin.PostAsJsonAsync("/api/v1/users",
            new { email = "nova@horta.test", displayName = "Pessoa nova", profile = "Operator", initialPassword = initial });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var anonymous = factory.CreateAnonymousClient(NoCookies);
        var login = await LoginAsync(anonymous, "nova@horta.test", initial);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var initialRefresh = CookieValue(login);
        using var pending = factory.CreateAnonymousClient(NoCookies);
        pending.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());

        await IrrigationChangesTests.AssertProblemAsync(await pending.GetAsync("/api/v1/gardens"), HttpStatusCode.Forbidden, "auth.password_change_required");
        await IrrigationChangesTests.AssertProblemAsync(await IrrigationTestData.PostAsync(pending, IrrigationTestData.Request()),
            HttpStatusCode.Forbidden, "auth.password_change_required");
        Assert.True((await pending.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("mustChangePassword").GetBoolean());
        await IrrigationChangesTests.AssertProblemAsync(await pending.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = initial, newPassword = initial }), HttpStatusCode.BadRequest, "validation.invalid_request");

        var changed = await pending.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = initial, newPassword = "Outra-Colheita-Verde-91" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.False(string.IsNullOrEmpty(CookieValue(changed)));
        using var ready = factory.CreateAnonymousClient(NoCookies);
        ready.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await ready.GetAsync("/api/v1/gardens")).StatusCode);
        Assert.False((await ready.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("mustChangePassword").GetBoolean());
        await IrrigationChangesTests.AssertProblemAsync(await RefreshAsync(anonymous, initialRefresh), HttpStatusCode.Unauthorized, "auth.session_expired");
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(anonymous, "nova@horta.test", initial)).StatusCode);
        await factory.WithDbAsync(async db => Assert.False((await db.Users.SingleAsync(x => x.Id == id)).MustChangePassword));
    }

    [Fact]
    public async Task Authentication_routes_are_rate_limited_per_client()
    {
        using var factory = await postgres.CreateFactoryAsync(settings: new Dictionary<string, string?> { ["Authentication:RateLimitPerMinute"] = "2" });
        using var client = factory.CreateAnonymousClient(NoCookies);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, TestAccounts.Operator.Email, TestAccounts.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, TestAccounts.Operator.Email, "Senha-errada-1")).StatusCode);
        var limited = await LoginAsync(client, TestAccounts.Operator.Email, TestAccounts.Password);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        using var json = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        Assert.Equal("auth.rate_limited", json.RootElement.GetProperty("code").GetString());
        // Rotas fora da autenticação não consomem a cota.
        using var admin = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/gardens")).StatusCode);
    }

    [Fact]
    public async Task Bootstrap_creates_first_administrator_once_with_pending_password_change()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Bootstrap:Email"] = "primeiro.adm@horta.test",
            ["Bootstrap:DisplayName"] = "Primeiro Adm fictício",
            ["Bootstrap:Password"] = "Inicial-Canteiro-Norte-52"
        };
        await using var factory = await postgres.CreateFactoryAsync(withContext: false, withUsers: false, settings: settings);
        Assert.Equal(0, await RunBootstrapAsync(factory));
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync();
            Assert.Equal("primeiro.adm@horta.test", user.Email);
            Assert.Equal(UserProfile.Administrator, user.Profile);
            Assert.True(user.IsActive);
            Assert.True(user.MustChangePassword);
            Assert.NotNull(user.PasswordHash);
            Assert.DoesNotContain("Inicial-Canteiro-Norte-52", user.PasswordHash);
        });
        Assert.Equal(1, await RunBootstrapAsync(factory));
        await factory.WithDbAsync(async db => Assert.Equal(1, await db.Users.CountAsync()));
        Assert.Contains(factory.Logs.Events, x => x.Properties.TryGetValue("EventName", out var value) && value.ToString().Trim('"') == "BootstrapAlreadyDone");

        using var client = factory.CreateAnonymousClient(NoCookies);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "primeiro.adm@horta.test", "Inicial-Canteiro-Norte-52")).StatusCode);
    }

    [Fact]
    public async Task Bootstrap_refuses_when_an_active_administrator_exists()
    {
        await using var factory = await postgres.CreateFactoryAsync(withContext: false, settings: new Dictionary<string, string?>
        {
            ["Bootstrap:Email"] = "outro.adm@horta.test",
            ["Bootstrap:DisplayName"] = "Outro Adm fictício",
            ["Bootstrap:Password"] = "Inicial-Canteiro-Sul-64"
        });
        Assert.Equal(1, await RunBootstrapAsync(factory));
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(TestAccounts.All.Count, await db.Users.CountAsync());
            Assert.False(await db.Users.AnyAsync(x => x.Email == "outro.adm@horta.test"));
        });
    }

    internal static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        if (cookie is not null) request.Headers.Add("Cookie", $"{CookieName}={cookie}");
        return client.SendAsync(request);
    }

    private static string SetCookie(HttpResponseMessage response) =>
        Assert.Single(response.Headers.GetValues("Set-Cookie"), x => x.StartsWith(CookieName + "=", StringComparison.Ordinal));

    private static string CookieValue(HttpResponseMessage response) =>
        SetCookie(response).Split(';')[0][(CookieName.Length + 1)..];

    private static async Task<int> RunBootstrapAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AdministratorBootstrap>().RunAsync(CancellationToken.None);
    }
}
