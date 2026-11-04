using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.ListParticipantCatalog;

internal sealed class ListParticipantCatalogQueryHandler(AppDbContext db, ILogger<ListParticipantCatalogQueryHandler> logger)
{
    public async Task<Result<Page<ParticipantResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.ListParticipantCatalog", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<ParticipantResponse>>.Failure(validation.Error));
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken))
            return process.Complete(Result<Page<ParticipantResponse>>.Failure(Error.NotFound()));
        process.Step("Querying", "Consultando catálogo administrativo de participantes");
        var query = db.Participants.AsNoTracking().Where(x => x.GardenId == gardenId && (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(ParticipantProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<ParticipantResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}

