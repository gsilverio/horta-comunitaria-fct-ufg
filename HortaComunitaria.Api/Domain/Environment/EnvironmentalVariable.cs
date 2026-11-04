using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Environment;
/// <summary>Cadastro EnvironmentalVariable, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class EnvironmentalVariable
{
    private EnvironmentalVariable() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo code do cadastro.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Identifica as medições destinadas ao indicador de umidade do solo.</summary>
    public bool IsSoilMoisture { get; private set; }
    /// <summary>Campo name do cadastro.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo quantity do cadastro.</summary>
    public string Quantity { get; private set; } = "";
    /// <summary>Campo referenceUnit do cadastro.</summary>
    public string ReferenceUnit { get; private set; } = "";
    /// <summary>Campo description do cadastro.</summary>
    public string? Description { get; private set; }
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida o cadastro; sem código legado, usa a própria identidade UUID.</summary>
    public static Result<EnvironmentalVariable> Create(string? code, string name, string quantity, string referenceUnit, string? description, bool? isSoilMoisture = null)
    {
        var identity = Guid.CreateVersion7();
        code ??= identity.ToString("D");
        var error = RecordRules.Text(code, "code", 40, true) ??
            RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(quantity, "quantity", 120, true) ??
            RecordRules.Text(referenceUnit, "referenceUnit", 40, true) ??
            RecordRules.Text(description, "description", 1000, false);
        if (error is not null) return Result<EnvironmentalVariable>.Failure(error);

        return Result<EnvironmentalVariable>.Success(new() { Id = identity, Code = code.Trim(), Name = name.Trim(), Quantity = quantity.Trim(), ReferenceUnit = referenceUnit.Trim(), Description = RecordRules.Optional(description), IsSoilMoisture = isSoilMoisture ?? code.Trim() == "soil_moisture" });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string code, string name, string quantity, string referenceUnit, string? description, bool? isSoilMoisture = null)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(code, name, quantity, referenceUnit, description, isSoilMoisture ?? IsSoilMoisture);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (IsSoilMoisture == proposed.Value.IsSoilMoisture && Code == proposed.Value.Code && Name == proposed.Value.Name && Quantity == proposed.Value.Quantity && ReferenceUnit == proposed.Value.ReferenceUnit && Description == proposed.Value.Description) return Result<bool>.Success(false);
        IsSoilMoisture = proposed.Value.IsSoilMoisture;
        Code = proposed.Value.Code;
        Name = proposed.Value.Name;
        Quantity = proposed.Value.Quantity;
        ReferenceUnit = proposed.Value.ReferenceUnit;
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
