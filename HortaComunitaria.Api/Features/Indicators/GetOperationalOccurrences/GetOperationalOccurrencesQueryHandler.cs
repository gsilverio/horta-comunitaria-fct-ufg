using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetOperationalOccurrences;

internal sealed record GetOperationalOccurrencesRequest(string? From, string? To, int Page, int PageSize, string? Category, string? Status);

internal sealed record OccurrenceOrigin(Guid Id, DateTimeOffset OpenedAt, string Category, OccurrenceStatus Status);
internal sealed record GetOperationalOccurrencesResponse(DateOnly From, DateOnly To, string TimeZone, int Count, int OpenCount, int InProgressCount, int ResolvedCount, IndicatorAvailability Availability, Page<OccurrenceOrigin> Origins);

internal sealed class GetOperationalOccurrencesQueryHandler(AppDbContext db, ILogger<GetOperationalOccurrencesQueryHandler> logger)
{
    public async Task<Result<GetOperationalOccurrencesResponse>> HandleAsync(Guid gardenId, GetOperationalOccurrencesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetOperationalOccurrences", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetOperationalOccurrencesResponse> Reject(Error error) => process.Complete(Result<GetOperationalOccurrencesResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        var status = EnumFilter.Parse<OccurrenceStatus>(request.Status, "status");
        if (!status.IsSuccess) return Reject(status.Error);
        var query = db.Occurrences.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.OpenedAt >= range.StartUtc && x.OpenedAt < range.EndUtc && (request.Category == null || x.Category == request.Category.Trim()) && (status.Value.Value == null || x.Status == status.Value.Value));
        var count = await query.CountAsync(cancellationToken);
        var open = await query.CountAsync(x => x.Status == OccurrenceStatus.Open, cancellationToken);
        var ongoing = await query.CountAsync(x => x.Status == OccurrenceStatus.InProgress, cancellationToken);
        var items = await query.OrderByDescending(x => x.OpenedAt).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new OccurrenceOrigin(x.Id, x.OpenedAt, x.Category, x.Status)).ToListAsync(cancellationToken);
        var response = new GetOperationalOccurrencesResponse(range.From, range.To, LocalPeriod.TimeZoneId, count, open, ongoing, count - open - ongoing, IndicatorCoverage.Availability(count, count), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetOperationalOccurrencesResponse>.Success(response), items.Count);
    }
}
