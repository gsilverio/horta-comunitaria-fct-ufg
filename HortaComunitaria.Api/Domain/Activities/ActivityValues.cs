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

namespace HortaComunitaria.Api.Domain.Activities;
/// <summary>Valores editáveis de Activity, criados somente após validação de invariantes locais.</summary>
public sealed record ActivityValues
{
    private ActivityValues() { }
    /// <summary>Campo title validado e normalizado.</summary>
    public string Title { get; private init; } = "";
    /// <summary>Campo category validado e normalizado.</summary>
    public string Category { get; private init; } = "";
    /// <summary>Campo performedOn validado e normalizado.</summary>
    public DateOnly PerformedOn { get; private init; }
    /// <summary>Campo description validado e normalizado.</summary>
    public string? Description { get; private init; }
    /// <summary>Campo bedId validado e normalizado.</summary>
    public Guid? BedId { get; private init; }
    /// <summary>Campo sectorId validado e normalizado.</summary>
    public Guid? SectorId { get; private init; }
    /// <summary>Campo cycleId validado e normalizado.</summary>
    public Guid? CycleId { get; private init; }
    /// <summary>Campo occurrenceId validado e normalizado.</summary>
    public Guid? OccurrenceId { get; private init; }
    /// <summary>Campo isOperational validado e normalizado.</summary>
    public bool IsOperational { get; private init; }
    /// <summary>Campo isSocial validado e normalizado.</summary>
    public bool IsSocial { get; private init; }
    /// <summary>Campo isEducational validado e normalizado.</summary>
    public bool IsEducational { get; private init; }
    /// <summary>Campo objective validado e normalizado.</summary>
    public string? Objective { get; private init; }
    /// <summary>Campo modality validado e normalizado.</summary>
    public string? Modality { get; private init; }
    /// <summary>Campo reportedAudienceTotal validado e normalizado.</summary>
    public int? ReportedAudienceTotal { get; private init; }
    /// <summary>Campo audienceListComplete validado e normalizado.</summary>
    public bool? AudienceListComplete { get; private init; }
    /// <summary>Campo participants validado e normalizado.</summary>
    public IReadOnlyList<ActivityParticipantValues> Participants { get; private init; } = Array.Empty<ActivityParticipantValues>();
    /// <summary>Campo inputUsages validado e normalizado.</summary>
    public IReadOnlyList<InputUsageValues> InputUsages { get; private init; } = Array.Empty<InputUsageValues>();
    /// <summary>Campo institutions validado e normalizado.</summary>
    public IReadOnlyList<ActivityInstitutionValues> Institutions { get; private init; } = Array.Empty<ActivityInstitutionValues>();
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<ActivityValues> Create(string title, string category, DateOnly performedOn, string? description, Guid? bedId, Guid? sectorId, Guid? cycleId, Guid? occurrenceId, bool isOperational, bool isSocial, bool isEducational, string? objective, string? modality, int? reportedAudienceTotal, bool? audienceListComplete, IReadOnlyList<ActivityParticipantValues> participants, IReadOnlyList<InputUsageValues> inputUsages, IReadOnlyList<ActivityInstitutionValues> institutions)
    {
        var error = RecordRules.Text(title, "title", 120, true) ?? RecordRules.Text(category, "category", 120, true) ?? (performedOn == default ? Error.Validation("performedOn", "Data obrigatória.") : null) ?? RecordRules.Text(description, "description", 1000, false) ?? (bedId == Guid.Empty ? Error.Validation("bedId", "Identificador inválido.") : null) ?? (sectorId == Guid.Empty ? Error.Validation("sectorId", "Identificador inválido.") : null) ?? (cycleId == Guid.Empty ? Error.Validation("cycleId", "Identificador inválido.") : null) ?? (occurrenceId == Guid.Empty ? Error.Validation("occurrenceId", "Identificador inválido.") : null) ?? RecordRules.Text(objective, "objective", 1000, false) ?? RecordRules.Text(modality, "modality", 120, false);
        if (error is not null) return Result<ActivityValues>.Failure(error);
        if (new Guid?[] { bedId, sectorId, cycleId }.Count(x => x is not null) > 1 || !isOperational && !isSocial && !isEducational) return Result<ActivityValues>.Failure(Error.Validation("context", "Informe no máximo um contexto e ao menos uma dimensão."));
        if (participants is null || inputUsages is null || institutions is null || participants.Select(x => x.ParticipantId).Distinct().Count() != participants.Count || institutions.Select(x => x.InstitutionId).Distinct().Count() != institutions.Count) return Result<ActivityValues>.Failure(Error.Validation("participants", "Listas obrigatórias, sem participantes ou instituições repetidos."));
        var nominal = participants.Count(x => x.CountsAsAudience);
        if (reportedAudienceTotal < 0 || reportedAudienceTotal < nominal || audienceListComplete == true && reportedAudienceTotal is not null && reportedAudienceTotal != nominal) return Result<ActivityValues>.Failure(Error.Validation("reportedAudienceTotal", "Total deve incluir o público nominal e conferir com uma lista declarada completa."));
        return Result<ActivityValues>.Success(new() { Title = title.Trim(), Category = category.Trim(), PerformedOn = performedOn, Description = RecordRules.Optional(description), BedId = bedId, SectorId = sectorId, CycleId = cycleId, OccurrenceId = occurrenceId, IsOperational = isOperational, IsSocial = isSocial, IsEducational = isEducational, Objective = RecordRules.Optional(objective), Modality = RecordRules.Optional(modality), ReportedAudienceTotal = reportedAudienceTotal, AudienceListComplete = audienceListComplete, Participants = Array.AsReadOnly(participants.OrderBy(x => x.ParticipantId).ToArray()), InputUsages = Array.AsReadOnly(inputUsages.OrderBy(x => x.InputId).ThenBy(x => x.Unit).ThenBy(x => x.Quantity).ToArray()), Institutions = Array.AsReadOnly(institutions.OrderBy(x => x.InstitutionId).ToArray()) });
    }
}
