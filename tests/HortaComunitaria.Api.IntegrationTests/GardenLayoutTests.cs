using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class GardenLayoutTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static LayoutDocument Document(Guid? bedId = null) => new(20, 20,
        [new(0, 0), new(20, 0), new(20, 20), new(0, 20)],
        [new("s", "Setor do desenho", "sector", "rectangle", X: 1, Y: 1, Width: 18, Height: 18, RotationDeg: 0),
         new("b", "Canteiro do desenho", "bed", "rectangle", EntityId: bedId, SectorId: "s", X: 2, Y: 2, Width: 3, Height: 2, RotationDeg: 0)]);
    private static async Task<Guid> Garden(HttpClient client) => (await (await client.PostAsJsonAsync("/api/v1/gardens", new { name = "Horta do mapa" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

    [Fact]
    public async Task Saves_geometry_catalogs_and_history_atomically_without_real_dimensions_and_retries_without_duplicates()
    {
        using var factory = await postgres.CreateFactoryAsync(); using var client = factory.CreateClient();
        var garden = await Garden(client); var path = $"/api/v1/gardens/{garden}/layout";
        var request = new { operationId = Guid.CreateVersion7(), expectedVersion = 0, layout = Document() };
        var first = await client.PutAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("version").GetInt32());
        var document = body.GetProperty("layout").Deserialize<LayoutDocument>(Json)!;
        Assert.All(document.Elements, element => Assert.NotNull(element.EntityId));
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(path, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path, new { request.operationId, expectedVersion = 0, layout = Document() with { Width = 21 } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path, new { operationId = Guid.CreateVersion7(), expectedVersion = 0, layout = document })).StatusCode);
        using var anotherClient = factory.CreateClient();
        var read = await anotherClient.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(body.GetProperty("layout").GetRawText(), read.GetProperty("layout").GetRawText());
        await factory.WithDbAsync(async db => {
            Assert.Equal(1, await db.IrrigationSectors.CountAsync(x => x.GardenId == garden));
            Assert.Equal(1, await db.Beds.CountAsync(x => x.GardenId == garden));
            Assert.Equal(1, await db.BedSectorAssignments.CountAsync(x => x.GardenId == garden));
            Assert.False(await db.BedConfigurations.AnyAsync(x => x.GardenId == garden));
            Assert.Equal(1, await db.AuditEntries.CountAsync(x => x.GardenId == garden && x.EntityType == "GardenLayout"));
        });
        var update = new { operationId = Guid.CreateVersion7(), expectedVersion = 1, layout = document with { Width = 21 } };
        var saves = await Task.WhenAll(client.PutAsJsonAsync(path, update), anotherClient.PutAsJsonAsync(path, new { operationId = Guid.CreateVersion7(), expectedVersion = 1, layout = document }));
        Assert.Single(saves, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(saves, x => x.StatusCode == HttpStatusCode.Conflict);
        await factory.WithDbAsync(async db => Assert.Equal(2, (await db.Set<GardenLayout>().SingleAsync(x => x.GardenId == garden)).Version));
    }

    [Fact]
    public async Task Rejects_foreign_references_and_invalid_geometry_without_leaving_partial_catalogs()
    {
        using var factory = await postgres.CreateFactoryAsync(); using var client = factory.CreateClient();
        var garden = await Garden(client); var otherGarden = await Garden(client);
        var foreign = await (await client.PostAsJsonAsync($"/api/v1/gardens/{otherGarden}/beds", new { name = "Canteiro de outra horta" })).Content.ReadFromJsonAsync<JsonElement>();
        var path = $"/api/v1/gardens/{garden}/layout";
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, new { operationId = Guid.CreateVersion7(), expectedVersion = 0, layout = Document(foreign.GetProperty("id").GetGuid()) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(path, new { operationId = Guid.CreateVersion7(), expectedVersion = 0, layout = Document() with { Boundary = [new(0, 0), new(20, 20), new(0, 20), new(20, 0)] } })).StatusCode);
        await factory.WithDbAsync(async db => {
            Assert.False(await db.IrrigationSectors.AnyAsync(x => x.GardenId == garden));
            Assert.False(await db.Beds.AnyAsync(x => x.GardenId == garden));
            Assert.False(await db.Set<GardenLayout>().AnyAsync(x => x.GardenId == garden));
        });
    }

    [Fact]
    public async Task Removing_map_geometry_preserves_historical_catalogs_and_can_be_loaded_from_another_session()
    {
        using var factory = await postgres.CreateFactoryAsync(); using var client = factory.CreateClient();
        var garden = await Garden(client); var path = $"/api/v1/gardens/{garden}/layout";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(path, new { operationId = Guid.CreateVersion7(), expectedVersion = 0, layout = Document() })).StatusCode);
        var empty = new LayoutDocument(20, 20, null, []);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(path, new { operationId = Guid.CreateVersion7(), expectedVersion = 1, layout = empty })).StatusCode);
        var read = await client.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(JsonValueKind.Null, read.GetProperty("layout").GetProperty("boundary").ValueKind);
        await factory.WithDbAsync(async db => {
            Assert.Equal(1, await db.Beds.CountAsync(x => x.GardenId == garden));
            Assert.Equal(1, await db.IrrigationSectors.CountAsync(x => x.GardenId == garden));
            Assert.Equal(2, await db.AuditEntries.CountAsync(x => x.GardenId == garden && x.EntityType == "GardenLayout"));
        });
    }
}
