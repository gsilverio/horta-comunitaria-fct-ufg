using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
namespace HortaComunitaria.Api.Features.Occurrences.ListOccurrences;

internal sealed record ListOccurrencesRequest(string? From, string? To, int Page, int PageSize, string? Validity, string? Category, string? Status, Guid? BedId, Guid? SectorId, Guid? CycleId, Guid? IrrigationId);
internal sealed class ListOccurrencesQueryHandler(AppDbContext db, ILogger<ListOccurrencesQueryHandler> logger)
{
    public async Task<Result<Page<OccurrenceResponse>>> HandleAsync(Guid gardenId, ListOccurrencesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Occurrences.ListOccurrences", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<OccurrenceResponse>> Reject(Error error) => process.Complete(Result<Page<OccurrenceResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error with { Code = "record.invalid_period" });
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        var category = RecordRules.Optional(request.Category);
        var categoryError = RecordRules.Text(category, "category", 120);
        if (categoryError is not null) return Reject(categoryError);
        var status = EnumFilter.Parse<OccurrenceStatus>(request.Status, "status");
        if (!status.IsSuccess) return Reject(status.Error);
        if (request.BedId == Guid.Empty) return Reject(Error.Validation("bedId", "Identificador inválido."));
        if (request.SectorId == Guid.Empty) return Reject(Error.Validation("sectorId", "Identificador inválido."));
        if (request.CycleId == Guid.Empty) return Reject(Error.Validation("cycleId", "Identificador inválido."));
        if (request.IrrigationId == Guid.Empty) return Reject(Error.Validation("irrigationId", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.Occurrences.AsNoTracking().Where(x => x.GardenId == gardenId && x.OpenedAt >= period.Value.StartUtc && x.OpenedAt < period.Value.EndUtc && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (category == null || x.Category == category) && (status.Value.Value == null || x.Status == status.Value.Value) && (request.BedId == null || x.BedId == request.BedId) && (request.SectorId == null || x.SectorId == request.SectorId) && (request.CycleId == null || x.CycleId == request.CycleId) && (request.IrrigationId == null || x.IrrigationId == request.IrrigationId));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OpenedAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(OccurrenceProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<OccurrenceResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
