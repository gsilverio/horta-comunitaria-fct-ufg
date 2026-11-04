using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.Costs;

namespace HortaComunitaria.Api.Domain.Occurrences;
/// <summary>Valores editáveis de Occurrence, criados somente após validação de invariantes locais.</summary>
public sealed record OccurrenceValues
{
    private OccurrenceValues() { }
    /// <summary>Campo openedAt validado e normalizado.</summary>
    public DateTimeOffset OpenedAt { get; private init; }
    /// <summary>Campo category validado e normalizado.</summary>
    public string Category { get; private init; } = "";
    /// <summary>Campo description validado e normalizado.</summary>
    public string Description { get; private init; } = "";
    /// <summary>Campo status validado e normalizado.</summary>
    public OccurrenceStatus Status { get; private init; }
    /// <summary>Campo resolvedAt validado e normalizado.</summary>
    public DateTimeOffset? ResolvedAt { get; private init; }
    /// <summary>Campo resolution validado e normalizado.</summary>
    public string? Resolution { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Campo bedId validado e normalizado.</summary>
    public Guid? BedId { get; private init; }
    /// <summary>Campo sectorId validado e normalizado.</summary>
    public Guid? SectorId { get; private init; }
    /// <summary>Campo cycleId validado e normalizado.</summary>
    public Guid? CycleId { get; private init; }
    /// <summary>Campo irrigationId validado e normalizado.</summary>
    public Guid? IrrigationId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<OccurrenceValues> Create(DateTimeOffset openedAt, string category, string description, OccurrenceStatus status, DateTimeOffset? resolvedAt, string? resolution, Guid? responsibleParticipantId, Guid? bedId, Guid? sectorId, Guid? cycleId, Guid? irrigationId)
    {
        var error = (openedAt == default ? Error.Validation("openedAt", "Instante obrigatório.") : null) ?? RecordRules.Text(category, "category", 120, true) ?? RecordRules.Text(description, "description", 1000, true) ?? (!Enum.IsDefined(status) ? Error.Validation("status", "Opção indefinida.") : null) ?? RecordRules.Text(resolution, "resolution", 1000, false) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null) ?? (bedId == Guid.Empty ? Error.Validation("bedId", "Identificador inválido.") : null) ?? (sectorId == Guid.Empty ? Error.Validation("sectorId", "Identificador inválido.") : null) ?? (cycleId == Guid.Empty ? Error.Validation("cycleId", "Identificador inválido.") : null) ?? (irrigationId == Guid.Empty ? Error.Validation("irrigationId", "Identificador inválido.") : null);
        if (error is not null) return Result<OccurrenceValues>.Failure(error);
        if (new Guid?[] { bedId, sectorId, cycleId, irrigationId }.Count(x => x is not null) > 1) return Result<OccurrenceValues>.Failure(Error.Validation("context", "Informe no máximo um contexto específico."));
        if (status == OccurrenceStatus.Resolved && (resolvedAt is null || string.IsNullOrWhiteSpace(resolution)) || status != OccurrenceStatus.Resolved && (resolvedAt is not null || !string.IsNullOrWhiteSpace(resolution)) || resolvedAt < openedAt) return Result<OccurrenceValues>.Failure(Error.Validation("resolution", "Resolução exige instante não anterior à abertura e descrição, somente no estado Resolved."));
        return Result<OccurrenceValues>.Success(new() { OpenedAt = openedAt.ToUniversalTime(), Category = category.Trim(), Description = description.Trim(), Status = status, ResolvedAt = resolvedAt?.ToUniversalTime(), Resolution = RecordRules.Optional(resolution), ResponsibleParticipantId = responsibleParticipantId, BedId = bedId, SectorId = sectorId, CycleId = cycleId, IrrigationId = irrigationId });
    }
}
