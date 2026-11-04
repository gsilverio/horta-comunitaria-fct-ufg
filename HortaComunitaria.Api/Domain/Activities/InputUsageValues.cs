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
/// <summary>Valores imutáveis da associação InputUsage.</summary>
public sealed record InputUsageValues
{
    private InputUsageValues() { }
    /// <summary>Campo inputId validado.</summary>
    public Guid InputId { get; private init; }
    /// <summary>Campo quantity validado.</summary>
    public decimal Quantity { get; private init; }
    /// <summary>Campo unit validado.</summary>
    public string Unit { get; private init; } = "";
    /// <summary>Valida referências, textos e grandezas antes de construir a associação.</summary>
    public static Result<InputUsageValues> Create(Guid inputId, decimal quantity, string unit)
    {
        var error = (inputId == Guid.Empty ? Error.Validation("inputId", "Identificador inválido.") : null) ?? RecordRules.Number(quantity, "quantity", 3) ?? RecordRules.Text(unit, "unit", 40, true);
        if (error is not null) return Result<InputUsageValues>.Failure(error);
        if (quantity <= 0) return Result<InputUsageValues>.Failure(Error.Validation("quantity", "Quantidade usada deve ser positiva."));
        return Result<InputUsageValues>.Success(new() { InputId = inputId, Quantity = quantity, Unit = unit.Trim() });
    }
}
