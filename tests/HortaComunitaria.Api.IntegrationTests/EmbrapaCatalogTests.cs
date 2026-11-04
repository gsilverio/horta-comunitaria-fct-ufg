using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class EmbrapaCatalogTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Import_is_complete_offline_audited_and_idempotent_after_edits()
    {
        await using var factory = await postgres.CreateFactoryAsync();
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var seed = ActivatorUtilities.CreateInstance<EmbrapaCatalogSeed>(scope.ServiceProvider);
        Assert.Equal(new CatalogSeedResult(23, 113), await seed.RunAsync(default));
        Guid id = default;
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(23, await db.Species.CountAsync(x => x.CatalogKey != null));
            Assert.Equal(113, await db.Cultivars.CountAsync(x => x.CatalogKey != null));
            Assert.Equal(136, await db.AuditEntries.CountAsync(x => x.Reason == "Importação explícita do catálogo Embrapa 2023."));
            var species = await db.Species.SingleAsync(x => x.CatalogKey == "embrapa-2023:alface");
            id = species.Id;
            var reason = ChangeReason.Create("Revisão local do cadastro").Value;
            Assert.True(species.Update(species.Version, reason, DateTimeOffset.UtcNow, "Alface revisada", species.ScientificName, species.Description).IsSuccess);
            var cultivar = await db.Cultivars.FirstAsync(x => x.SpeciesId == id);
            Assert.True(cultivar.Inactivate(cultivar.Version, reason, DateTimeOffset.UtcNow).IsSuccess);
            await db.SaveChangesAsync();
        });
        Assert.Equal(new CatalogSeedResult(0, 0), await seed.RunAsync(default));
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(23, await db.Species.CountAsync(x => x.CatalogKey != null));
            Assert.Equal(113, await db.Cultivars.CountAsync(x => x.CatalogKey != null));
            Assert.Equal(1, await db.Cultivars.CountAsync(x => x.SpeciesId == id && !x.IsActive));
        });
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/v1/species/{id}");
        Assert.Equal("Alface revisada", detail.GetProperty("commonName").GetString());
        Assert.False(detail.TryGetProperty("imageBase64", out _));
        var image = await client.GetFromJsonAsync<JsonElement>(detail.GetProperty("imageUrl").GetString());
        var bytes = Convert.FromBase64String(image.GetProperty("base64").GetString()!);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), image.GetProperty("sha256").GetString());
        Assert.StartsWith("https://commons.wikimedia.org/", image.GetProperty("sourceUrl").GetString());
        Assert.False(string.IsNullOrWhiteSpace(image.GetProperty("author").GetString()));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/species/{Guid.CreateVersion7()}/image")).StatusCode);
    }

    [Fact]
    public async Task Import_rolls_back_everything_when_a_manual_catalog_conflicts()
    {
        await using var factory = await postgres.CreateFactoryAsync();
        await factory.WithDbAsync(async db =>
        {
            db.Species.Add(Species.Create("Tomate", "Solanum lycopersicum", null).Value);
            await db.SaveChangesAsync();
        });
        using var scope = factory.Services.CreateScope();
        var seed = ActivatorUtilities.CreateInstance<EmbrapaCatalogSeed>(scope.ServiceProvider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => seed.RunAsync(default));
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(0, await db.Species.CountAsync(x => x.CatalogKey != null));
            Assert.Equal(0, await db.Cultivars.CountAsync(x => x.CatalogKey != null));
            Assert.Equal(0, await db.AuditEntries.CountAsync(x => x.Reason == "Importação explícita do catálogo Embrapa 2023."));
        });
    }
}
