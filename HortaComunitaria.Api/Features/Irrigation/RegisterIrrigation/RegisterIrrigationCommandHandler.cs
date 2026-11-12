using System.Text.Json;
using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using IrrigationRecord = HortaComunitaria.Api.Domain.Irrigation.Irrigation;

namespace HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;

/// <summary>Formulário do protótipo; autoria e metadados são atribuídos pelo servidor.</summary>
internal sealed record RegisterIrrigationRequest(
    [property: JsonRequired] Guid OperationId,
    [property: JsonRequired] Guid SectorId,
    [property: JsonRequired] Guid[] BedIds,
    [property: JsonRequired] DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    decimal? ActiveDurationMinutes,
    decimal? VolumeLiters,
    decimal? FlowRateLitersPerMinute,
    VolumeSource? VolumeSource,
    string? VolumeMethod,
    [property: JsonRequired] IrrigationStatus Status,
    Guid? ResponsibleParticipantId,
    string? Notes);

internal sealed record RegisterIrrigationResponse(bool Created, IrrigationDetailResponse Detail);

internal sealed class RegisterIrrigationCommandHandler(AppDbContext db, CurrentAuthor author,
    TimeProvider timeProvider, ILogger<RegisterIrrigationCommandHandler> logger)
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public async Task<Result<RegisterIrrigationResponse>> HandleAsync(Guid gardenId, RegisterIrrigationRequest request,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Irrigation.RegisterIrrigation",
            new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId, ["SectorId"] = request.SectorId });
        Result<RegisterIrrigationResponse> Reject(Error error) => process.Complete(Result<RegisterIrrigationResponse>.Failure(error));
        process.Step("ValidatingRequest", "Validando dados do registro de irrigação");
        if (request.OperationId == Guid.Empty)
            return Reject(Error.Validation("id", "Identificadores devem ser UUIDs não vazios."));
        var identifiers = IrrigationContextResolver.ValidateIds(gardenId, request.SectorId, request.BedIds, request.ResponsibleParticipantId);
        if (!identifiers.IsSuccess) return Reject(identifiers.Error);
        var values = IrrigationValues.Create(request.StartedAt, request.EndedAt, request.ActiveDurationMinutes,
            request.VolumeLiters, request.FlowRateLitersPerMinute, request.VolumeSource, request.VolumeMethod, request.Status, request.Notes);
        if (!values.IsSuccess) return Reject(values.Error);
        var normalized = values.Value;
        var payload = JsonSerializer.Serialize(new
        {
            request.SectorId,
            BedIds = request.BedIds.Order().ToArray(),
            normalized.StartedAt,
            normalized.EndedAt,
            normalized.ActiveDurationMinutes,
            normalized.VolumeLiters,
            normalized.FlowRateLitersPerMinute,
            normalized.VolumeSource,
            normalized.VolumeMethod,
            normalized.Status,
            request.ResponsibleParticipantId,
            normalized.Notes
        }, PayloadOptions);
        process.Step("CheckingOperation", "Verificando se a operação {OperationId} já foi registrada", request.OperationId);
        var existing = await FindOperationAsync(gardenId, request.OperationId, cancellationToken);
        if (existing is not null) return process.Complete(await ReplayAsync(existing, payload, process, cancellationToken));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken, shared: true);
        await WriteLocks.BedsAsync(db, gardenId, request.BedIds, cancellationToken, shared: true);
        process.Step("ResolvingContext", "Conferindo setor e vínculos históricos dos {BedCount} canteiros selecionados", request.BedIds.Length);
        var assignments = await IrrigationContextResolver.ResolveAsync(db, gardenId, request.SectorId, request.BedIds,
            request.ResponsibleParticipantId, normalized.StartedAt, cancellationToken);
        if (!assignments.IsSuccess) return Reject(assignments.Error);
        process.Step("ResolvingAuthor", "Resolvendo autoria técnica do registro");
        var created = IrrigationRecord.Create(gardenId, request.SectorId, request.OperationId, normalized,
            request.ResponsibleParticipantId, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(timeProvider.GetUtcNow()), payload, assignments.Value);
        if (!created.IsSuccess) return Reject(created.Error);
        var irrigation = created.Value;
        var snapshot = IrrigationSnapshots.Capture(irrigation);
        db.Irrigations.Add(irrigation);
        db.AuditEntries.Add(AuditEntry.CreateInclusion(irrigation, snapshot).Value);
        process.Step("Persisting", "Persistindo irrigação {IrrigationId} e histórico na mesma transação", irrigation.Id);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_irrigations_garden_operation" })
        {
            // SaveChanges reverte sua transação; o vencedor já confirmou o fato e o histórico.
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var winner = await FindOperationAsync(gardenId, request.OperationId, cancellationToken)
                ?? throw new InvalidOperationException("Operação concorrente confirmada não foi localizada.");
            return process.Complete(await ReplayAsync(winner, payload, process, cancellationToken));
        }
        await transaction.CommitAsync(cancellationToken);
        process.Step("LoadingResult", "Consultando irrigação {IrrigationId} após confirmação da gravação", irrigation.Id);
        var detail = await IrrigationDetailProjection.LoadAsync(db, gardenId, irrigation.Id, cancellationToken)
            ?? throw new InvalidOperationException("Irrigação confirmada não foi localizada.");
        process.Event("IrrigationRegistered", "Created", "Irrigação {IrrigationId} registrada com histórico e {BedCount} canteiros",
            irrigation.Id, request.BedIds.Length);
        return process.Complete(Result<RegisterIrrigationResponse>.Success(new(true, detail)));
    }

    private Task<IrrigationRecord?> FindOperationAsync(Guid gardenId, Guid operationId, CancellationToken cancellationToken) =>
        db.Irrigations.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == operationId, cancellationToken);

    private async Task<Result<RegisterIrrigationResponse>> ReplayAsync(IrrigationRecord existing, string payload, ProcessLog process,
        CancellationToken cancellationToken)
    {
        process.Step("ReplayingOperation", "Conferindo o conteúdo da tentativa e recuperando irrigação {IrrigationId}", existing.Id);
        using var original = JsonDocument.Parse(existing.CreationPayload);
        using var candidate = JsonDocument.Parse(payload);
        if (!JsonElement.DeepEquals(original.RootElement, candidate.RootElement))
            return Result<RegisterIrrigationResponse>.Failure(new("irrigation.operation_conflict",
                "Chave de operação já utilizada com outro conteúdo.", ErrorCategory.Conflict));
        var detail = await IrrigationDetailProjection.LoadAsync(db, existing.GardenId, existing.Id, cancellationToken)
            ?? throw new InvalidOperationException("Irrigação repetida não foi localizada.");
        process.Event("IrrigationReplayed", "Replayed", "Irrigação {IrrigationId} recuperada; a repetição não criou outro registro", existing.Id);
        return Result<RegisterIrrigationResponse>.Success(new(false, detail));
    }
}
