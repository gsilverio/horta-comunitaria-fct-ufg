using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Evidence.ListEvidences;

internal sealed record ListEvidencesRequest(string? TargetType, Guid? TargetId, string? Validity, int Page, int PageSize);

internal sealed class ListEvidencesQueryHandler(AppDbContext db, ILogger<ListEvidencesQueryHandler> logger)
{
    public async Task<Result<Page<EvidenceResponse>>> HandleAsync(Guid gardenId, ListEvidencesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Evidence.ListEvidences", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<EvidenceResponse>> Reject(Error error) => process.Complete(Result<Page<EvidenceResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        if (request.TargetId is not null && request.TargetType is null) return Reject(Error.Validation("targetType", "Informe o tipo junto do registro."));
        if (request.TargetId == Guid.Empty) return Reject(Error.Validation("targetId", "Identificador inválido."));
        var query = db.EvidenceFiles.AsNoTracking().Where(x => x.GardenId == gardenId);
        if (request.TargetType is not null)
        {
            var target = EvidenceTargets.Parse(request.TargetType);
            if (!target.IsSuccess) return Reject(target.Error);
            query = request.TargetId is { } id ? EvidenceTargets.WhereTarget(query, target.Value, id) : EvidenceTargets.WhereType(query, target.Value);
        }
        if (validity.Value.Value is { } state) query = query.Where(x => x.Validity == state);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando metadados das fotografias antes da paginação");
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.RecordedAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var items = rows.Select(EvidenceProjection.Map).ToList();
        return process.Complete(Result<Page<EvidenceResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
