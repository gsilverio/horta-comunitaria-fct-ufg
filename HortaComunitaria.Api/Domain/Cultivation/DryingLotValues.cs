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
/// <summary>Valores editáveis de DryingLot, criados somente após validação de invariantes locais.</summary>
public sealed record DryingLotValues
{
    private DryingLotValues() { }
    /// <summary>Campo code validado e normalizado.</summary>
    public string Code { get; private init; } = "";
    /// <summary>Campo method validado e normalizado.</summary>
    public string? Method { get; private init; }
    /// <summary>Campo startedOn validado e normalizado.</summary>
    public DateOnly StartedOn { get; private init; }
    /// <summary>Campo endedOn validado e normalizado.</summary>
    public DateOnly? EndedOn { get; private init; }
    /// <summary>Campo totalDryMassKg validado e normalizado.</summary>
    public decimal? TotalDryMassKg { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Campo items validado e normalizado.</summary>
    public IReadOnlyList<DryingItemValues> Items { get; private init; } = Array.Empty<DryingItemValues>();
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<DryingLotValues> Create(string code, string? method, DateOnly startedOn, DateOnly? endedOn, decimal? totalDryMassKg, Guid? responsibleParticipantId, IReadOnlyList<DryingItemValues> items)
    {
        var error = RecordRules.Text(code, "code", 40, true) ?? RecordRules.Text(method, "method", 200, false) ?? (startedOn == default ? Error.Validation("startedOn", "Data obrigatória.") : null) ?? RecordRules.Number(totalDryMassKg, "totalDryMassKg", 3) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null);
        if (error is not null) return Result<DryingLotValues>.Failure(error);
        if (endedOn < startedOn || (endedOn is null) != (totalDryMassKg is null)) return Result<DryingLotValues>.Failure(Error.Validation("completion", "Fim e massa seca conhecidos devem ser informados juntos, com fim não anterior ao início."));
        if (items is null || items.Select(x => x.HarvestId).Distinct().Count() != items.Count) return Result<DryingLotValues>.Failure(Error.Validation("items", "Itens exigem colheitas únicas e massas positivas."));
        return Result<DryingLotValues>.Success(new() { Code = code.Trim(), Method = RecordRules.Optional(method), StartedOn = startedOn, EndedOn = endedOn, TotalDryMassKg = totalDryMassKg, ResponsibleParticipantId = responsibleParticipantId, Items = Array.AsReadOnly(items.OrderBy(x => x.HarvestId).ToArray()) });
    }
}
