using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Fato manual Harvest, com identidade, autoria imutável, versão e validade independentes.</summary>
public sealed class Harvest
{
    private Harvest() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry da inclusão, sem impor unicidade por dia.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Campo cycleId do fato.</summary>
    public Guid CycleId { get; private set; }
    /// <summary>Campo harvestedOn do fato.</summary>
    public DateOnly HarvestedOn { get; private set; }
    /// <summary>Campo freshMassKg do fato.</summary>
    public decimal FreshMassKg { get; private set; }
    /// <summary>Campo usableMassKg do fato.</summary>
    public decimal? UsableMassKg { get; private set; }
    /// <summary>Campo lossReason do fato.</summary>
    public string? LossReason { get; private set; }
    /// <summary>Campo responsibleParticipantId do fato.</summary>
    public Guid? ResponsibleParticipantId { get; private set; }


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
    public static Result<Harvest> Create(Guid gardenId, Guid operationId, HarvestValues values, Guid actor, DateTimeOffset at, string payload)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(payload))
            return Result<Harvest>.Failure(Error.Validation("id", "Identidade, operação e autoria obrigatórias."));
        var entity = new Harvest
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            OperationId = operationId,
            RecordedByUserId = actor,
            RecordedAt = at.ToUniversalTime(),
            CreationPayload = payload
        };
        entity.Apply(values);
        return Result<Harvest>.Success(entity);
    }
    /// <summary>Confere concorrência e validade antes de alterar o fato.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
    /// <summary>Corrige somente valores editáveis, preservando a autoria e payload da inclusão.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, HarvestValues values, DateTimeOffset at)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error);

        if (CycleId == values.CycleId && HarvestedOn == values.HarvestedOn && FreshMassKg == values.FreshMassKg && UsableMassKg == values.UsableMassKg && LossReason == values.LossReason && ResponsibleParticipantId == values.ResponsibleParticipantId) return Result<bool>.Success(false);
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
    private void Apply(HarvestValues values) { CycleId = values.CycleId; HarvestedOn = values.HarvestedOn; FreshMassKg = values.FreshMassKg; UsableMassKg = values.UsableMassKg; LossReason = values.LossReason; ResponsibleParticipantId = values.ResponsibleParticipantId; }
}
