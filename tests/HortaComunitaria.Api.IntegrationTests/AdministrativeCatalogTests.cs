using System.Net;
using System.Text.Json;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Activities;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class AdministrativeCatalogTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Administrative_reads_include_inactive_versions_and_preserve_active_selectors()
    {
        using var factory = await postgres.CreateFactoryAsync(withContext: false);
        using var client = factory.CreateClient();
        var active = Garden.Create("A", "Ativa").Value;
        var inactive = Garden.Create("Z", "Inativa", false).Value;
        await factory.WithDbAsync(async db =>
        {
            db.Gardens.AddRange(active, inactive);
            db.IrrigationSectors.AddRange(
                IrrigationSector.Create(inactive.Id, "A", "Ativo").Value,
                IrrigationSector.Create(inactive.Id, "Z", "Inativo", false).Value);
            db.Participants.AddRange(
                Participant.Create(inactive.Id, "Ativo").Value,
                Participant.Create(inactive.Id, "Inativo", false).Value);
            await db.SaveChangesAsync();
        });
        foreach (var path in new[] { "/api/v1/gardens/catalog", $"/api/v1/gardens/{inactive.Id}/sectors/catalog", $"/api/v1/gardens/{inactive.Id}/participants/catalog" })
        {
            using var all = JsonDocument.Parse(await client.GetStringAsync(path));
            Assert.Equal(2, all.RootElement.GetProperty("totalCount").GetInt32());
            Assert.All(all.RootElement.GetProperty("items").EnumerateArray(), item => Assert.Equal(1, item.GetProperty("version").GetInt32()));
            using var filtered = JsonDocument.Parse(await client.GetStringAsync(path + "?isActive=false&pageSize=1"));
            Assert.Equal(1, filtered.RootElement.GetProperty("totalCount").GetInt32());
            Assert.False(filtered.RootElement.GetProperty("items")[0].GetProperty("isActive").GetBoolean());
            using var first = JsonDocument.Parse(await client.GetStringAsync(path + "?pageSize=1"));
            using var second = JsonDocument.Parse(await client.GetStringAsync(path + "?pageSize=1&page=2"));
            Assert.NotEqual(first.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid(), second.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + "?page=0")).StatusCode);
        }
        using var selector = JsonDocument.Parse(await client.GetStringAsync("/api/v1/gardens"));
        Assert.Equal(active.Id, selector.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(1, selector.RootElement.GetProperty("totalCount").GetInt32());
        foreach (var resource in new[] { "sectors", "participants" })
        {
            using var empty = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/gardens/{active.Id}/{resource}/catalog"));
            Assert.Equal(0, empty.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/gardens/{Guid.CreateVersion7()}/{resource}/catalog")).StatusCode);
        }
    }
}
