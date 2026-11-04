using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;

namespace HortaComunitaria.Api.Domain.Irrigation;

/// <summary>Fato de irrigação com autoria do servidor, contexto histórico e payload inicial imutável.</summary>
public sealed class Irrigation
{
    private readonly List<IrrigationBed> beds = [];
    private Irrigation() { }
    /// <summary>Identidade UUID v7 do fato.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta à qual todas as referências devem pertencer.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Setor registrado, independente de associações atuais.</summary>
    public Guid IrrigationSectorId { get; private set; }
    /// <summary>Início do fato em UTC.</summary>
    public DateTimeOffset StartedAt { get; private set; }
    /// <summary>Fim em UTC, quando coletado.</summary>
    public DateTimeOffset? EndedAt { get; private set; }
    /// <summary>Duração ativa em minutos.</summary>
    public decimal? ActiveDurationMinutes { get; private set; }
    /// <summary>Volume em litros, sem multiplicação pelos filhos.</summary>
    public decimal? VolumeLiters { get; private set; }
    /// <summary>Vazão informativa em litros/minuto.</summary>
    public decimal? FlowRateLitersPerMinute { get; private set; }
    /// <summary>Origem do volume conhecido.</summary>
    public VolumeSource? VolumeSource { get; private set; }
    /// <summary>Método informado, normalizado.</summary>
    public string? VolumeMethod { get; private set; }
    /// <summary>Situação operacional do evento.</summary>
    public IrrigationStatus Status { get; private set; }
    /// <summary>Responsável pelo trabalho, independente da autoria.</summary>
    public Guid? ResponsibleParticipantId { get; private set; }
    /// <summary>Observações opcionais.</summary>
    public string? Notes { get; private set; }
    /// <summary>Autor resolvido exclusivamente no servidor.</summary>
    public Guid RecordedByUserId { get; private set; }
    /// <summary>Instante de inclusão; não é o instante do fato.</summary>
    public DateTimeOffset RecordedAt { get; private set; }
    /// <summary>Instante da última mudança; inicialmente desconhecido.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Elegibilidade para indicadores.</summary>
    public RecordValidity Validity { get; private set; }
    /// <summary>Token persistido de concorrência; inicia em 1.</summary>
    public int Version { get; private set; }
    /// <summary>Chave de repetição única dentro da horta.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Representação inicial normalizada, serializada fora do domínio.</summary>
    public string CreationPayload { get; private set; } = "";
    /// <summary>Canteiros atendidos e referências históricas, sem volumes próprios.</summary>
    public IReadOnlyCollection<IrrigationBed> Beds => beds.AsReadOnly();

    /// <summary>Cria o fato após resolução das relações no handler, sem consultar cadastros.</summary>
    public static Result<Irrigation> Create(Guid gardenId, Guid sectorId, Guid operationId,
        IrrigationValues values, Guid? responsibleParticipantId, Guid authorId, DateTimeOffset recordedAt,
        string creationPayload, IReadOnlyCollection<BedSectorAssignment> assignments)
    {
        if (gardenId == Guid.Empty || sectorId == Guid.Empty || operationId == Guid.Empty || authorId == Guid.Empty ||
            responsibleParticipantId == Guid.Empty || string.IsNullOrWhiteSpace(creationPayload))
            return Result<Irrigation>.Failure(Error.Validation("id", "Identificadores e payload inicial devem ser válidos."));
        if (assignments.Any(x => x.GardenId != gardenId || x.IrrigationSectorId != sectorId) ||
            assignments.Select(x => x.BedId).Distinct().Count() != assignments.Count)
            return Result<Irrigation>.Failure(new("irrigation.invalid_bed_assignment",
                "Vínculos devem ser distintos e pertencer à horta e ao setor.", ErrorCategory.Validation));
        var irrigation = new Irrigation
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            IrrigationSectorId = sectorId,
            OperationId = operationId,
            StartedAt = values.StartedAt,
            EndedAt = values.EndedAt,
            ActiveDurationMinutes = values.ActiveDurationMinutes,
            VolumeLiters = values.VolumeLiters,
            FlowRateLitersPerMinute = values.FlowRateLitersPerMinute,
            VolumeSource = values.VolumeSource,
            VolumeMethod = values.VolumeMethod,
            Status = values.Status,
            Notes = values.Notes,
            ResponsibleParticipantId = responsibleParticipantId,
            RecordedByUserId = authorId,
            RecordedAt = recordedAt.ToUniversalTime(),
            Validity = RecordValidity.Valid,
            Version = 1,
            CreationPayload = creationPayload
        };
        foreach (var assignment in assignments)
            irrigation.beds.Add(IrrigationBed.Create(irrigation, assignment).Value);
        return Result<Irrigation>.Success(irrigation);
    }

    /// <summary>Confere a intenção de alterar a versão atual; registros inválidos não podem mudar.</summary>
    public Result<Unit> ValidateChange(int expectedVersion)
    {
        if (expectedVersion < 1)
            return Result<Unit>.Failure(Error.Validation("expectedVersion", "Versão esperada deve ser positiva."));
        if (Version != expectedVersion)
            return Result<Unit>.Failure(new("irrigation.version_conflict", "O registro foi alterado. Recarregue antes de revisar.", ErrorCategory.Conflict));
        if (Validity == RecordValidity.Invalid)
            return Result<Unit>.Failure(new("irrigation.record_invalid", "Registro invalidado não pode ser alterado.", ErrorCategory.Conflict));
        return Result<Unit>.Success(new());
    }

    /// <summary>Aplica valores/vínculos válidos e incrementa a versão apenas quando houver mudança efetiva.</summary>
    public Result<bool> Correct(int expectedVersion, ChangeReason reason, Guid sectorId, IrrigationValues values,
        Guid? responsibleParticipantId, IReadOnlyCollection<BedSectorAssignment> assignments, DateTimeOffset updatedAt)
    {
        var validation = ValidateChange(expectedVersion);
        if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);
        ArgumentNullException.ThrowIfNull(reason);
        if (sectorId == Guid.Empty || responsibleParticipantId == Guid.Empty ||
            assignments.Any(x => x.GardenId != GardenId || x.IrrigationSectorId != sectorId) ||
            assignments.Select(x => x.BedId).Distinct().Count() != assignments.Count)
            return Result<bool>.Failure(new("irrigation.invalid_bed_assignment", "Contexto dos vínculos corrigidos é inválido.", ErrorCategory.Validation));
        if (IrrigationSectorId == sectorId && ResponsibleParticipantId == responsibleParticipantId &&
            StartedAt == values.StartedAt && EndedAt == values.EndedAt && ActiveDurationMinutes == values.ActiveDurationMinutes &&
            VolumeLiters == values.VolumeLiters && FlowRateLitersPerMinute == values.FlowRateLitersPerMinute &&
            VolumeSource == values.VolumeSource && VolumeMethod == values.VolumeMethod && Status == values.Status && Notes == values.Notes &&
            beds.Count == assignments.Count && assignments.All(x => beds.Any(b => b.BedId == x.BedId && b.BedSectorAssignmentId == x.Id)))
            return Result<bool>.Success(false);
        IrrigationSectorId = sectorId;
        StartedAt = values.StartedAt;
        EndedAt = values.EndedAt;
        ActiveDurationMinutes = values.ActiveDurationMinutes;
        VolumeLiters = values.VolumeLiters;
        FlowRateLitersPerMinute = values.FlowRateLitersPerMinute;
        VolumeSource = values.VolumeSource;
        VolumeMethod = values.VolumeMethod;
        Status = values.Status;
        Notes = values.Notes;
        ResponsibleParticipantId = responsibleParticipantId;
        var selected = assignments.Select(x => x.BedId).ToHashSet();
        beds.RemoveAll(x => !selected.Contains(x.BedId));
        foreach (var assignment in assignments)
        {
            var existing = beds.SingleOrDefault(x => x.BedId == assignment.BedId);
            if (existing is null) beds.Add(IrrigationBed.Create(this, assignment).Value);
            else existing.Reassign(this, assignment);
        }
        Version = checked(Version + 1);
        UpdatedAt = updatedAt.ToUniversalTime();
        return Result<bool>.Success(true);
    }

    /// <summary>Invalida sem remover dados ou filhos, preservando criação e autoria originais.</summary>
    public Result<Unit> Invalidate(int expectedVersion, ChangeReason reason, DateTimeOffset updatedAt)
    {
        var validation = ValidateChange(expectedVersion);
        if (!validation.IsSuccess) return validation;
        ArgumentNullException.ThrowIfNull(reason);
        Validity = RecordValidity.Invalid;
        Version = checked(Version + 1);
        UpdatedAt = updatedAt.ToUniversalTime();
        return Result<Unit>.Success(new());
    }
}
