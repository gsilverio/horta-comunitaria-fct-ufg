using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HortaComunitaria.Api.Features.Activities.CreateParticipant;

internal sealed record CreateParticipantRequest([property: JsonRequired] string DisplayName, string? Category);
internal sealed class CreateParticipantCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CreateParticipantCommandHandler> logger)
{
    public async Task<Result<ParticipantResponse>> HandleAsync(Guid gardenId, CreateParticipantRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.CreateParticipant", new Dictionary<string, object?> { ["GardenId"] = gardenId, });
        Result<ParticipantResponse> Reject(Error error) => process.Complete(Result<ParticipantResponse>.Failure(error));
        var candidate = Participant.Create(gardenId, request.DisplayName, category: request.Category);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        process.Step("CheckingContext", "Conferindo contexto e chave do cadastro");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if (await db.Participants.AnyAsync(x => false, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        db.Participants.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Participant", entity.Id, await author.ResolveAsync(cancellationToken), now, null, AuditSnapshots.Capture(ParticipantProjection.Map(entity))));
        process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateParticipantConfirmed", "Created", "Operação CreateParticipant confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<ParticipantResponse>.Success(ParticipantProjection.Map(entity)));
    }
}
