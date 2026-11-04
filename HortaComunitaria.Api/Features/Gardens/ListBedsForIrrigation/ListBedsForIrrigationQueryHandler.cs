using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListBedsForIrrigation;

/// <summary>Seleção histórica; a data não assume o dia atual.</summary>
internal sealed record ListBedsForIrrigationRequest(string? On, Guid? SectorId, int Page, int PageSize);
/// <summary>Somente dados necessários à seleção; não expõe entidades persistidas.</summary>
internal sealed record ListBedsForIrrigationResponse(Guid Id, string Code, string Name, Guid AssignmentId);

internal sealed class ListBedsForIrrigationQueryHandler(AppDbContext db, ILogger<ListBedsForIrrigationQueryHandler> logger)
{
    public async Task<Result<Page<ListBedsForIrrigationResponse>>> HandleAsync(Guid gardenId, ListBedsForIrrigationRequest request,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListBedsForIrrigation",
            new Dictionary<string, object?> { ["GardenId"] = gardenId, ["SectorId"] = request.SectorId });
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Failure(validation.Error));
        if (!DateOnly.TryParseExact(request.On, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var selectedDate))
            return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Failure(Error.Validation("on", "Data obrigatória no formato YYYY-MM-DD.")));
        if (request.SectorId is null || request.SectorId == Guid.Empty)
            return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Failure(Error.Validation("sectorId", "Setor é obrigatório.")));
        using var dateScope = logger.BeginScope(new Dictionary<string, object?>
        { ["On"] = selectedDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) });
        if (gardenId == Guid.Empty)
            return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Failure(Error.Validation("gardenId", "Identificador da horta é obrigatório.")));
        process.Step("CheckingContext", "Conferindo horta e setor ativos para seleção dos canteiros");
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken))
            return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Failure(Error.NotFound()));

        if (!await db.IrrigationSectors.AsNoTracking().AnyAsync(
            x => x.Id == request.SectorId && x.GardenId == gardenId && x.IsActive, cancellationToken))
            return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Failure(Error.NotFound()));
        process.Step("Querying", "Consultando canteiros com associação vigente em {On}; página {Page}", selectedDate, pagination.Page);
        var query = from bed in db.Beds.AsNoTracking()
                    join assignment in db.BedSectorAssignments.AsNoTracking() on bed.Id equals assignment.BedId
                    where bed.GardenId == gardenId && bed.IsActive &&
                        assignment.GardenId == gardenId && assignment.IrrigationSectorId == request.SectorId &&
                        assignment.ValidFrom <= selectedDate && (assignment.ValidUntil == null || selectedDate < assignment.ValidUntil)
                    select new { bed.Id, bed.Code, bed.Name, AssignmentId = assignment.Id };
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new ListBedsForIrrigationResponse(x.Id, x.Code, x.Name, x.AssignmentId))
            .ToListAsync(cancellationToken);
        return process.Complete(Result<Page<ListBedsForIrrigationResponse>>.Success(
            new Page<ListBedsForIrrigationResponse>(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
