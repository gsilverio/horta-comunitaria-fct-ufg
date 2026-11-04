using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.ListParticipants;

/// <summary>Somente dados necessários à seleção; não expõe entidades persistidas.</summary>
internal sealed record ListParticipantsResponse(Guid Id, string DisplayName);

internal sealed class ListParticipantsQueryHandler(AppDbContext db, ILogger<ListParticipantsQueryHandler> logger)
{
    public async Task<Result<Page<ListParticipantsResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.ListParticipants", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<ListParticipantsResponse>>.Failure(validation.Error));
        if (gardenId == Guid.Empty)
            return process.Complete(Result<Page<ListParticipantsResponse>>.Failure(Error.Validation("gardenId", "Identificador da horta é obrigatório.")));
        process.Step("CheckingGarden", "Conferindo existência da horta {GardenId}", gardenId);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken))
            return process.Complete(Result<Page<ListParticipantsResponse>>.Failure(Error.NotFound()));
        process.Step("Querying", "Consultando participantes ativos; página {Page}, tamanho {PageSize}", pagination.Page, pagination.PageSize);
        var query = db.Participants.AsNoTracking()
            .Where(x => x.IsActive && x.GardenId == gardenId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new ListParticipantsResponse(x.Id, x.DisplayName))
            .ToListAsync(cancellationToken);
        return process.Complete(Result<Page<ListParticipantsResponse>>.Success(
            new Page<ListParticipantsResponse>(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
