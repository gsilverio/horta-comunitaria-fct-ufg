using HortaComunitaria.Api.Common.Results;
using IrrigationRecord = HortaComunitaria.Api.Domain.Irrigation.Irrigation;

namespace HortaComunitaria.Api.Domain.History;

/// <summary>Histórico de inclusão/alteração da irrigação; snapshots são preparados fora do domínio.</summary>
public sealed class AuditEntry
{
    private AuditEntry() { }
    /// <summary>Identidade UUID v7 da alteração.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta do fato auditado.</summary>
    public Guid? GardenId { get; private set; }
    /// <summary>Vínculo concreto do histórico legado de irrigação; outros registros usam sua identidade auditada.</summary>
    public Guid? IrrigationId { get; private set; }
    /// <summary>Autor técnico da operação.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Tipo do fato auditado nesta entrega.</summary>
    public string EntityType { get; private set; } = "";
    /// <summary>Identidade do fato preservado.</summary>
    public Guid EntityKey { get; private set; }
    /// <summary>Operação realizada.</summary>
    public AuditOperation Operation { get; private set; }
    /// <summary>Motivo, não aplicável à inclusão.</summary>
    public string? Reason { get; private set; }
    /// <summary>Instante da operação em UTC.</summary>
    public DateTimeOffset OccurredAt { get; private set; }
    /// <summary>Snapshot anterior, inexistente na inclusão.</summary>
    public string? Before { get; private set; }
    /// <summary>Snapshot após a operação, serializado na fronteira de aplicação.</summary>
    public string After { get; private set; } = "";

    /// <summary>Vincula a inclusão ao mesmo fato, instante e autor do servidor.</summary>
    public static Result<AuditEntry> CreateInclusion(IrrigationRecord irrigation, string after)
    {
        if (string.IsNullOrWhiteSpace(after))
            return Result<AuditEntry>.Failure(Error.Validation("after", "Snapshot da inclusão é obrigatório."));
        return Result<AuditEntry>.Success(new()
        {
            Id = Guid.CreateVersion7(),
            GardenId = irrigation.GardenId,
            UserId = irrigation.RecordedByUserId,
            EntityType = "Irrigation",
            EntityKey = irrigation.Id,
            IrrigationId = irrigation.Id,
            Operation = AuditOperation.Created,
            OccurredAt = irrigation.RecordedAt,
            After = after
        });
    }

    /// <summary>Registra uma alteração efetiva com antes/depois e autoria da operação, sem mudar a autoria do fato.</summary>
    public static Result<AuditEntry> CreateChange(IrrigationRecord irrigation, AuditOperation operation, ChangeReason reason,
        Guid actorId, DateTimeOffset occurredAt, string before, string after)
    {
        if (operation is not (AuditOperation.Corrected or AuditOperation.Invalidated) || actorId == Guid.Empty ||
            string.IsNullOrWhiteSpace(before) || string.IsNullOrWhiteSpace(after) || irrigation.UpdatedAt != occurredAt.ToUniversalTime() ||
            operation == AuditOperation.Invalidated && irrigation.Validity != RecordValidity.Invalid)
            return Result<AuditEntry>.Failure(Error.Validation("audit", "Alteração auditada exige operação, autoria e snapshots válidos."));
        return Result<AuditEntry>.Success(new()
        {
            Id = Guid.CreateVersion7(),
            GardenId = irrigation.GardenId,
            UserId = actorId,
            EntityType = "Irrigation",
            EntityKey = irrigation.Id,
            IrrigationId = irrigation.Id,
            Operation = operation,
            Reason = reason.Value,
            OccurredAt = occurredAt.ToUniversalTime(),
            Before = before,
            After = after
        });
    }

    /// <summary>Cria histórico transacional de cadastro ou fato, com autor do servidor e snapshots completos.</summary>
    public static Result<AuditEntry> CreateRecord(Guid? gardenId, string entityType, Guid entityKey, Guid actor,
        DateTimeOffset at, AuditOperation operation, string? reason, string? before, string after)
    {
        if (gardenId == Guid.Empty || entityKey == Guid.Empty || actor == Guid.Empty || !Enum.IsDefined(operation) ||
            string.IsNullOrWhiteSpace(entityType) || entityType.Length > 80 || string.IsNullOrWhiteSpace(after) ||
            operation != AuditOperation.Created && (string.IsNullOrWhiteSpace(before) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500))
            return Result<AuditEntry>.Failure(Error.Validation("audit", "Histórico exige identidade, autoria, motivo e snapshots compatíveis."));
        return Result<AuditEntry>.Success(new()
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            EntityType = entityType,
            EntityKey = entityKey,
            IrrigationId = entityType == "Irrigation" ? entityKey : null,
            UserId = actor,
            OccurredAt = at.ToUniversalTime(),
            Operation = operation,
            Reason = RecordRules.Optional(reason),
            Before = before,
            After = after
        });
    }
}
