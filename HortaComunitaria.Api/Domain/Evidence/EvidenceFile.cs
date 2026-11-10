using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;

namespace HortaComunitaria.Api.Domain.Evidence;

/// <summary>
/// Fotografia de campo vinculada a exatamente um registro da horta. O banco guarda metadados e a referência
/// gerada pelo servidor; o conteúdo fica atrás da fronteira de armazenamento de arquivos.
/// </summary>
public sealed class EvidenceFile
{
    /// <summary>Tamanho máximo do título.</summary>
    public const int TitleMaximum = 120;
    /// <summary>Tamanho máximo da descrição.</summary>
    public const int DescriptionMaximum = 500;

    private EvidenceFile() { }
    /// <summary>Identidade UUID v7.</summary>
    public Guid Id { get; private set; }
    /// <summary>Horta da fotografia e de seu registro vinculado.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Identifica retry do envio, sem impedir outra foto legítima.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Canteiro vinculado.</summary>
    public Guid? BedId { get; private set; }
    /// <summary>Ciclo vinculado.</summary>
    public Guid? CycleId { get; private set; }
    /// <summary>Plantio vinculado.</summary>
    public Guid? PlantingId { get; private set; }
    /// <summary>Avaliação de pegamento vinculada.</summary>
    public Guid? EstablishmentAssessmentId { get; private set; }
    /// <summary>Observação agronômica vinculada.</summary>
    public Guid? AgronomicObservationId { get; private set; }
    /// <summary>Colheita vinculada.</summary>
    public Guid? HarvestId { get; private set; }
    /// <summary>Lote de secagem vinculado.</summary>
    public Guid? DryingLotId { get; private set; }
    /// <summary>Atividade vinculada.</summary>
    public Guid? ActivityId { get; private set; }
    /// <summary>Ocorrência vinculada.</summary>
    public Guid? OccurrenceId { get; private set; }
    /// <summary>Título curto exibido na galeria.</summary>
    public string Title { get; private set; } = "";
    /// <summary>Descrição opcional do que a foto mostra.</summary>
    public string? Description { get; private set; }
    /// <summary>Data local em que a foto foi tirada, quando conhecida.</summary>
    public DateOnly? CapturedOn { get; private set; }
    /// <summary>Referência opaca gerada pelo servidor; nunca derivada do nome enviado.</summary>
    public string StorageKey { get; private set; } = "";
    /// <summary>Nome original apenas para exibição/download.</summary>
    public string OriginalFileName { get; private set; } = "";
    /// <summary>Tipo de conteúdo conferido pela assinatura do arquivo.</summary>
    public string ContentType { get; private set; } = "";
    /// <summary>Tamanho do conteúdo em bytes.</summary>
    public long SizeBytes { get; private set; }
    /// <summary>SHA-256 hexadecimal do conteúdo, usado na proteção de retry.</summary>
    public string Sha256 { get; private set; } = "";
    /// <summary>Autor resolvido no servidor.</summary>
    public Guid RecordedByUserId { get; private set; }
    /// <summary>Instante de inclusão em UTC.</summary>
    public DateTimeOffset RecordedAt { get; private set; }
    /// <summary>Última alteração efetiva em UTC.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }
    /// <summary>Token de concorrência persistido.</summary>
    public int Version { get; private set; } = 1;
    /// <summary>Fotos invalidadas permanecem consultáveis no histórico, fora da galeria padrão.</summary>
    public RecordValidity Validity { get; private set; } = RecordValidity.Valid;

    /// <summary>Destino derivado da única FK preenchida.</summary>
    public EvidenceTarget Target =>
        BedId is not null ? EvidenceTarget.Bed :
        CycleId is not null ? EvidenceTarget.CultivationCycle :
        PlantingId is not null ? EvidenceTarget.Planting :
        EstablishmentAssessmentId is not null ? EvidenceTarget.EstablishmentAssessment :
        AgronomicObservationId is not null ? EvidenceTarget.AgronomicObservation :
        HarvestId is not null ? EvidenceTarget.Harvest :
        DryingLotId is not null ? EvidenceTarget.DryingLot :
        ActivityId is not null ? EvidenceTarget.Activity : EvidenceTarget.Occurrence;

    /// <summary>Identificador do registro vinculado.</summary>
    public Guid TargetId => BedId ?? CycleId ?? PlantingId ?? EstablishmentAssessmentId ?? AgronomicObservationId ?? HarvestId ?? DryingLotId ?? ActivityId ?? OccurrenceId ?? Guid.Empty;

    /// <summary>Valida título, descrição e data informados pela pessoa.</summary>
    public static Result<(string Title, string? Description)> ValidateText(string? title, string? description)
    {
        var error = RecordRules.Text(title, "title", TitleMaximum, required: true) ?? RecordRules.Text(description, "description", DescriptionMaximum);
        return error is null
            ? Result<(string, string?)>.Success((title!.Trim(), RecordRules.Optional(description)))
            : Result<(string, string?)>.Failure(error);
    }

    /// <summary>Cria os metadados de uma fotografia já armazenada.</summary>
    public static Result<EvidenceFile> Create(Guid gardenId, Guid operationId, EvidenceTarget target, Guid targetId, string? title, string? description,
        DateOnly? capturedOn, string storageKey, string originalFileName, string contentType, long sizeBytes, string sha256, Guid actor, DateTimeOffset at)
    {
        if (gardenId == Guid.Empty || operationId == Guid.Empty || targetId == Guid.Empty || actor == Guid.Empty)
            return Result<EvidenceFile>.Failure(Error.Validation("id", "Identidade, operação, vínculo e autoria obrigatórios."));
        if (!Enum.IsDefined(target)) return Result<EvidenceFile>.Failure(Error.Validation("targetType", "Tipo de registro não suportado."));
        if (string.IsNullOrWhiteSpace(storageKey) || string.IsNullOrWhiteSpace(contentType) || sizeBytes <= 0 || sha256.Length != 64)
            return Result<EvidenceFile>.Failure(Error.Validation("file", "Arquivo ausente ou incompleto."));
        var text = ValidateText(title, description);
        if (!text.IsSuccess) return Result<EvidenceFile>.Failure(text.Error);
        var name = Path.GetFileName(originalFileName ?? "").Trim();
        var entity = new EvidenceFile
        {
            Id = Guid.CreateVersion7(), GardenId = gardenId, OperationId = operationId,
            Title = text.Value.Title, Description = text.Value.Description, CapturedOn = capturedOn,
            StorageKey = storageKey, OriginalFileName = name.Length is 0 ? "foto" : name.Length > 255 ? name[^255..] : name,
            ContentType = contentType, SizeBytes = sizeBytes, Sha256 = sha256,
            RecordedByUserId = actor, RecordedAt = at.ToUniversalTime()
        };
        switch (target)
        {
            case EvidenceTarget.Bed: entity.BedId = targetId; break;
            case EvidenceTarget.CultivationCycle: entity.CycleId = targetId; break;
            case EvidenceTarget.Planting: entity.PlantingId = targetId; break;
            case EvidenceTarget.EstablishmentAssessment: entity.EstablishmentAssessmentId = targetId; break;
            case EvidenceTarget.AgronomicObservation: entity.AgronomicObservationId = targetId; break;
            case EvidenceTarget.Harvest: entity.HarvestId = targetId; break;
            case EvidenceTarget.DryingLot: entity.DryingLotId = targetId; break;
            case EvidenceTarget.Activity: entity.ActivityId = targetId; break;
            case EvidenceTarget.Occurrence: entity.OccurrenceId = targetId; break;
        }
        return Result<EvidenceFile>.Success(entity);
    }

    /// <summary>Corrige título, descrição e data sem trocar o arquivo nem o vínculo.</summary>
    public Result<bool> Correct(int expectedVersion, string? title, string? description, DateOnly? capturedOn, DateTimeOffset at)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        if (error is not null) return Result<bool>.Failure(error);
        var text = ValidateText(title, description);
        if (!text.IsSuccess) return Result<bool>.Failure(text.Error);
        if (Title == text.Value.Title && Description == text.Value.Description && CapturedOn == capturedOn) return Result<bool>.Success(false);
        Title = text.Value.Title; Description = text.Value.Description; CapturedOn = capturedOn;
        Version++; UpdatedAt = at.ToUniversalTime();
        return Result<bool>.Success(true);
    }

    /// <summary>Retira a foto da galeria sem apagar o arquivo, os metadados ou o histórico.</summary>
    public Result<Unit> Invalidate(int expectedVersion, DateTimeOffset at)
    {
        var error = RecordRules.Change(expectedVersion, Version, Validity == RecordValidity.Valid);
        if (error is not null) return Result<Unit>.Failure(error);
        Validity = RecordValidity.Invalid; Version++; UpdatedAt = at.ToUniversalTime();
        return Result<Unit>.Success(new());
    }
}
