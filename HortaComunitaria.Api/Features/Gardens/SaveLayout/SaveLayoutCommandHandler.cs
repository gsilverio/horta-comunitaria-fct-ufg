using System.Security.Cryptography;
using System.Text;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.SaveLayout;

internal sealed record SaveLayoutRequest(Guid OperationId, int ExpectedVersion, LayoutDocument Layout);

internal sealed class SaveLayoutCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<SaveLayoutCommandHandler> logger)
{
    public async Task<Result<LayoutResponse>> HandleAsync(Guid gardenId, SaveLayoutRequest request, CancellationToken token)
    {
        using var process = new ProcessLog(logger, "Gardens.SaveLayout", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<LayoutResponse> Reject(Error error) => process.Complete(Result<LayoutResponse>.Failure(error));
        if (request.OperationId == Guid.Empty || request.ExpectedVersion < 0 || request.Layout is null) return Reject(Error.Validation("layout", "Informe configuração, operação e versão válidas."));
        var error = LayoutGeometry.Validate(request.Layout);
        if (error is not null) return Reject(error);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AuditSnapshots.Capture(request))));
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await WriteLocks.CatalogsAsync(db, token);
        await WriteLocks.GardenAsync(db, gardenId, token);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, token)) return Reject(Error.NotFound());
        var stored = await db.Set<GardenLayout>().SingleOrDefaultAsync(x => x.GardenId == gardenId, token);
        if (stored?.OperationId == request.OperationId)
        {
            if (stored.RequestHash != hash) return Reject(new("layout.operation_conflict", "A tentativa de salvar foi reutilizada com outro conteúdo.", ErrorCategory.Conflict));
            return process.Complete(Result<LayoutResponse>.Success(LayoutProjection.Map(stored)));
        }
        if ((stored?.Version ?? 0) != request.ExpectedVersion) return Reject(new("layout.version_conflict", "Há uma configuração mais recente. Recarregue e revise seu rascunho antes de salvar.", ErrorCategory.Conflict));
        process.Step("CheckingContext", "Validando referências e cadastros do mapa na mesma horta");
        var sectors = new Dictionary<string, IrrigationSector>();
        var elements = request.Layout.Elements.ToArray();
        var actor = await author.ResolveAsync(token);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var today = LocalPeriod.LocalDate(now);
        var reason = ChangeReason.Create("Configuração do mapa interativo").Value;
        foreach (var (element, index) in elements.Select((element, index) => (element, index)).Where(x => x.element.Role == "sector"))
        {
            IrrigationSector sector;
            if (element.EntityId is { } entityId)
            {
                var existing = await db.IrrigationSectors.SingleOrDefaultAsync(x => x.Id == entityId && x.GardenId == gardenId && x.IsActive, token);
                if (existing is null) return Reject(Error.NotFound());
                sector = existing;
            }
            else
            {
                var candidate = IrrigationSector.Create(gardenId, null, element.Name);
                if (!candidate.IsSuccess) return Reject(candidate.Error);
                sector = candidate.Value; db.IrrigationSectors.Add(sector);
                db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "IrrigationSector", sector.Id, actor, now, null, AuditSnapshots.Capture(IrrigationSectorProjection.Map(sector))));
            }
            if (sectors.Values.Any(x => x.Id == sector.Id)) return Reject(Error.Validation("entityId", "Um cadastro de setor não pode representar duas formas diferentes."));
            sectors.Add(element.Id, sector);
            elements[index] = element with { EntityId = sector.Id, Source = "catalog" };
        }
        var bedIds = new HashSet<Guid>();
        foreach (var (element, index) in elements.Select((element, index) => (element, index)).Where(x => x.element.Role == "bed"))
        {
            Bed bed;
            var created = element.EntityId is null;
            if (element.EntityId is { } entityId)
            {
                var existing = await db.Beds.SingleOrDefaultAsync(x => x.Id == entityId && x.GardenId == gardenId && x.IsActive, token);
                if (existing is null) return Reject(Error.NotFound());
                bed = existing;
            }
            else
            {
                var candidate = Bed.Create(gardenId, null, element.Name);
                if (!candidate.IsSuccess) return Reject(candidate.Error);
                bed = candidate.Value; db.Beds.Add(bed);
                db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Bed", bed.Id, actor, now, null, AuditSnapshots.Capture(BedProjection.Map(bed))));
            }
            if (!bedIds.Add(bed.Id)) return Reject(Error.Validation("entityId", "Um cadastro de canteiro não pode representar duas formas diferentes."));
            var sector = sectors[element.SectorId!];
            var contexts = created ? [] : await db.BedSectorAssignments.Where(x => x.BedId == bed.Id).OrderBy(x => x.ValidFrom).ToListAsync(token);
            var current = contexts.LastOrDefault();
            if (current?.IrrigationSectorId != sector.Id)
            {
                // Mudanças de vigência existentes continuam explícitas no cadastro, para não reclassificar fatos.
                if (current is not null) return Reject(new("layout.assignment_conflict", "Um canteiro salvo mudou de setor. Registre a nova vigência no cadastro antes de salvar o mapa.", ErrorCategory.Conflict));
                var before = AuditSnapshots.Capture(BedProjection.Map(bed));
                var assignment = BedSectorAssignment.Create(bed, sector, today, null, contexts);
                if (!assignment.IsSuccess) return Reject(assignment.Error);
                var changed = bed.ChangeContext(bed.Version, reason, now);
                if (!changed.IsSuccess) return Reject(changed.Error);
                db.BedSectorAssignments.Add(assignment.Value);
                db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Bed", bed.Id, actor, now, before, AuditSnapshots.Capture(new { Bed = BedProjection.Map(bed), Assignment = BedSectorAssignmentProjection.Map(assignment.Value) }), reason.Value, AuditOperation.Corrected));
            }
            elements[index] = element with { EntityId = bed.Id, SectorName = sector.Name, Source = "catalog" };
        }
        if (elements.Any(x => x.Role == "reference" && x.EntityId is not null)) return Reject(Error.Validation("entityId", "Referências visuais não são cadastros."));
        var document = request.Layout with { Elements = elements };
        var beforeLayout = stored?.Document;
        var entity = stored ?? GardenLayout.Create(gardenId);
        var saved = entity.Save(request.ExpectedVersion, request.OperationId, hash, AuditSnapshots.Capture(document), actor, now);
        if (!saved.IsSuccess) return Reject(saved.Error);
        if (stored is null) db.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "GardenLayout", gardenId, actor, now, beforeLayout, entity.Document, stored is null ? null : reason.Value, stored is null ? AuditOperation.Created : AuditOperation.Corrected));
        process.Step("Persisting", "Salvando mapa, cadastros e histórico atomicamente");
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return process.Complete(Result<LayoutResponse>.Success(LayoutProjection.Map(entity)));
    }

}
