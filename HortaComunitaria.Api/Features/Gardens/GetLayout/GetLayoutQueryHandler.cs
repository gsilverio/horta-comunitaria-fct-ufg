using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HortaComunitaria.Api.Infrastructure.Observability;

namespace HortaComunitaria.Api.Features.Gardens.GetLayout;

internal sealed class GetLayoutQueryHandler(AppDbContext db, ILogger<GetLayoutQueryHandler> logger)
{
    public async Task<Result<LayoutResponse>> HandleAsync(Guid gardenId, CancellationToken token)
    {
        using var process = new ProcessLog(logger, "Gardens.GetLayout", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        process.Step("Querying", "Consultando configuração confirmada do mapa");
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, token)) return process.Complete(Result<LayoutResponse>.Failure(Error.NotFound()));
        var layout = await db.Set<GardenLayout>().AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId, token);
        return process.Complete(Result<LayoutResponse>.Success(layout is null ? new(0, null, null) : LayoutProjection.Map(layout)));
    }
}
