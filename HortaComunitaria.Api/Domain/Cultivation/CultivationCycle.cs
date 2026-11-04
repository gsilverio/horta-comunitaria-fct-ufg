using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Fato manual CultivationCycle, com identidade, autoria imutável, versão e validade independentes.</summary>
public sealed class CultivationCycle
{
    private CultivationCycle() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry da inclusão, sem impor unicidade por dia.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Campo bedId do fato.</summary>
    public Guid BedId { get; private set; }
    /// <summary>Campo speciesId do fato.</summary>
    public Guid SpeciesId { get; private set; }
    /// <summary>Campo cultivarId do fato.</summary>
    public Guid? CultivarId { get; private set; }
    /// <summary>Campo startedOn do fato.</summary>
    public DateOnly StartedOn { get; private set; }
    /// <summary>Campo occupiedAreaM2 do fato.</summary>
    public decimal OccupiedAreaM2 { get; private set; }
    /// <summary>Campo endedOn do fato.</summary>
    public DateOnly? EndedOn { get; private set; }
    /// <summary>Campo responsibleParticipantId do fato.</summary>
    public Guid? ResponsibleParticipantId { get; private set; }
    /// <summary>Referência histórica bedConfigurationId, resolvida pelo servidor.</summary>
    public Guid BedConfigurationId { get; private set; }
    /// <summary>Referência histórica referenceAreaM2, resolvida pelo servidor.</summary>
    public decimal ReferenceAreaM2 { get; private set; }
    /// <summary>Situação derivada do encerramento conhecido.</summary>
    public CultivationStatus Status { get; private set; }

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
    public static Result<CultivationCycle> Create(Guid gardenId, Guid operationId, CultivationCycleValues values, Guid actor, DateTimeOffset at, string payload, Guid bedConfigurationId, decimal referenceAreaM2)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(payload))
            return Result<CultivationCycle>.Failure(Error.Validation("id", "Identidade, operação e autoria obrigatórias."));
        var entity = new CultivationCycle
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            OperationId = operationId,
            RecordedByUserId = actor,
            RecordedAt = at.ToUniversalTime(),
            CreationPayload = payload
        };
        entity.Apply(values, bedConfigurationId, referenceAreaM2);
        return Result<CultivationCycle>.Success(entity);
    }
    /// <summary>Confere concorrência e validade antes de alterar o fato.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
    /// <summary>Corrige somente valores editáveis, preservando a autoria e payload da inclusão.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, CultivationCycleValues values, DateTimeOffset at, Guid bedConfigurationId, decimal referenceAreaM2)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error);

        if (BedId == values.BedId && SpeciesId == values.SpeciesId && CultivarId == values.CultivarId && StartedOn == values.StartedOn && OccupiedAreaM2 == values.OccupiedAreaM2 && EndedOn == values.EndedOn && ResponsibleParticipantId == values.ResponsibleParticipantId && BedConfigurationId == bedConfigurationId && ReferenceAreaM2 == referenceAreaM2) return Result<bool>.Success(false);
        Apply(values, bedConfigurationId, referenceAreaM2);
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
    private void Apply(CultivationCycleValues values, Guid bedConfigurationId, decimal referenceAreaM2) { BedId = values.BedId; SpeciesId = values.SpeciesId; CultivarId = values.CultivarId; StartedOn = values.StartedOn; OccupiedAreaM2 = values.OccupiedAreaM2; EndedOn = values.EndedOn; ResponsibleParticipantId = values.ResponsibleParticipantId; BedConfigurationId = bedConfigurationId; ReferenceAreaM2 = referenceAreaM2; Status = values.EndedOn is null ? CultivationStatus.InProgress : CultivationStatus.Closed; }
}
