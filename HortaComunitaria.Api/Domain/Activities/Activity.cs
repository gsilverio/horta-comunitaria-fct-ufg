using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Activities;
/// <summary>Fato manual Activity, com identidade, autoria imutável, versão e validade independentes.</summary>
public sealed class Activity
{
    private Activity() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry da inclusão, sem impor unicidade por dia.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Campo title do fato.</summary>
    public string Title { get; private set; } = "";
    /// <summary>Campo category do fato.</summary>
    public string Category { get; private set; } = "";
    /// <summary>Campo performedOn do fato.</summary>
    public DateOnly PerformedOn { get; private set; }
    /// <summary>Campo description do fato.</summary>
    public string? Description { get; private set; }
    /// <summary>Campo bedId do fato.</summary>
    public Guid? BedId { get; private set; }
    /// <summary>Campo sectorId do fato.</summary>
    public Guid? SectorId { get; private set; }
    /// <summary>Campo cycleId do fato.</summary>
    public Guid? CycleId { get; private set; }
    /// <summary>Campo occurrenceId do fato.</summary>
    public Guid? OccurrenceId { get; private set; }
    /// <summary>Campo isOperational do fato.</summary>
    public bool IsOperational { get; private set; }
    /// <summary>Campo isSocial do fato.</summary>
    public bool IsSocial { get; private set; }
    /// <summary>Campo isEducational do fato.</summary>
    public bool IsEducational { get; private set; }
    /// <summary>Campo objective do fato.</summary>
    public string? Objective { get; private set; }
    /// <summary>Campo modality do fato.</summary>
    public string? Modality { get; private set; }
    /// <summary>Campo reportedAudienceTotal do fato.</summary>
    public int? ReportedAudienceTotal { get; private set; }
    /// <summary>Campo audienceListComplete do fato.</summary>
    public bool? AudienceListComplete { get; private set; }
    /// <summary>Campo participants do fato.</summary>
    public IReadOnlyCollection<ActivityParticipant> Participants => _participants.AsReadOnly();
    private readonly List<ActivityParticipant> _participants = new();
    /// <summary>Campo inputUsages do fato.</summary>
    public IReadOnlyCollection<InputUsage> InputUsages => _inputUsages.AsReadOnly();
    private readonly List<InputUsage> _inputUsages = new();
    /// <summary>Campo institutions do fato.</summary>
    public IReadOnlyCollection<ActivityInstitution> Institutions => _institutions.AsReadOnly();
    private readonly List<ActivityInstitution> _institutions = new();


    /// <summary>Autor resolvido no servidor.</summary>
    public Guid RecordedByUserId { get; private set; }
    /// <summary>Instante de inclusão em UTC, distinto do fato.</summary>
    public DateTimeOffset RecordedAt { get; private set; }
    /// <summary>Última alteração efetiva em UTC.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Token de concorrência persistido.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Elegibilidade nos indicadores.</summary>
    public RecordValidity Validity { get; private set; } = RecordValidity.Valid;
    /// <summary>Conteúdo normalizado da inclusão, imutável mesmo após correções.</summary>
    public string CreationPayload { get; private set; } = "";
    /// <summary>Cria um fato completo com autoria atribuída pelo servidor.</summary>
    public static Result<Activity> Create(Guid gardenId, Guid operationId, ActivityValues values, Guid actor, DateTimeOffset at, string payload)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(payload))
            return Result<Activity>.Failure(Error.Validation("id", "Identidade, operação e autoria obrigatórias."));
        var entity = new Activity
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            OperationId = operationId,
            RecordedByUserId = actor,
            RecordedAt = at.ToUniversalTime(),
            CreationPayload = payload
        };
        entity.Apply(values);
        return Result<Activity>.Success(entity);
    }
    /// <summary>Confere concorrência e validade antes de alterar o fato.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
    /// <summary>Corrige somente valores editáveis, preservando a autoria e payload da inclusão.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, ActivityValues values, DateTimeOffset at)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error);

        if (Title == values.Title && Category == values.Category && PerformedOn == values.PerformedOn && Description == values.Description && BedId == values.BedId && SectorId == values.SectorId && CycleId == values.CycleId && OccurrenceId == values.OccurrenceId && IsOperational == values.IsOperational && IsSocial == values.IsSocial && IsEducational == values.IsEducational && Objective == values.Objective && Modality == values.Modality && ReportedAudienceTotal == values.ReportedAudienceTotal && AudienceListComplete == values.AudienceListComplete && Participants.Select(x => x.Snapshot()).OrderBy(x => x.ParticipantId).SequenceEqual(values.Participants) && InputUsages.Select(x => x.Snapshot()).OrderBy(x => x.InputId).ThenBy(x => x.Unit).ThenBy(x => x.Quantity).SequenceEqual(values.InputUsages) && Institutions.Select(x => x.Snapshot()).OrderBy(x => x.InstitutionId).SequenceEqual(values.Institutions)) return Result<bool>.Success(false);
        Apply(values);
        Version++; UpdatedAt = at.ToUniversalTime();
        return Result<bool>.Success(true);
    }
    /// <summary>Retira o fato dos indicadores sem apagar sua origem ou histórico.</summary>
    public Result<Unit> Invalidate(int expectedVersion, ChangeReason reason, DateTimeOffset at)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return valid;
        Validity = RecordValidity.Invalid; Version++; UpdatedAt = at.ToUniversalTime();
        return Result<Unit>.Success(new());
    }
    private void Apply(ActivityValues values)
    {
        Title = values.Title; Category = values.Category; PerformedOn = values.PerformedOn; Description = values.Description; BedId = values.BedId; SectorId = values.SectorId; CycleId = values.CycleId; OccurrenceId = values.OccurrenceId; IsOperational = values.IsOperational; IsSocial = values.IsSocial; IsEducational = values.IsEducational; Objective = values.Objective; Modality = values.Modality; ReportedAudienceTotal = values.ReportedAudienceTotal; AudienceListComplete = values.AudienceListComplete; foreach (var old in _participants.Where(x => !values.Participants.Any(v => v.ParticipantId == x.ParticipantId)).ToArray()) _participants.Remove(old);
        foreach (var value in values.Participants)
        {
            var retained = _participants.SingleOrDefault(x => x.ParticipantId == value.ParticipantId);
            if (retained is null) _participants.Add(ActivityParticipant.Create(GardenId, Id, value).Value); else retained.Update(value);
        }
        _inputUsages.Clear(); _inputUsages.AddRange(values.InputUsages.Select(value => InputUsage.Create(GardenId, Id, value).Value)); foreach (var old in _institutions.Where(x => !values.Institutions.Any(v => v.InstitutionId == x.InstitutionId)).ToArray()) _institutions.Remove(old);
        foreach (var value in values.Institutions)
        {
            var retained = _institutions.SingleOrDefault(x => x.InstitutionId == value.InstitutionId);
            if (retained is null) _institutions.Add(ActivityInstitution.Create(GardenId, Id, value).Value); else retained.Update(value);
        }
    }
}
