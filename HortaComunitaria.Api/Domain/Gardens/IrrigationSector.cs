using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Gardens;
/// <summary>Cadastro IrrigationSector com identidade estável, versão e inativação sem exclusão física.</summary>
public sealed class IrrigationSector
{
    private IrrigationSector() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta proprietária do cadastro.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Campo code do cadastro, preservado nas alterações.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Campo name do cadastro, preservado nas alterações.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo description do cadastro, preservado nas alterações.</summary>
    public string? Description { get; private set; }
    /// <summary>Disponibilidade para novos registros.</summary>
    public bool IsActive { get; private set; }
    /// <summary>Versão esperada nas alterações.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última alteração efetiva em UTC.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Cria o cadastro com campos normalizados e opcionais desconhecidos preservados.</summary>
    public static Result<IrrigationSector> Create(Guid gardenId, string? code, string name, bool isActive = true, Guid? id = null, string? description = null)
    {
        var identity = id ?? Guid.CreateVersion7();
        code ??= identity.ToString("D");
        if (gardenId == Guid.Empty) return Result<IrrigationSector>.Failure(Error.Validation("gardenId", "Horta obrigatória."));
        var error = RecordRules.Text(code, "code", 40, true) ??
            RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(description, "description", 1000, false);
        if (error is not null) return Result<IrrigationSector>.Failure(error);
        if (id == Guid.Empty) return Result<IrrigationSector>.Failure(Error.Validation("id", "Identificador inválido."));
        return Result<IrrigationSector>.Success(new() { Id = identity, GardenId = gardenId, Code = code.Trim(), Name = name.Trim(), Description = RecordRules.Optional(description), IsActive = isActive });
    }
    /// <summary>Atualiza somente campos editáveis; correção idêntica mantém versão e instante.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string code, string name, string? description)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(GardenId, code, name, IsActive, Id, description);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        var candidate = proposed.Value;
        if ((Code, Name, Description) == (candidate.Code, candidate.Name, candidate.Description)) return Result<bool>.Success(false);
        Code = candidate.Code;
        Name = candidate.Name;
        Description = candidate.Description;
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
