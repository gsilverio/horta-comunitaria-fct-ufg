using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Activities;
/// <summary>Cadastro Participant com identidade estável, versão e inativação sem exclusão física.</summary>
public sealed class Participant
{
    private Participant() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta proprietária do cadastro.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Campo displayName do cadastro, preservado nas alterações.</summary>
    public string DisplayName { get; private set; } = "";
    /// <summary>Campo category do cadastro, preservado nas alterações.</summary>
    public string? Category { get; private set; }
    /// <summary>Disponibilidade para novos registros.</summary>
    public bool IsActive { get; private set; }
    /// <summary>Versão esperada nas alterações.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última alteração efetiva em UTC.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Cria o cadastro com campos normalizados e opcionais desconhecidos preservados.</summary>
    public static Result<Participant> Create(Guid gardenId, string displayName, bool isActive = true, Guid? id = null, string? category = null)
    {
        if (gardenId == Guid.Empty) return Result<Participant>.Failure(Error.Validation("gardenId", "Horta obrigatória."));
        var error = RecordRules.Text(displayName, "displayName", 120, true) ??
            RecordRules.Text(category, "category", 120, false);
        if (error is not null) return Result<Participant>.Failure(error);
        if (id == Guid.Empty) return Result<Participant>.Failure(Error.Validation("id", "Identificador inválido."));
        return Result<Participant>.Success(new() { Id = id ?? Guid.CreateVersion7(), GardenId = gardenId, DisplayName = displayName.Trim(), Category = RecordRules.Optional(category), IsActive = isActive });
    }
    /// <summary>Atualiza somente campos editáveis; correção idêntica mantém versão e instante.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string displayName, string? category)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(GardenId, displayName, IsActive, Id, category);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        var candidate = proposed.Value;
        if ((DisplayName, Category) == (candidate.DisplayName, candidate.Category)) return Result<bool>.Success(false);
        DisplayName = candidate.DisplayName;
        Category = candidate.Category;
        Version++; UpdatedAt = at.ToUniversalTime();
        return Result<bool>.Success(true);
    }
    /// <summary>Impede novas seleções sem apagar referências anteriores.</summary>
    public Result<bool> Inactivate(int expectedVersion, ChangeReason reason, DateTimeOffset at)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        IsActive = false; Version++; UpdatedAt = at.ToUniversalTime();
        return Result<bool>.Success(true);
    }

}
