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
/// <summary>Associação pertencente ao agregado Activity, confirmada e auditada com seu pai.</summary>
public sealed class InputUsage
{
    private InputUsage() { }
    /// <summary>Identidade de um uso legítimo, inclusive quando o mesmo insumo se repete.</summary>
    public Guid Id { get; private set; }
    /// <summary>Contexto da associação, reforçado por FK composta.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identidade do pai.</summary>
    public Guid ActivityId { get; private set; }
    /// <summary>Campo inputId da associação.</summary>
    public Guid InputId { get; private set; }
    /// <summary>Campo quantity da associação.</summary>
    public decimal Quantity { get; private set; }
    /// <summary>Campo unit da associação.</summary>
    public string Unit { get; private set; } = "";
    /// <summary>Cria uma associação a partir de valores previamente validados.</summary>
    public static Result<InputUsage> Create(Guid gardenId, Guid parentId, InputUsageValues values)
    {
        if (gardenId == Guid.Empty || parentId == Guid.Empty) return Result<InputUsage>.Failure(Error.Validation("id", "Pai e horta obrigatórios."));
        var entity = new InputUsage { GardenId = gardenId, ActivityId = parentId, Id = Guid.CreateVersion7() };
        entity.Update(values);
        return Result<InputUsage>.Success(entity);
    }
    internal void Update(InputUsageValues values) { InputId = values.InputId; Quantity = values.Quantity; Unit = values.Unit; }
    internal InputUsageValues Snapshot() => InputUsageValues.Create(InputId, Quantity, Unit).Value;
}
