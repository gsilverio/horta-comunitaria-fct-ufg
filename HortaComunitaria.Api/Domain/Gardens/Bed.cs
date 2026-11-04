using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Gardens;
/// <summary>Cadastro Bed com identidade estável, versão e inativação sem exclusão física.</summary>
public sealed class Bed
{
    private Bed() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta proprietária do cadastro.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Campo code do cadastro, preservado nas alterações.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Campo name do cadastro, preservado nas alterações.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo location do cadastro, preservado nas alterações.</summary>
    public string? Location { get; private set; }
    /// <summary>Disponibilidade para novos registros.</summary>
    public bool IsActive { get; private set; }
    /// <summary>Versão esperada nas alterações.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última alteração efetiva em UTC.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Cria o cadastro com campos normalizados e opcionais desconhecidos preservados.</summary>
    public static Result<Bed> Create(Guid gardenId, string? code, string name, bool isActive = true, Guid? id = null, string? location = null)
    {
        var identity = id ?? Guid.CreateVersion7();
        code ??= identity.ToString("D");
        if (gardenId == Guid.Empty) return Result<Bed>.Failure(Error.Validation("gardenId", "Horta obrigatória."));
        var error = RecordRules.Text(code, "code", 40, true) ??
            RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(location, "location", 1000, false);
        if (error is not null) return Result<Bed>.Failure(error);
        if (id == Guid.Empty) return Result<Bed>.Failure(Error.Validation("id", "Identificador inválido."));
        return Result<Bed>.Success(new() { Id = identity, GardenId = gardenId, Code = code.Trim(), Name = name.Trim(), Location = RecordRules.Optional(location), IsActive = isActive });
    }
    /// <summary>Atualiza somente campos editáveis; correção idêntica mantém versão e instante.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string code, string name, string? location)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(GardenId, code, name, IsActive, Id, location);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        var candidate = proposed.Value;
        if ((Code, Name, Location) == (candidate.Code, candidate.Name, candidate.Location)) return Result<bool>.Success(false);
        Code = candidate.Code;
        Name = candidate.Name;
        Location = candidate.Location;
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

    /// <summary>Registra uma mudança de vigência como nova versão do canteiro.</summary>
    public Result<Unit> ChangeContext(int expectedVersion, ChangeReason reason, DateTimeOffset at)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<Unit>.Failure(error);
        Version++; UpdatedAt = at.ToUniversalTime();
        return Result<Unit>.Success(new());
    }

}
