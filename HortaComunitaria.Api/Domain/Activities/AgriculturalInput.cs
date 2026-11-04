using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Activities;
/// <summary>Cadastro AgriculturalInput, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class AgriculturalInput
{
    private AgriculturalInput() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo name do cadastro.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo category do cadastro.</summary>
    public string? Category { get; private set; }
    /// <summary>Campo referenceUnit do cadastro.</summary>
    public string ReferenceUnit { get; private set; } = "";
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida e normaliza o cadastro.</summary>
    public static Result<AgriculturalInput> Create(string name, string? category, string referenceUnit)
    {
        var error = RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(category, "category", 120, false) ??
            RecordRules.Text(referenceUnit, "referenceUnit", 40, true);
        if (error is not null) return Result<AgriculturalInput>.Failure(error);

        return Result<AgriculturalInput>.Success(new() { Id = Guid.CreateVersion7(), Name = name.Trim(), Category = RecordRules.Optional(category), ReferenceUnit = referenceUnit.Trim() });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string name, string? category, string referenceUnit)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(name, category, referenceUnit);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (Name == proposed.Value.Name && Category == proposed.Value.Category && ReferenceUnit == proposed.Value.ReferenceUnit) return Result<bool>.Success(false);
        Name = proposed.Value.Name;
        Category = proposed.Value.Category;
        ReferenceUnit = proposed.Value.ReferenceUnit;
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
