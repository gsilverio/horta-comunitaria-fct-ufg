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
/// <summary>Valores imutáveis da associação DryingItem.</summary>
public sealed record DryingItemValues
{
    private DryingItemValues() { }
    /// <summary>Campo harvestId validado.</summary>
    public Guid HarvestId { get; private init; }
    /// <summary>Campo allocatedMassKg validado.</summary>
    public decimal AllocatedMassKg { get; private init; }
    /// <summary>Valida referências, textos e grandezas antes de construir a associação.</summary>
    public static Result<DryingItemValues> Create(Guid harvestId, decimal allocatedMassKg)
    {
        var error = (harvestId == Guid.Empty ? Error.Validation("harvestId", "Identificador inválido.") : null) ?? RecordRules.Number(allocatedMassKg, "allocatedMassKg", 3);
        if (error is not null) return Result<DryingItemValues>.Failure(error);
        if (allocatedMassKg <= 0) return Result<DryingItemValues>.Failure(Error.Validation("allocatedMassKg", "Massa destinada deve ser positiva."));
        return Result<DryingItemValues>.Success(new() { HarvestId = harvestId, AllocatedMassKg = allocatedMassKg });
    }
}
