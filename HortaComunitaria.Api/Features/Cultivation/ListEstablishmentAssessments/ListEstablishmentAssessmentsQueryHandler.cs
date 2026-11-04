using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.ListEstablishmentAssessments;

internal sealed record ListEstablishmentAssessmentsRequest(string? From, string? To, int Page, int PageSize, string? Validity, Guid? PlantingId, string? Method, Guid? CycleId, Guid? SpeciesId);
internal sealed class ListEstablishmentAssessmentsQueryHandler(AppDbContext db, ILogger<ListEstablishmentAssessmentsQueryHandler> logger)
{
    public async Task<Result<Page<EstablishmentAssessmentResponse>>> HandleAsync(Guid gardenId, ListEstablishmentAssessmentsRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListEstablishmentAssessments", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<EstablishmentAssessmentResponse>> Reject(Error error) => process.Complete(Result<Page<EstablishmentAssessmentResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error with { Code = "record.invalid_period" });
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        if (request.PlantingId == Guid.Empty) return Reject(Error.Validation("plantingId", "Identificador inválido."));
        var method = RecordRules.Optional(request.Method);
        var methodError = RecordRules.Text(method, "method", 200);
        if (methodError is not null) return Reject(methodError);
        if (request.CycleId == Guid.Empty) return Reject(Error.Validation("cycleId", "Identificador inválido."));
        if (request.SpeciesId == Guid.Empty) return Reject(Error.Validation("speciesId", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.EstablishmentAssessments.AsNoTracking().Where(x => x.GardenId == gardenId && x.AssessedOn >= period.Value.From && x.AssessedOn <= period.Value.To && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (request.PlantingId == null || x.PlantingId == request.PlantingId) && (method == null || x.Method == method) && db.Plantings.Any(p => p.Id == x.PlantingId && (request.CycleId == null || p.CycleId == request.CycleId) && (request.SpeciesId == null || db.CultivationCycles.Any(c => c.Id == p.CycleId && c.SpeciesId == request.SpeciesId))));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.AssessedOn).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(EstablishmentAssessmentProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<EstablishmentAssessmentResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
