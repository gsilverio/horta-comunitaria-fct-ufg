using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.ListDryingLots;

internal sealed record ListDryingLotsRequest(string? From, string? To, int Page, int PageSize, string? Validity, string? Status);
internal sealed class ListDryingLotsQueryHandler(AppDbContext db, ILogger<ListDryingLotsQueryHandler> logger)
{
    public async Task<Result<Page<DryingLotResponse>>> HandleAsync(Guid gardenId, ListDryingLotsRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListDryingLots", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<DryingLotResponse>> Reject(Error error) => process.Complete(Result<Page<DryingLotResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error with { Code = "record.invalid_period" });
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        var status = EnumFilter.Parse<DryingStatus>(request.Status, "status");
        if (!status.IsSuccess) return Reject(status.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.DryingLots.AsNoTracking().Include(x => x.Items).AsSplitQuery().Where(x => x.GardenId == gardenId && x.StartedOn >= period.Value.From && x.StartedOn <= period.Value.To && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (status.Value.Value == null || x.Status == status.Value.Value));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.StartedOn).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(DryingLotProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<DryingLotResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
