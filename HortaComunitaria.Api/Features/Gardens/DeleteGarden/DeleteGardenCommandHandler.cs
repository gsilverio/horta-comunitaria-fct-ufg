using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.DeleteGarden;

/// <summary>Confirmação da exclusão definitiva.</summary>
/// <param name="ExpectedVersion">Versão observada do cadastro da horta.</param>
/// <param name="Reason">Motivo registrado no rastro da exclusão.</param>
internal sealed record DeleteGardenRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);

/// <summary>
/// Exclui fisicamente uma horta que ainda não tem fatos de campo: ciclos, lotes de material, irrigações, atividades,
/// ocorrências, custos, secagens, medições ambientais ou fotografias. Remove junto a estrutura (setores, canteiros,
/// vigências, configurações, pontos de coleta, participantes, mapa) e o histórico desses cadastros. Hortas com fatos
/// continuam apenas inativáveis, para preservar histórico e indicadores. Um rastro da exclusão fica no histórico global.
/// </summary>
internal sealed class DeleteGardenCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<DeleteGardenCommandHandler> logger)
{
    public async Task<Result<Unit>> HandleAsync(Guid id, DeleteGardenRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.DeleteGarden", new Dictionary<string, object?> { ["ResourceId"] = id });
        Result<Unit> Reject(Error error) => process.Complete(Result<Unit>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);

        process.Step("LoadingRecord", "Bloqueando a horta e conferindo a versão esperada");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // O lock exclusivo da horta impede que um fato novo seja gravado enquanto a ausência de fatos é conferida.
        await WriteLocks.GardenAsync(db, id, cancellationToken);
        var garden = await db.Gardens.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (garden is null) return Reject(Error.NotFound());
        if (garden.Version != request.ExpectedVersion)
            return Reject(new("record.version_conflict", "Cadastro alterado; recarregue e revise.", ErrorCategory.Conflict));

        process.Step("CheckingFacts", "Conferindo se a horta já possui fatos de campo");
        if (await HasFieldFactsAsync(id, cancellationToken))
            return Reject(new("garden.has_records", "Esta horta já possui registros de campo. Para preservar o histórico e os indicadores, ela só pode ser inativada.", ErrorCategory.Conflict));

        var actor = await author.ResolveAsync(cancellationToken);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var before = AuditSnapshots.Capture(GardenProjection.Map(garden));

        process.Step("Deleting", "Excluindo estrutura, cadastros e histórico da horta");
        // Ordem respeita as FKs restritivas: dependentes antes dos cadastros referenciados.
        await db.Set<GardenLayout>().Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.SamplingPoints.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.BedConfigurations.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.BedSectorAssignments.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Beds.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.IrrigationSectors.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Participants.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.AuditEntries.Where(x => x.GardenId == id).ExecuteDeleteAsync(cancellationToken);
        await db.Gardens.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);

        // Rastro global (sem vínculo com a horta removida): quem excluiu, quando, por quê e o último estado do cadastro.
        db.AuditEntries.Add(AuditSnapshots.Create(null, "Garden", id, actor, now, before, before,
            "Exclusão definitiva: " + reason.Value.Value, AuditOperation.Invalidated));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("GardenDeleted", "Deleted", "Horta {ResourceId} excluída definitivamente", id);
        return process.Complete(Result<Unit>.Success(new()));
    }

    private async Task<bool> HasFieldFactsAsync(Guid id, CancellationToken token) =>
        await db.CultivationCycles.AnyAsync(x => x.GardenId == id, token) ||
        await db.PropagationLots.AnyAsync(x => x.GardenId == id, token) ||
        await db.Irrigations.AnyAsync(x => x.GardenId == id, token) ||
        await db.Activities.AnyAsync(x => x.GardenId == id, token) ||
        await db.Occurrences.AnyAsync(x => x.GardenId == id, token) ||
        await db.FinancialEntries.AnyAsync(x => x.GardenId == id, token) ||
        await db.DryingLots.AnyAsync(x => x.GardenId == id, token) ||
        await db.EnvironmentalMeasurements.AnyAsync(x => x.GardenId == id, token) ||
        await db.EvidenceFiles.AnyAsync(x => x.GardenId == id, token);
}
