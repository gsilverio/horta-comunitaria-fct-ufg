using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Cadastro PropagationLot, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class PropagationLot
{
    private PropagationLot() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo gardenId do cadastro.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Campo code do cadastro.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Campo speciesId do cadastro.</summary>
    public Guid SpeciesId { get; private set; }
    /// <summary>Campo cultivarId do cadastro.</summary>
    public Guid? CultivarId { get; private set; }
    /// <summary>Campo materialType do cadastro.</summary>
    public MaterialType MaterialType { get; private set; }
    /// <summary>Campo origin do cadastro.</summary>
    public string? Origin { get; private set; }
    /// <summary>Campo receivedOn do cadastro.</summary>
    public DateOnly? ReceivedOn { get; private set; }
    /// <summary>Campo notes do cadastro.</summary>
    public string? Notes { get; private set; }
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida o cadastro; sem código legado, usa a própria identidade UUID.</summary>
    public static Result<PropagationLot> Create(Guid gardenId, string? code, Guid speciesId, Guid? cultivarId, MaterialType materialType, string? origin, DateOnly? receivedOn, string? notes)
    {
        var identity = Guid.CreateVersion7();
        code ??= identity.ToString("D");
        var error = (gardenId == Guid.Empty ? Error.Validation("gardenId", "Identificador inválido.") : null) ??
            RecordRules.Text(code, "code", 40, true) ??
            (speciesId == Guid.Empty ? Error.Validation("speciesId", "Identificador inválido.") : null) ??
            (cultivarId == Guid.Empty ? Error.Validation("cultivarId", "Identificador inválido.") : null) ??
            (!Enum.IsDefined(materialType) ? Error.Validation("materialType", "Opção indefinida.") : null) ??
            RecordRules.Text(origin, "origin", 1000, false) ??
            null ??
            RecordRules.Text(notes, "notes", 1000, false);
        if (error is not null) return Result<PropagationLot>.Failure(error);

        return Result<PropagationLot>.Success(new() { Id = identity, GardenId = gardenId, Code = code.Trim(), SpeciesId = speciesId, CultivarId = cultivarId, MaterialType = materialType, Origin = RecordRules.Optional(origin), ReceivedOn = receivedOn, Notes = RecordRules.Optional(notes) });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string code, Guid speciesId, Guid? cultivarId, MaterialType materialType, string? origin, DateOnly? receivedOn, string? notes)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(GardenId, code, speciesId, cultivarId, materialType, origin, receivedOn, notes);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (Code == proposed.Value.Code && SpeciesId == proposed.Value.SpeciesId && CultivarId == proposed.Value.CultivarId && MaterialType == proposed.Value.MaterialType && Origin == proposed.Value.Origin && ReceivedOn == proposed.Value.ReceivedOn && Notes == proposed.Value.Notes) return Result<bool>.Success(false);
        Code = proposed.Value.Code;
        SpeciesId = proposed.Value.SpeciesId;
        CultivarId = proposed.Value.CultivarId;
        MaterialType = proposed.Value.MaterialType;
        Origin = proposed.Value.Origin;
        ReceivedOn = proposed.Value.ReceivedOn;
        Notes = proposed.Value.Notes;
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
