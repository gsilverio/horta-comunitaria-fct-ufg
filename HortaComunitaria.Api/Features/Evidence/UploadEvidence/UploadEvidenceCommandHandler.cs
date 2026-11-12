using System.Security.Cryptography;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Evidence;
using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Files;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HortaComunitaria.Api.Features.Evidence.UploadEvidence;

/// <summary>Campos do formulário multipart, já separados do arquivo.</summary>
internal sealed record UploadEvidenceRequest(string? OperationId, string? TargetType, string? TargetId, string? Title, string? Description, string? CapturedOn);

/// <summary>Arquivo recebido, sem depender do tipo HTTP no handler.</summary>
internal sealed record UploadedFile(string FileName, long Length, Func<Stream> Open);

internal sealed class UploadEvidenceCommandHandler(AppDbContext db, IFileStorage storage, IOptions<FileStorageOptions> options, CurrentAuthor author,
    TimeProvider clock, ILogger<UploadEvidenceCommandHandler> logger)
{
    public async Task<Result<CreatedEvidenceResponse>> HandleAsync(Guid gardenId, UploadEvidenceRequest request, UploadedFile? file, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Evidence.UploadEvidence", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<CreatedEvidenceResponse> Reject(Error error) => process.Complete(Result<CreatedEvidenceResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        if (!Guid.TryParse(request.OperationId, out var operationId) || operationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var target = EvidenceTargets.Parse(request.TargetType);
        if (!target.IsSuccess) return Reject(target.Error);
        if (!Guid.TryParse(request.TargetId, out var targetId) || targetId == Guid.Empty) return Reject(Error.Validation("targetId", "Identificador do registro inválido."));
        DateOnly? capturedOn = null;
        if (!string.IsNullOrWhiteSpace(request.CapturedOn))
        {
            if (!DateOnly.TryParseExact(request.CapturedOn, "yyyy-MM-dd", out var date)) return Reject(Error.Validation("capturedOn", "Use a data no formato AAAA-MM-DD."));
            capturedOn = date;
        }
        var text = EvidenceFile.ValidateText(request.Title, request.Description);
        if (!text.IsSuccess) return Reject(text.Error);
        if (file is null || file.Length <= 0) return Reject(Error.Validation("file", "Selecione uma fotografia."));
        var maximum = options.Value.MaxFileBytes;
        if (file.Length > maximum) return Reject(Error.Validation("file", $"A fotografia excede o limite de {maximum / (1024 * 1024)} MB."));

        process.Step("ReadingFile", "Lendo e conferindo o tipo da fotografia");
        byte[] content;
        await using (var input = file.Open())
        using (var buffer = new MemoryStream((int)file.Length))
        {
            await input.CopyToAsync(buffer, cancellationToken);
            if (buffer.Length > maximum) return Reject(Error.Validation("file", "A fotografia excede o limite configurado."));
            content = buffer.ToArray();
        }
        var kind = ImageSignature.Detect(content);
        if (kind is null) return Reject(new("evidence.unsupported_file", "Envie uma imagem JPEG, PNG ou WebP.", ErrorCategory.Validation, new Dictionary<string, string[]> { ["file"] = ["Envie uma imagem JPEG, PNG ou WebP."] }));
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));

        var existing = await db.EvidenceFiles.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == operationId, cancellationToken);
        if (existing is not null) return Replay(existing);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo o registro vinculado na mesma horta");
        var attachable = await EvidenceTargets.EnsureAttachableAsync(db, gardenId, target.Value, targetId, cancellationToken);
        if (!attachable.IsSuccess) return Reject(attachable.Error);

        // O arquivo é gravado antes da transação; se a confirmação não ocorrer, o objeto é removido.
        process.Step("StoringFile", "Gravando a fotografia no armazenamento de arquivos");
        var storageKey = await storage.SaveAsync(content, kind.Value.Extension, cancellationToken);
        var committed = false;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
            existing = await db.EvidenceFiles.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == operationId, cancellationToken);
            if (existing is not null) return Replay(existing);
            attachable = await EvidenceTargets.EnsureAttachableAsync(db, gardenId, target.Value, targetId, cancellationToken);
            if (!attachable.IsSuccess) return Reject(attachable.Error);
            var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
            var actor = await author.ResolveAsync(cancellationToken);
            var candidate = EvidenceFile.Create(gardenId, operationId, target.Value, targetId, text.Value.Title, text.Value.Description, capturedOn,
                storageKey, file.FileName, kind.Value.ContentType, content.LongLength, sha256, actor, now);
            if (!candidate.IsSuccess) return Reject(candidate.Error);
            var entity = candidate.Value;
            db.EvidenceFiles.Add(entity);
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "EvidenceFile", entity.Id, actor, now, null, AuditSnapshots.Capture(EvidenceProjection.Map(entity))));
            process.Step("Persisting", "Confirmando metadados e auditoria na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            committed = true;
            process.Event("UploadEvidenceConfirmed", "Created", "Fotografia {ResourceId} vinculada ao registro {TargetId}", entity.Id, targetId);
            return process.Complete(Result<CreatedEvidenceResponse>.Success(new(true, EvidenceProjection.Map(entity))));
        }
        finally
        {
            if (!committed) await storage.DeleteAsync(storageKey, CancellationToken.None);
        }

        Result<CreatedEvidenceResponse> Replay(EvidenceFile previous) =>
            previous.Sha256 == sha256 && previous.Target == target.Value && previous.TargetId == targetId
                ? process.Complete(Result<CreatedEvidenceResponse>.Success(new(false, EvidenceProjection.Map(previous))))
                : Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
    }
}
