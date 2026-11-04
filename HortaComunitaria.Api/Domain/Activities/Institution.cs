using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Activities;
/// <summary>Cadastro Institution, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class Institution
{
    private Institution() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo name do cadastro.</summary>
    public string Name { get; private set; } = "";
    /// <summary>Campo acronym do cadastro.</summary>
    public string? Acronym { get; private set; }
    /// <summary>Campo type do cadastro.</summary>
    public string? Type { get; private set; }
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida e normaliza o cadastro.</summary>
    public static Result<Institution> Create(string name, string? acronym, string? type)
    {
        var error = RecordRules.Text(name, "name", 120, true) ??
            RecordRules.Text(acronym, "acronym", 40, false) ??
            RecordRules.Text(type, "type", 120, false);
        if (error is not null) return Result<Institution>.Failure(error);

        return Result<Institution>.Success(new() { Id = Guid.CreateVersion7(), Name = name.Trim(), Acronym = RecordRules.Optional(acronym), Type = RecordRules.Optional(type) });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string name, string? acronym, string? type)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(name, acronym, type);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (Name == proposed.Value.Name && Acronym == proposed.Value.Acronym && Type == proposed.Value.Type) return Result<bool>.Success(false);
        Name = proposed.Value.Name;
        Acronym = proposed.Value.Acronym;
        Type = proposed.Value.Type;
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
