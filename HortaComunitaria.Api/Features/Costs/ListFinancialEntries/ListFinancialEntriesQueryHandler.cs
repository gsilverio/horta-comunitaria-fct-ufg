using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
namespace HortaComunitaria.Api.Features.Costs.ListFinancialEntries;

internal sealed record ListFinancialEntriesRequest(string? From, string? To, int Page, int PageSize, string? Validity, string? Category, string? Nature, Guid? BedId, Guid? CycleId);
internal sealed class ListFinancialEntriesQueryHandler(AppDbContext db, ILogger<ListFinancialEntriesQueryHandler> logger)
{
    public async Task<Result<Page<FinancialEntryResponse>>> HandleAsync(Guid gardenId, ListFinancialEntriesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Costs.ListFinancialEntries", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<FinancialEntryResponse>> Reject(Error error) => process.Complete(Result<Page<FinancialEntryResponse>>.Failure(error));
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
        var nature = EnumFilter.Parse<FinancialNature>(request.Nature, "nature");
        if (!nature.IsSuccess) return Reject(nature.Error);
        if (request.BedId == Guid.Empty) return Reject(Error.Validation("bedId", "Identificador inválido."));
        if (request.CycleId == Guid.Empty) return Reject(Error.Validation("cycleId", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.FinancialEntries.AsNoTracking().Where(x => x.GardenId == gardenId && x.EntryDate >= period.Value.From && x.EntryDate <= period.Value.To && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (category == null || x.Category == category) && (nature.Value.Value == null || x.Nature == nature.Value.Value) && (request.BedId == null || x.BedId == request.BedId) && (request.CycleId == null || x.CycleId == request.CycleId));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.EntryDate).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(FinancialEntryProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<FinancialEntryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
