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
/// <summary>Associação pertencente ao agregado DryingLot, confirmada e auditada com seu pai.</summary>
public sealed class DryingItem
{
    private DryingItem() { }

    /// <summary>Contexto da associação, reforçado por FK composta.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identidade do pai.</summary>
    public Guid DryingLotId { get; private set; }
    /// <summary>Campo harvestId da associação.</summary>
    public Guid HarvestId { get; private set; }
    /// <summary>Campo allocatedMassKg da associação.</summary>
    public decimal AllocatedMassKg { get; private set; }
    /// <summary>Cria uma associação a partir de valores previamente validados.</summary>
    public static Result<DryingItem> Create(Guid gardenId, Guid parentId, DryingItemValues values)
    {
        if (gardenId == Guid.Empty || parentId == Guid.Empty) return Result<DryingItem>.Failure(Error.Validation("id", "Pai e horta obrigatórios."));
        var entity = new DryingItem { GardenId = gardenId, DryingLotId = parentId };
        entity.Update(values);
        return Result<DryingItem>.Success(entity);
    }
    internal void Update(DryingItemValues values) { HarvestId = values.HarvestId; AllocatedMassKg = values.AllocatedMassKg; }
    internal DryingItemValues Snapshot() => DryingItemValues.Create(HarvestId, AllocatedMassKg).Value;
}
