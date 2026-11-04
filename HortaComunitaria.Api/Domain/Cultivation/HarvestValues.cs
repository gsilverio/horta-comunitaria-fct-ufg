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

namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Valores editáveis de Harvest, criados somente após validação de invariantes locais.</summary>
public sealed record HarvestValues
{
    private HarvestValues() { }
    /// <summary>Campo cycleId validado e normalizado.</summary>
    public Guid CycleId { get; private init; }
    /// <summary>Campo harvestedOn validado e normalizado.</summary>
    public DateOnly HarvestedOn { get; private init; }
    /// <summary>Campo freshMassKg validado e normalizado.</summary>
    public decimal FreshMassKg { get; private init; }
    /// <summary>Campo usableMassKg validado e normalizado.</summary>
    public decimal? UsableMassKg { get; private init; }
    /// <summary>Campo lossReason validado e normalizado.</summary>
    public string? LossReason { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<HarvestValues> Create(Guid cycleId, DateOnly harvestedOn, decimal freshMassKg, decimal? usableMassKg, string? lossReason, Guid? responsibleParticipantId)
    {
        var error = (cycleId == Guid.Empty ? Error.Validation("cycleId", "Identificador inválido.") : null) ?? (harvestedOn == default ? Error.Validation("harvestedOn", "Data obrigatória.") : null) ?? RecordRules.Number(freshMassKg, "freshMassKg", 3) ?? RecordRules.Number(usableMassKg, "usableMassKg", 3) ?? RecordRules.Text(lossReason, "lossReason", 1000, false) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null);
        if (error is not null) return Result<HarvestValues>.Failure(error);
        if (usableMassKg > freshMassKg) return Result<HarvestValues>.Failure(Error.Validation("usableMassKg", "Massa aproveitável não supera a fresca."));
        return Result<HarvestValues>.Success(new() { CycleId = cycleId, HarvestedOn = harvestedOn, FreshMassKg = freshMassKg, UsableMassKg = usableMassKg, LossReason = RecordRules.Optional(lossReason), ResponsibleParticipantId = responsibleParticipantId });
    }
}
