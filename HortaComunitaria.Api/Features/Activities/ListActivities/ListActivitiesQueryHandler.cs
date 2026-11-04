using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.ListActivities;

internal sealed record ListActivitiesRequest(string? From, string? To, int Page, int PageSize, string? Validity, Guid? BedId, Guid? SectorId, Guid? CycleId, Guid? OccurrenceId, string? Category, bool? IsSocial, bool? IsEducational, Guid? InstitutionId);
internal sealed class ListActivitiesQueryHandler(AppDbContext db, ILogger<ListActivitiesQueryHandler> logger)
{
    public async Task<Result<Page<ActivityResponse>>> HandleAsync(Guid gardenId, ListActivitiesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.ListActivities", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<ActivityResponse>> Reject(Error error) => process.Complete(Result<Page<ActivityResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error with { Code = "record.invalid_period" });
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        if (request.BedId == Guid.Empty) return Reject(Error.Validation("bedId", "Identificador inválido."));
        if (request.SectorId == Guid.Empty) return Reject(Error.Validation("sectorId", "Identificador inválido."));
        if (request.CycleId == Guid.Empty) return Reject(Error.Validation("cycleId", "Identificador inválido."));
        if (request.OccurrenceId == Guid.Empty) return Reject(Error.Validation("occurrenceId", "Identificador inválido."));
        var category = RecordRules.Optional(request.Category);
        var categoryError = RecordRules.Text(category, "category", 120);
        if (categoryError is not null) return Reject(categoryError);
        if (request.InstitutionId == Guid.Empty) return Reject(Error.Validation("institutionId", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.Activities.AsNoTracking().Include(x => x.Participants).Include(x => x.InputUsages).Include(x => x.Institutions).AsSplitQuery().Where(x => x.GardenId == gardenId && x.PerformedOn >= period.Value.From && x.PerformedOn <= period.Value.To && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (request.BedId == null || x.BedId == request.BedId) && (request.SectorId == null || x.SectorId == request.SectorId) && (request.CycleId == null || x.CycleId == request.CycleId) && (request.OccurrenceId == null || x.OccurrenceId == request.OccurrenceId) && (category == null || x.Category == category) && (request.IsSocial == null || x.IsSocial == request.IsSocial) && (request.IsEducational == null || x.IsEducational == request.IsEducational) && (request.InstitutionId == null || x.Institutions.Any(i => i.InstitutionId == request.InstitutionId)));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.PerformedOn).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(ActivityProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<ActivityResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
