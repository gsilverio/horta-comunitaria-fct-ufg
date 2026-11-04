using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListIrrigationSectors;

/// <summary>Somente dados necessários à seleção; não expõe entidades persistidas.</summary>
internal sealed record ListIrrigationSectorsResponse(Guid Id, string Code, string Name);

internal sealed class ListIrrigationSectorsQueryHandler(AppDbContext db, ILogger<ListIrrigationSectorsQueryHandler> logger)
{
    public async Task<Result<Page<ListIrrigationSectorsResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListIrrigationSectors", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<ListIrrigationSectorsResponse>>.Failure(validation.Error));
        if (gardenId == Guid.Empty)
            return process.Complete(Result<Page<ListIrrigationSectorsResponse>>.Failure(Error.Validation("gardenId", "Identificador da horta é obrigatório.")));
        process.Step("CheckingGarden", "Conferindo existência da horta {GardenId}", gardenId);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken))
            return process.Complete(Result<Page<ListIrrigationSectorsResponse>>.Failure(Error.NotFound()));
        process.Step("Querying", "Consultando setores ativos; página {Page}, tamanho {PageSize}", pagination.Page, pagination.PageSize);
        var query = db.IrrigationSectors.AsNoTracking()
            .Where(x => x.IsActive && x.GardenId == gardenId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new ListIrrigationSectorsResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
        return process.Complete(Result<Page<ListIrrigationSectorsResponse>>.Success(
            new Page<ListIrrigationSectorsResponse>(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
