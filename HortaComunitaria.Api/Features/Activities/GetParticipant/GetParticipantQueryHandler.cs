using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.GetParticipant;

internal sealed class GetParticipantQueryHandler(AppDbContext db, ILogger<GetParticipantQueryHandler> logger)
{
    public async Task<Result<ParticipantResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.GetParticipant", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<ParticipantResponse> Reject(Error error) => process.Complete(Result<ParticipantResponse>.Failure(error));
        if (id == Guid.Empty || gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe do cadastro preservado");
        var entity = await db.Participants.AsNoTracking().Where(x => x.Id == id && x.GardenId == gardenId).Select(ParticipantProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return process.Complete(entity is null ? Result<ParticipantResponse>.Failure(Error.NotFound()) : Result<ParticipantResponse>.Success(entity));
    }
}
