using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Fato manual DryingLot, com identidade, autoria imutável, versão e validade independentes.</summary>
public sealed class DryingLot
{
    private DryingLot() { }
    /// <summary>Situação derivada da pesagem final conhecida.</summary>
    public DryingStatus Status { get; private set; }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry da inclusão, sem impor unicidade por dia.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Campo code do fato.</summary>
    public string Code { get; private set; } = "";
    /// <summary>Campo method do fato.</summary>
    public string? Method { get; private set; }
    /// <summary>Campo startedOn do fato.</summary>
    public DateOnly StartedOn { get; private set; }
    /// <summary>Campo endedOn do fato.</summary>
    public DateOnly? EndedOn { get; private set; }
    /// <summary>Campo totalDryMassKg do fato.</summary>
    public decimal? TotalDryMassKg { get; private set; }
    /// <summary>Campo responsibleParticipantId do fato.</summary>
    public Guid? ResponsibleParticipantId { get; private set; }
    /// <summary>Campo items do fato.</summary>
    public IReadOnlyCollection<DryingItem> Items => _items.AsReadOnly();
    private readonly List<DryingItem> _items = new();


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
    public static Result<DryingLot> Create(Guid gardenId, Guid operationId, DryingLotValues values, Guid actor, DateTimeOffset at, string payload)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(payload))
            return Result<DryingLot>.Failure(Error.Validation("id", "Identidade, operação e autoria obrigatórias."));
        var entity = new DryingLot
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            OperationId = operationId,
            RecordedByUserId = actor,
            RecordedAt = at.ToUniversalTime(),
            CreationPayload = payload
        };
        entity.Apply(values);
        return Result<DryingLot>.Success(entity);
    }
    /// <summary>Confere concorrência e validade antes de alterar o fato.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
    /// <summary>Corrige somente valores editáveis, preservando a autoria e payload da inclusão.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, DryingLotValues values, DateTimeOffset at)
    {
        var valid = ValidateChange(expectedVersion);
        if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error);

        if (Code == values.Code && Method == values.Method && StartedOn == values.StartedOn && EndedOn == values.EndedOn && TotalDryMassKg == values.TotalDryMassKg && ResponsibleParticipantId == values.ResponsibleParticipantId && Items.Select(x => x.Snapshot()).OrderBy(x => x.HarvestId).SequenceEqual(values.Items)) return Result<bool>.Success(false);
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
    private void Apply(DryingLotValues values)
    {
        Status = values.EndedOn is not null && values.TotalDryMassKg is not null ? DryingStatus.Completed : DryingStatus.InProgress; Code = values.Code; Method = values.Method; StartedOn = values.StartedOn; EndedOn = values.EndedOn; TotalDryMassKg = values.TotalDryMassKg; ResponsibleParticipantId = values.ResponsibleParticipantId; foreach (var old in _items.Where(x => !values.Items.Any(v => v.HarvestId == x.HarvestId)).ToArray()) _items.Remove(old);
        foreach (var value in values.Items)
        {
            var retained = _items.SingleOrDefault(x => x.HarvestId == value.HarvestId);
            if (retained is null) _items.Add(DryingItem.Create(GardenId, Id, value).Value); else retained.Update(value);
        }
    }
}
