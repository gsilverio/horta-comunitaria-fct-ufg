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

namespace HortaComunitaria.Api.Domain.Costs;
/// <summary>Valores editáveis de FinancialEntry, criados somente após validação de invariantes locais.</summary>
public sealed record FinancialEntryValues
{
    private FinancialEntryValues() { }
    /// <summary>Campo entryDate validado e normalizado.</summary>
    public DateOnly EntryDate { get; private init; }
    /// <summary>Campo description validado e normalizado.</summary>
    public string Description { get; private init; } = "";
    /// <summary>Campo category validado e normalizado.</summary>
    public string Category { get; private init; } = "";
    /// <summary>Campo amountBrl validado e normalizado.</summary>
    public decimal AmountBrl { get; private init; }
    /// <summary>Campo nature validado e normalizado.</summary>
    public FinancialNature Nature { get; private init; }
    /// <summary>Campo bedId validado e normalizado.</summary>
    public Guid? BedId { get; private init; }
    /// <summary>Campo cycleId validado e normalizado.</summary>
    public Guid? CycleId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<FinancialEntryValues> Create(DateOnly entryDate, string description, string category, decimal amountBrl, FinancialNature nature, Guid? bedId, Guid? cycleId)
    {
        var error = (entryDate == default ? Error.Validation("entryDate", "Data obrigatória.") : null) ?? RecordRules.Text(description, "description", 1000, true) ?? RecordRules.Text(category, "category", 120, true) ?? RecordRules.Number(amountBrl, "amountBrl", 2) ?? (!Enum.IsDefined(nature) ? Error.Validation("nature", "Opção indefinida.") : null) ?? (bedId == Guid.Empty ? Error.Validation("bedId", "Identificador inválido.") : null) ?? (cycleId == Guid.Empty ? Error.Validation("cycleId", "Identificador inválido.") : null);
        if (error is not null) return Result<FinancialEntryValues>.Failure(error);
        if (bedId is not null && cycleId is not null) return Result<FinancialEntryValues>.Failure(Error.Validation("context", "Informe no máximo um canteiro ou ciclo."));
        return Result<FinancialEntryValues>.Success(new() { EntryDate = entryDate, Description = description.Trim(), Category = category.Trim(), AmountBrl = amountBrl, Nature = nature, BedId = bedId, CycleId = cycleId });
    }
}
