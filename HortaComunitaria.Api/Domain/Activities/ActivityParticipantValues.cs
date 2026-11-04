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
/// <summary>Valores imutáveis da associação ActivityParticipant.</summary>
public sealed record ActivityParticipantValues
{
    private ActivityParticipantValues() { }
    /// <summary>Campo participantId validado.</summary>
    public Guid ParticipantId { get; private init; }
    /// <summary>Campo role validado.</summary>
    public string? Role { get; private init; }
    /// <summary>Campo workHours validado.</summary>
    public decimal? WorkHours { get; private init; }
    /// <summary>Campo countsAsAudience validado.</summary>
    public bool CountsAsAudience { get; private init; }
    /// <summary>Valida referências, textos e grandezas antes de construir a associação.</summary>
    public static Result<ActivityParticipantValues> Create(Guid participantId, string? role, decimal? workHours, bool countsAsAudience)
    {
        var error = (participantId == Guid.Empty ? Error.Validation("participantId", "Identificador inválido.") : null) ?? RecordRules.Text(role, "role", 120, false) ?? RecordRules.Number(workHours, "workHours", 3);
        if (error is not null) return Result<ActivityParticipantValues>.Failure(error);

        return Result<ActivityParticipantValues>.Success(new() { ParticipantId = participantId, Role = RecordRules.Optional(role), WorkHours = workHours, CountsAsAudience = countsAsAudience });
    }
}
