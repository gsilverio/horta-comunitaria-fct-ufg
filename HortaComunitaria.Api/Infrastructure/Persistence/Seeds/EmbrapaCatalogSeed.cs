using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Seeds;

internal sealed record CatalogDocument(int SchemaVersion, CatalogSpecies[] Species);
internal sealed record CatalogSpecies(string Key, string CommonName, string ScientificName, string Description, CatalogCultivar[] Cultivars);
internal sealed record CatalogCultivar(string Name, int SourcePage);
internal sealed record CatalogSeedResult(int SpeciesCreated, int CultivarsCreated);

// Importação explícita, offline e aditiva. Nunca executada por migrations ou pelo startup HTTP.
internal sealed class EmbrapaCatalogSeed(AppDbContext db, TimeProvider clock, ILogger<EmbrapaCatalogSeed> logger)
{
    internal static T ReadResource<T>(string name)
    {
        var assembly = typeof(EmbrapaCatalogSeed).Assembly;
        using var stream = assembly.GetManifestResourceStream($"HortaComunitaria.Api.Infrastructure.Persistence.Seeds.Data.{name}")
            ?? throw new InvalidOperationException("Recurso de catálogo ausente.");
        return JsonSerializer.Deserialize<T>(stream, AuditSnapshots.Options)
            ?? throw new InvalidOperationException("Recurso de catálogo vazio.");
    }

    internal async Task<CatalogSeedResult> RunAsync(CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Catalog.ImportEmbrapa");
        var catalog = ReadResource<CatalogDocument>("embrapa-catalog.json");
        var images = ReadResource<Dictionary<string, JsonObject>>("commons-images.json");
        if (catalog.SchemaVersion != 1 || catalog.Species.Length != 23 || catalog.Species.Sum(x => x.Cultivars.Length) != 113 ||
            catalog.Species.Select(x => x.Key).Distinct().Count() != catalog.Species.Length)
            throw new InvalidOperationException("Manifesto Embrapa inválido.");
        // Verifica toda a carga antes de abrir transação ou tocar nos registros.
        var prepared = catalog.Species.Select(item =>
        {
            var image = images[item.Key].DeepClone().AsObject();
            if (image["reviewed"]?.GetValue<bool>() != true)
                throw new InvalidOperationException($"Imagem não revisada: {item.Key}.");
            var base64 = image["base64"]!.GetValue<string>();
            var hash = Convert.ToHexStringLower(SHA256.HashData(Convert.FromBase64String(base64)));
            if (hash != image["sha256"]!.GetValue<string>()) throw new InvalidOperationException("Hash de imagem divergente.");
            image.Remove("base64");
            var result = Species.Create(item.CommonName, item.ScientificName, item.Description);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Error.Message);
            result.Value.InitializeCatalog("embrapa-2023:" + item.Key, base64, image.ToJsonString());
            return (Item: item, Entity: result.Value);
        }).ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        // Importação administrativa sem HTTP: autoria do primeiro administrador ativo (criado por --bootstrap-admin).
        var actor = await db.Users.AsNoTracking().Where(x => x.IsActive && x.Profile == UserProfile.Administrator)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Nenhum administrador ativo; execute --bootstrap-admin antes da importação.");
        var at = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var speciesCreated = 0;
        var cultivarsCreated = 0;
        process.Step("ImportingCatalog", "Conferindo identidades editoriais e importando apenas cadastros ausentes");
        foreach (var (item, candidate) in prepared)
        {
            var existing = await db.Species.SingleOrDefaultAsync(x => x.CatalogKey == candidate.CatalogKey, cancellationToken);
            if (existing is null)
            {
                // Não adota silenciosamente cadastros manuais ou decide entre homônimos.
                if (await db.Species.AnyAsync(x => x.CatalogKey == null && (x.CommonName == item.CommonName || x.ScientificName == item.ScientificName), cancellationToken))
                    throw new InvalidOperationException($"Cadastro manual correspondente a {item.CommonName}; concilie antes de importar.");
                existing = candidate;
                db.Species.Add(existing);
                db.AuditEntries.Add(AuditSnapshots.Create(null, "Species", existing.Id, actor, at, null,
                    AuditSnapshots.Capture(new { existing.Id, existing.CommonName, existing.ScientificName, existing.Description, existing.CatalogKey, existing.IsActive, existing.Version, ImageSha256 = images[item.Key]["sha256"]!.GetValue<string>() }),
                    "Importação explícita do catálogo Embrapa 2023."));
                speciesCreated++;
            }
            foreach (var cultivar in item.Cultivars)
            {
                var key = $"embrapa-2023:page-{cultivar.SourcePage}";
                var persisted = await db.Cultivars.SingleOrDefaultAsync(x => x.CatalogKey == key, cancellationToken);
                if (persisted is not null)
                {
                    if (persisted.SpeciesId != existing.Id) throw new InvalidOperationException("Vínculo editorial incompatível.");
                    continue;
                }
                if (await db.Cultivars.AnyAsync(x => x.SpeciesId == existing.Id && x.Name == cultivar.Name, cancellationToken))
                    throw new InvalidOperationException($"Cultivar manual correspondente a {cultivar.Name}; concilie antes de importar.");
                var result = Cultivar.Create(existing.Id, cultivar.Name, $"Embrapa Hortaliças (1981–2023), ficha nas páginas {cultivar.SourcePage}–{cultivar.SourcePage + 1}. Imagem da espécie é apenas ilustrativa.");
                if (!result.IsSuccess) throw new InvalidOperationException(result.Error.Message);
                var entity = result.Value;
                entity.InitializeCatalog(key);
                db.Cultivars.Add(entity);
                db.AuditEntries.Add(AuditSnapshots.Create(null, "Cultivar", entity.Id, actor, at, null,
                    AuditSnapshots.Capture(new { entity.Id, entity.SpeciesId, entity.Name, entity.Description, entity.CatalogKey, entity.IsActive, entity.Version }),
                    "Importação explícita do catálogo Embrapa 2023."));
                cultivarsCreated++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("EmbrapaCatalogImported", "Succeeded", "Catálogo importado: {SpeciesCreated} espécies/grupos e {CultivarsCreated} cultivares novos", speciesCreated, cultivarsCreated);
        process.Complete(Result<Unit>.Success(new()));
        return new(speciesCreated, cultivarsCreated);
    }
}
