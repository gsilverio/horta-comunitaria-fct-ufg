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
public sealed class ActivityInstitution
{
    private ActivityInstitution() { }

    /// <summary>Contexto da associação, reforçado por FK composta.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identidade do pai.</summary>
    public Guid ActivityId { get; private set; }
    /// <summary>Campo institutionId da associação.</summary>
    public Guid InstitutionId { get; private set; }
    /// <summary>Campo role da associação.</summary>
    public string? Role { get; private set; }
    /// <summary>Cria uma associação a partir de valores previamente validados.</summary>
    public static Result<ActivityInstitution> Create(Guid gardenId, Guid parentId, ActivityInstitutionValues values)
    {
        if (gardenId == Guid.Empty || parentId == Guid.Empty) return Result<ActivityInstitution>.Failure(Error.Validation("id", "Pai e horta obrigatórios."));
        var entity = new ActivityInstitution { GardenId = gardenId, ActivityId = parentId };
        entity.Update(values);
        return Result<ActivityInstitution>.Success(entity);
    }
    internal void Update(ActivityInstitutionValues values) { InstitutionId = values.InstitutionId; Role = values.Role; }
    internal ActivityInstitutionValues Snapshot() => ActivityInstitutionValues.Create(InstitutionId, Role).Value;
}
