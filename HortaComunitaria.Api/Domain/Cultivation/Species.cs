using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Cadastro Species, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class Species
{
    private Species() { }
    /// <summary>Dias após cada plantio para a ilustração intermediária; opcional.</summary>
    public int? VisualGrowthDays { get; private set; }
    /// <summary>Dias após cada plantio para a ilustração de colheita prevista; opcional.</summary>
    public int? VisualHarvestDays { get; private set; }

    /// <summary>Configura somente a estimativa visual, com concorrência e revisão explícita.</summary>
    public Result<bool> ConfigureVisualGrowth(int expectedVersion, ChangeReason reason, DateTimeOffset at, int? growthDays, int? harvestDays)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive) ?? VisualGrowthTiming.Validate(growthDays, harvestDays);
        if (error is not null) return Result<bool>.Failure(error);
        if (VisualGrowthDays == growthDays && VisualHarvestDays == harvestDays) return Result<bool>.Success(false);
        VisualGrowthDays = growthDays;
        VisualHarvestDays = harvestDays;
        Version++; UpdatedAt = at.ToUniversalTime();
        return Result<bool>.Success(true);
    }

    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo commonName do cadastro.</summary>
    public string CommonName { get; private set; } = "";
    /// <summary>Campo scientificName do cadastro.</summary>
    public string? ScientificName { get; private set; }
    /// <summary>Campo description do cadastro.</summary>
    public string? Description { get; private set; }
    /// <summary>Chave estável da carga editorial; não muda em edições do cadastro.</summary>
    public string? CatalogKey { get; private set; }
    /// <summary>Imagem ilustrativa em Base64; carregada somente pela consulta específica.</summary>
    public string? ImageBase64 { get; private set; }
    /// <summary>Metadados de licença e procedência em JSON, sem conteúdo executável.</summary>
    public string? ImageMetadataJson { get; private set; }

    /// <summary>Prepara um novo cadastro da carga revisada, antes de persistir.</summary>
    internal void InitializeCatalog(string key, string imageBase64, string metadataJson)
    {
        if (CatalogKey is not null || string.IsNullOrWhiteSpace(key) || key.Length > 80)
            throw new InvalidOperationException("Identidade de catálogo inválida.");
        var bytes = Convert.FromBase64String(imageBase64);
        if (bytes.Length is 0 or > 180000 || bytes.Length < 3 || bytes[0] != 0xff || bytes[1] != 0xd8 || bytes[2] != 0xff)
            throw new InvalidOperationException("A imagem do catálogo deve ser JPEG de até 180 KB.");
        CatalogKey = key;
        ImageBase64 = imageBase64;
        ImageMetadataJson = metadataJson;
    }
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida e normaliza o cadastro.</summary>
    public static Result<Species> Create(string commonName, string? scientificName, string? description)
    {
        var error = RecordRules.Text(commonName, "commonName", 120, true) ??
            RecordRules.Text(scientificName, "scientificName", 120, false) ??
            RecordRules.Text(description, "description", 1000, false);
        if (error is not null) return Result<Species>.Failure(error);

        return Result<Species>.Success(new() { Id = Guid.CreateVersion7(), CommonName = commonName.Trim(), ScientificName = RecordRules.Optional(scientificName), Description = RecordRules.Optional(description) });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string commonName, string? scientificName, string? description)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(commonName, scientificName, description);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (CommonName == proposed.Value.CommonName && ScientificName == proposed.Value.ScientificName && Description == proposed.Value.Description) return Result<bool>.Success(false);
        CommonName = proposed.Value.CommonName;
        ScientificName = proposed.Value.ScientificName;
        Description = proposed.Value.Description;
        Version++; UpdatedAt = at.ToUniversalTime();
        return Result<bool>.Success(true);
    }
    /// <summary>Preserva o cadastro e impede novas seleções.</summary>
    public Result<Unit> Inactivate(int expectedVersion, ChangeReason reason, DateTimeOffset at)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<Unit>.Failure(error);
        IsActive = false; Version++; UpdatedAt = at.ToUniversalTime();
        return Result<Unit>.Success(new());
    }
}
