using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Occurrences;
/// <summary>Fato manual Occurrence, com identidade, autoria imutável, versão e validade independentes.</summary>
public sealed class Occurrence
{
    private Occurrence() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry da inclusão, sem impor unicidade por dia.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Campo openedAt do fato.</summary>
    public DateTimeOffset OpenedAt { get; private set; }
    /// <summary>Campo category do fato.</summary>
    public string Category { get; private set; } = "";
    /// <summary>Campo description do fato.</summary>
    public string Description { get; private set; } = "";
    /// <summary>Campo status do fato.</summary>
    public OccurrenceStatus Status { get; private set; }
    /// <summary>Campo resolvedAt do fato.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }
    /// <summary>Campo resolution do fato.</summary>
    public string? Resolution { get; private set; }
    /// <summary>Campo responsibleParticipantId do fato.</summary>
    public Guid? ResponsibleParticipantId { get; private set; }
    /// <summary>Campo bedId do fato.</summary>
    public Guid? BedId { get; private set; }
    /// <summary>Campo sectorId do fato.</summary>
    public Guid? SectorId { get; private set; }
    /// <summary>Campo cycleId do fato.</summary>
    public Guid? CycleId { get; private set; }
    /// <summary>Campo irrigationId do fato.</summary>
    public Guid? IrrigationId { get; private set; }


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
    public static Result<Occurrence> Create(Guid gardenId, Guid operationId, OccurrenceValues values, Guid actor, DateTimeOffset at, string payload)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(payload))
            return Result<Occurrence>.Failure(Error.Validation("id", "Identidade, operação e autoria obrigatórias."));
        var entity = new Occurrence
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            OperationId = operationId,
            RecordedByUserId = actor,
            RecordedAt = at.ToUniversalTime(),
            CreationPayload = payload
        };
        entity.Apply(values);
        return Result<Occurrence>.Success(entity);
    }
    /// <summary>Confere concorrência e validade antes de alterar o fato.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
    /// <summary>Corrige somente valores editáveis, preservando a autoria e payload da inclusão.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, OccurrenceValues values, DateTimeOffset at)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error);
        if (Status == OccurrenceStatus.Resolved && values.Status != OccurrenceStatus.Resolved) return Result<bool>.Failure(new("occurrence.reopening_pending", "Reabertura de ocorrência resolvida não está definida no MVP.", ErrorCategory.Conflict));
        if (OpenedAt == values.OpenedAt && Category == values.Category && Description == values.Description && Status == values.Status && ResolvedAt == values.ResolvedAt && Resolution == values.Resolution && ResponsibleParticipantId == values.ResponsibleParticipantId && BedId == values.BedId && SectorId == values.SectorId && CycleId == values.CycleId && IrrigationId == values.IrrigationId) return Result<bool>.Success(false);
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
    private void Apply(OccurrenceValues values) { OpenedAt = values.OpenedAt; Category = values.Category; Description = values.Description; Status = values.Status; ResolvedAt = values.ResolvedAt; Resolution = values.Resolution; ResponsibleParticipantId = values.ResponsibleParticipantId; BedId = values.BedId; SectorId = values.SectorId; CycleId = values.CycleId; IrrigationId = values.IrrigationId; }
}
