using System.Data;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Irrigation.ListIrrigationHistory;

/// <summary>Histórico de um único fato; snapshots têm contrato de campos e não são texto JSON duplamente codificado.</summary>
internal sealed record IrrigationHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, DateTimeOffset OccurredAt,
    RecordedByResponse Actor, IrrigationSnapshot? Before, IrrigationSnapshot After);

internal sealed class ListIrrigationHistoryQueryHandler(AppDbContext db, ILogger<ListIrrigationHistoryQueryHandler> logger)
{
    public async Task<Result<Page<IrrigationHistoryResponse>>> HandleAsync(Guid gardenId, Guid id, PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Irrigation.ListIrrigationHistory", new Dictionary<string, object?>
        { ["GardenId"] = gardenId, ["IrrigationId"] = id });
        Result<Page<IrrigationHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<IrrigationHistoryResponse>>.Failure(error));
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificadores devem ser não vazios."));
        process.Step("CheckingRecord", "Conferindo irrigação {IrrigationId} no snapshot do histórico", id);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Irrigations.AsNoTracking().AnyAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken))
            return Reject(Error.NotFound());
        process.Step("Querying", "Consultando histórico da irrigação {IrrigationId}; página {Page}", id, pagination.Page);
        var query = db.AuditEntries.AsNoTracking().Where(x => x.GardenId == gardenId && x.EntityType == "Irrigation" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var page = query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize);
        var rows = await (from entry in page
                          join actor in db.Users.AsNoTracking() on entry.UserId equals actor.Id
                          orderby entry.OccurredAt descending, entry.Id descending
                          select new
                          {
                              entry.Id,
                              entry.Operation,
                              entry.Reason,
                              entry.OccurredAt,
                              entry.Before,
                              entry.After,
                              Actor = new RecordedByResponse(actor.Id, actor.DisplayName, actor.IsDemonstration)
                          })
                          .ToListAsync(cancellationToken);
        var items = rows.Select(x => new IrrigationHistoryResponse(x.Id, x.Operation, x.Reason, x.OccurredAt, x.Actor,
            x.Before is null ? null : IrrigationSnapshots.Read(x.Before), IrrigationSnapshots.Read(x.After))).ToArray();
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<IrrigationHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Length);
    }
}
