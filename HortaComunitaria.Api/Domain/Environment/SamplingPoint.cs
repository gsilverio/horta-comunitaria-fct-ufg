using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Environment;
/// <summary>Cadastro SamplingPoint, com campos opcionais preservados, versão e inativação rastreável.</summary>
public sealed class SamplingPoint
{
    private SamplingPoint() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Campo gardenId do cadastro.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Campo code do cadastro.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Campo description do cadastro.</summary>
    public string Description { get; private set; } = "";
    /// <summary>Campo location do cadastro.</summary>
    public string? Location { get; private set; }
    /// <summary>Campo bedId do cadastro.</summary>
    public Guid? BedId { get; private set; }
    /// <summary>Campo sectorId do cadastro.</summary>
    public Guid? SectorId { get; private set; }
    /// <summary>Disponibilidade para novos vínculos.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Versão esperada na edição.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Última mudança efetiva.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Valida o cadastro; sem código legado, usa a própria identidade UUID.</summary>
    public static Result<SamplingPoint> Create(Guid gardenId, string? code, string description, string? location, Guid? bedId, Guid? sectorId)
    {
        var identity = Guid.CreateVersion7();
        code ??= identity.ToString("D");
        var error = (gardenId == Guid.Empty ? Error.Validation("gardenId", "Identificador inválido.") : null) ??
            RecordRules.Text(code, "code", 40, true) ??
            RecordRules.Text(description, "description", 1000, true) ??
            RecordRules.Text(location, "location", 1000, false) ??
            (bedId == Guid.Empty ? Error.Validation("bedId", "Identificador inválido.") : null) ??
            (sectorId == Guid.Empty ? Error.Validation("sectorId", "Identificador inválido.") : null);
        if (error is not null) return Result<SamplingPoint>.Failure(error);
        if (bedId is not null && sectorId is not null) return Result<SamplingPoint>.Failure(Error.Validation("context", "Ponto admite no máximo um canteiro ou setor."));
        return Result<SamplingPoint>.Success(new() { Id = identity, GardenId = gardenId, Code = code.Trim(), Description = description.Trim(), Location = RecordRules.Optional(location), BedId = bedId, SectorId = sectorId });
    }
    /// <summary>Substitui campos editáveis sem mudar identidade ou proprietário.</summary>
    public Result<bool> Update(int expectedVersion, ChangeReason reason, DateTimeOffset at, string code, string description, string? location, Guid? bedId, Guid? sectorId)
    {
        var error = RecordRules.Change(expectedVersion, Version, IsActive);
        if (error is not null) return Result<bool>.Failure(error);
        var proposed = Create(GardenId, code, description, location, bedId, sectorId);
        if (!proposed.IsSuccess) return Result<bool>.Failure(proposed.Error);
        if (Code == proposed.Value.Code && Description == proposed.Value.Description && Location == proposed.Value.Location && BedId == proposed.Value.BedId && SectorId == proposed.Value.SectorId) return Result<bool>.Success(false);
        Code = proposed.Value.Code;
        Description = proposed.Value.Description;
        Location = proposed.Value.Location;
        BedId = proposed.Value.BedId;
        SectorId = proposed.Value.SectorId;
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
