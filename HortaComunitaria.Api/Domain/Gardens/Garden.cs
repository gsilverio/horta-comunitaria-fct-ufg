using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Gardens;
/// <summary>Cadastro Garden com identidade estável, versão e inativação sem exclusão física.</summary>
public sealed class Garden
{
    private Garden() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo code do cadastro, preservado nas alterações.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Campo name do cadastro, preservado nas alterações.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo location do cadastro, preservado nas alterações.</summary>
    public string? Location { get; private set; }
    /// <summary>Campo institutionalAreaM2 do cadastro, preservado nas alterações.</summary>
    public decimal? InstitutionalAreaM2 { get; private set; }
    /// <summary>Campo operationalAreaM2 do cadastro, preservado nas alterações.</summary>
    public decimal? OperationalAreaM2 { get; private set; }
    /// <summary>Campo experimentalAreaM2 do cadastro, preservado nas alterações.</summary>
    public decimal? ExperimentalAreaM2 { get; private set; }
    /// <summary>Disponibilidade para novos registros.</summary>
    public bool IsActive { get; private set; }
    /// <summary>Versão esperada nas alterações.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última alteração efetiva em UTC.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Cria o cadastro com campos normalizados e opcionais desconhecidos preservados.</summary>
    public static Result<Garden> Create(string? code, string name, bool isActive = true, Guid? id = null, string? location = null, decimal? institutionalAreaM2 = null, decimal? operationalAreaM2 = null, decimal? experimentalAreaM2 = null)
    {
        var identity = id ?? Guid.CreateVersion7();
        code ??= identity.ToString("D");

        var error = RecordRules.Text(code, "code", 40, true) ??
            RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(location, "location", 1000, false) ??
            RecordRules.Number(institutionalAreaM2, "institutionalAreaM2", positive: true) ??
            RecordRules.Number(operationalAreaM2, "operationalAreaM2", positive: true) ??
            RecordRules.Number(experimentalAreaM2, "experimentalAreaM2", positive: true);
        if (error is not null) return Result<Garden>.Failure(error);
        if (id == Guid.Empty) return Result<Garden>.Failure(Error.Validation("id", "Identificador inválido."));
        return Result<Garden>.Success(new() { Id = identity, Code = code.Trim(), Name = name.Trim(), Location = RecordRules.Optional(location), InstitutionalAreaM2 = institutionalAreaM2, OperationalAreaM2 = operationalAreaM2, ExperimentalAreaM2 = experimentalAreaM2, IsActive = isActive });
    }
    /// <summary>Atualiza somente campos editáveis; correção idêntica mantém versão e instante.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string code, string name, string? location, decimal? institutionalAreaM2, decimal? operationalAreaM2, decimal? experimentalAreaM2)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(code, name, IsActive, Id, location, institutionalAreaM2, operationalAreaM2, experimentalAreaM2);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        var candidate = proposed.Value;
        if ((Code, Name, Location, InstitutionalAreaM2, OperationalAreaM2, ExperimentalAreaM2) == (candidate.Code, candidate.Name, candidate.Location, candidate.InstitutionalAreaM2, candidate.OperationalAreaM2, candidate.ExperimentalAreaM2)) return Result<bool>.Success(false);
        Code = candidate.Code;
        Name = candidate.Name;
        Location = candidate.Location;
        InstitutionalAreaM2 = candidate.InstitutionalAreaM2;
        OperationalAreaM2 = candidate.OperationalAreaM2;
        ExperimentalAreaM2 = candidate.ExperimentalAreaM2;
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
