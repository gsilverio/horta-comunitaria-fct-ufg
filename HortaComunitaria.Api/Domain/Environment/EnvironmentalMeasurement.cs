using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Environment;
/// <summary>Fato manual EnvironmentalMeasurement, com identidade, autoria imutável, versão e validade independentes.</summary>
public sealed class EnvironmentalMeasurement
{
    private EnvironmentalMeasurement() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry da inclusão, sem impor unicidade por dia.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Campo samplingPointId do fato.</summary>
    public Guid SamplingPointId { get; private set; }
    /// <summary>Campo variableId do fato.</summary>
    public Guid VariableId { get; private set; }
    /// <summary>Campo collectedAt do fato.</summary>
    public DateTimeOffset CollectedAt { get; private set; }
    /// <summary>Campo value do fato.</summary>
    public decimal Value { get; private set; }
    /// <summary>Campo unit do fato.</summary>
    public string Unit { get; private set; } = "";
    /// <summary>Campo method do fato.</summary>
    public string Method { get; private set; } = "";
    /// <summary>Campo quality do fato.</summary>
    public MeasurementQuality Quality { get; private set; }
    /// <summary>Campo responsibleParticipantId do fato.</summary>
    public Guid? ResponsibleParticipantId { get; private set; }
    /// <summary>Campo notes do fato.</summary>
    public string? Notes { get; private set; }
    /// <summary>Referência histórica bedId, resolvida pelo servidor.</summary>
    public Guid? BedId { get; private set; }
    /// <summary>Referência histórica sectorId, resolvida pelo servidor.</summary>
    public Guid? SectorId { get; private set; }
    /// <summary>Referência histórica bedSectorAssignmentId, resolvida pelo servidor.</summary>
    public Guid? BedSectorAssignmentId { get; private set; }

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
    public static Result<EnvironmentalMeasurement> Create(Guid gardenId, Guid operationId, EnvironmentalMeasurementValues values, Guid actor, DateTimeOffset at, string payload, Guid? bedId, Guid? sectorId, Guid? bedSectorAssignmentId)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(payload))
            return Result<EnvironmentalMeasurement>.Failure(Error.Validation("id", "Identidade, operação e autoria obrigatórias."));
        var entity = new EnvironmentalMeasurement
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            OperationId = operationId,
            RecordedByUserId = actor,
            RecordedAt = at.ToUniversalTime(),
            CreationPayload = payload
        };
        entity.Apply(values, bedId, sectorId, bedSectorAssignmentId);
        return Result<EnvironmentalMeasurement>.Success(entity);
    }
    /// <summary>Confere concorrência e validade antes de alterar o fato.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
    /// <summary>Corrige somente valores editáveis, preservando a autoria e payload da inclusão.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, EnvironmentalMeasurementValues values, DateTimeOffset at, Guid? bedId, Guid? sectorId, Guid? bedSectorAssignmentId)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error);

        if (SamplingPointId == values.SamplingPointId && VariableId == values.VariableId && CollectedAt == values.CollectedAt && Value == values.Value && Unit == values.Unit && Method == values.Method && Quality == values.Quality && ResponsibleParticipantId == values.ResponsibleParticipantId && Notes == values.Notes && BedId == bedId && SectorId == sectorId && BedSectorAssignmentId == bedSectorAssignmentId) return Result<bool>.Success(false);
        Apply(values, bedId, sectorId, bedSectorAssignmentId);
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
    private void Apply(EnvironmentalMeasurementValues values, Guid? bedId, Guid? sectorId, Guid? bedSectorAssignmentId) { SamplingPointId = values.SamplingPointId; VariableId = values.VariableId; CollectedAt = values.CollectedAt; Value = values.Value; Unit = values.Unit; Method = values.Method; Quality = values.Quality; ResponsibleParticipantId = values.ResponsibleParticipantId; Notes = values.Notes; BedId = bedId; SectorId = sectorId; BedSectorAssignmentId = bedSectorAssignmentId; }
}
