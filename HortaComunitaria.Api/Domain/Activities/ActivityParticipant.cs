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
public sealed class ActivityParticipant
{
    private ActivityParticipant() { }

    /// <summary>Contexto da associação, reforçado por FK composta.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identidade do pai.</summary>
    public Guid ActivityId { get; private set; }
    /// <summary>Campo participantId da associação.</summary>
    public Guid ParticipantId { get; private set; }
    /// <summary>Campo role da associação.</summary>
    public string? Role { get; private set; }
    /// <summary>Campo workHours da associação.</summary>
    public decimal? WorkHours { get; private set; }
    /// <summary>Campo countsAsAudience da associação.</summary>
    public bool CountsAsAudience { get; private set; }
    /// <summary>Cria uma associação a partir de valores previamente validados.</summary>
    public static Result<ActivityParticipant> Create(Guid gardenId, Guid parentId, ActivityParticipantValues values)
    {
        if (gardenId == Guid.Empty || parentId == Guid.Empty) return Result<ActivityParticipant>.Failure(Error.Validation("id", "Pai e horta obrigatórios."));
        var entity = new ActivityParticipant { GardenId = gardenId, ActivityId = parentId };
        entity.Update(values);
        return Result<ActivityParticipant>.Success(entity);
    }
    internal void Update(ActivityParticipantValues values) { ParticipantId = values.ParticipantId; Role = values.Role; WorkHours = values.WorkHours; CountsAsAudience = values.CountsAsAudience; }
    internal ActivityParticipantValues Snapshot() => ActivityParticipantValues.Create(ParticipantId, Role, WorkHours, CountsAsAudience).Value;
}
