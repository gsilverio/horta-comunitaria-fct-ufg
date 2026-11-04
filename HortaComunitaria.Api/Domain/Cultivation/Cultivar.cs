using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Cadastro Cultivar, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class Cultivar
{
    private Cultivar() { }
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
    /// <summary>Campo speciesId do cadastro.</summary>
    public Guid SpeciesId { get; private set; }
    /// <summary>Campo name do cadastro.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo description do cadastro.</summary>
    public string? Description { get; private set; }
    /// <summary>Identidade editorial estável que preserva a idempotência após renomeações.</summary>
    public string? CatalogKey { get; private set; }

    /// <summary>Associa um cadastro novo à ficha da publicação.</summary>
    internal void InitializeCatalog(string key)
    {
        if (CatalogKey is not null || string.IsNullOrWhiteSpace(key) || key.Length > 80)
            throw new InvalidOperationException("Identidade de catálogo inválida.");
        CatalogKey = key;
    }
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida e normaliza o cadastro.</summary>
    public static Result<Cultivar> Create(Guid speciesId, string name, string? description)
    {
        var error = (speciesId == Guid.Empty ? Error.Validation("speciesId", "Identificador inválido.") : null) ??
            RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(description, "description", 1000, false);
        if (error is not null) return Result<Cultivar>.Failure(error);

        return Result<Cultivar>.Success(new() { Id = Guid.CreateVersion7(), SpeciesId = speciesId, Name = name.Trim(), Description = RecordRules.Optional(description) });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string name, string? description)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(SpeciesId, name, description);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (Name == proposed.Value.Name && Description == proposed.Value.Description) return Result<bool>.Success(false);
        Name = proposed.Value.Name;
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
