using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Files;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Evidence.GetEvidenceContent;

/// <summary>Conteúdo aberto para streaming; o chamador descarta o stream.</summary>
internal sealed record EvidenceContent(Stream Content, string ContentType, string FileName, string Sha256);

internal sealed class GetEvidenceContentQueryHandler(AppDbContext db, IFileStorage storage, ILogger<GetEvidenceContentQueryHandler> logger)
{
    public async Task<Result<EvidenceContent>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Evidence.GetEvidenceContent", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        if (gardenId == Guid.Empty || id == Guid.Empty) return process.Complete(Result<EvidenceContent>.Failure(Error.Validation("id", "Identificador inválido.")));
        process.Step("Querying", "Consultando referência da fotografia na horta");
        var file = await db.EvidenceFiles.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id)
            .Select(x => new { x.StorageKey, x.ContentType, x.OriginalFileName, x.Sha256 }).SingleOrDefaultAsync(cancellationToken);
        if (file is null) return process.Complete(Result<EvidenceContent>.Failure(Error.NotFound()));
        var stream = storage.OpenRead(file.StorageKey);
        if (stream is null) return process.Complete(Result<EvidenceContent>.Failure(new("evidence.file_unavailable", "O arquivo da fotografia não está disponível no armazenamento.", ErrorCategory.NotFound)));
        return process.Complete(Result<EvidenceContent>.Success(new(stream, file.ContentType, file.OriginalFileName, file.Sha256)));
    }
}
