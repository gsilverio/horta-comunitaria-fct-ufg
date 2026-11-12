using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.IntegrationTests;

/// <summary>Perfis Adm/Operador por ação e autoria resolvida pela sessão.</summary>
public sealed class AuthorizationTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Operator_cannot_change_structure_catalogs_or_users()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient(UserProfile.Operator);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/gardens", new { name = "Horta do Operador" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(IrrigationTestData.BasePath + "/beds", new { name = "Canteiro do Operador" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/species", new { commonName = "Espécie do Operador" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/users",
            new { email = "x@horta.test", displayName = "Nova conta", profile = "Administrator", initialPassword = "Inicial-Semente-48" })).StatusCode);
        // Consultas continuam liberadas ao Operador.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/gardens")).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(1, await db.Gardens.CountAsync());
            Assert.Equal(DemoIds.Beds.Length, await db.Beds.CountAsync());
            Assert.Equal(0, await db.Species.CountAsync());
            Assert.Equal(TestAccounts.All.Count, await db.Users.CountAsync());
            Assert.Equal(0, await db.AuditEntries.CountAsync());
        });
        using var admin = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/v1/species", new { commonName = "Espécie do Adm" })).StatusCode);
    }

    [Fact]
    public async Task Operator_records_field_facts_as_the_authenticated_author()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient(UserProfile.Operator);
        var created = await IrrigationTestData.CreateAsync(client, IrrigationTestData.Request());
        Assert.Equal(DemoIds.Operator, created.RecordedBy.Id);
        Assert.Equal(TestAccounts.Operator.DisplayName, created.RecordedBy.DisplayName);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(DemoIds.Operator, (await db.Irrigations.SingleAsync()).RecordedByUserId);
            Assert.Equal(DemoIds.Operator, (await db.AuditEntries.SingleAsync()).UserId);
        });
    }

    [Fact]
    public async Task Administrator_cannot_demote_or_deactivate_own_account()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        var path = $"/api/v1/users/{DemoIds.Administrator}";
        var version = (await client.GetFromJsonAsync<JsonElement>(path)).GetProperty("version").GetString();
        foreach (var (profile, isActive) in new[] { ("Operator", true), ("Administrator", false) })
        {
            var response = await client.PutAsJsonAsync(path, new
            {
                displayName = TestAccounts.Administrator.DisplayName, profile, isActive, expectedVersion = version, reason = "Tentativa de rebaixamento"
            });
            await IrrigationChangesTests.AssertProblemAsync(response, HttpStatusCode.Conflict, "user.self_demotion");
        }
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Id == DemoIds.Administrator);
            Assert.Equal(UserProfile.Administrator, user.Profile);
            Assert.True(user.IsActive);
            Assert.Equal(0, await db.AuditEntries.CountAsync());
        });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task Deactivated_user_loses_writes_refresh_and_login_immediately()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var admin = factory.CreateClient();
        using var operatorClient = factory.CreateClient(UserProfile.Operator);
        using var anonymous = factory.CreateAnonymousClient(new() { HandleCookies = false });
        var login = await AuthenticationTests.LoginAsync(anonymous, TestAccounts.Operator.Email, TestAccounts.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var refreshCookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Secure-horta.refresh=", StringComparison.Ordinal)).Split(';')[0];

        var path = $"/api/v1/users/{DemoIds.Operator}";
        var version = (await admin.GetFromJsonAsync<JsonElement>(path)).GetProperty("version").GetString();
        var updated = await admin.PutAsJsonAsync(path, new
        {
            displayName = TestAccounts.Operator.DisplayName, profile = "Operator", isActive = false, expectedVersion = version, reason = "Saiu do projeto"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        // O access token ainda não expirou, mas a gravação confere no banco que o autor está ativo.
        Assert.Equal(HttpStatusCode.Unauthorized, (await IrrigationTestData.PostAsync(operatorClient, IrrigationTestData.Request())).StatusCode);
        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refresh.Headers.Add("Cookie", refreshCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(refresh)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthenticationTests.LoginAsync(anonymous, TestAccounts.Operator.Email, TestAccounts.Password)).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(0, await db.Irrigations.CountAsync());
            Assert.Equal(DemoIds.Administrator, (await db.AuditEntries.SingleAsync()).UserId);
        });
    }
}
