using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.Gardens;

/// <summary>Vínculo histórico de canteiro/setor da mesma horta no intervalo [início, fim).</summary>
public sealed class BedSectorAssignment
{
    private BedSectorAssignment() { }
    /// <summary>Identidade estável do vínculo histórico.</summary>
    public Guid Id { get; private set; }
    /// <summary>Contexto comum reforçado por FKs compostas.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Canteiro atendido.</summary>
    public Guid BedId { get; private set; }
    /// <summary>Setor vigente.</summary>
    public Guid IrrigationSectorId { get; private set; }
    /// <summary>Primeira data local incluída.</summary>
    public DateOnly ValidFrom { get; private set; }
    /// <summary>Primeira data local excluída, ou intervalo aberto.</summary>
    public DateOnly? ValidUntil { get; private set; }

    /// <summary>Encerra uma vigência aberta, preservando sua identidade e início.</summary>
    public Result<Unit> Close(DateOnly until)
    {
        if (ValidUntil is not null || until <= ValidFrom)
            return Result<Unit>.Failure(Error.Validation("validFrom", "Sucessão deve começar após o início da vigência aberta."));
        ValidUntil = until;
        return Result<Unit>.Success(new());
    }

    /// <summary>Confere contexto, datas e sobreposição com os vínculos existentes do canteiro.</summary>
    public static Result<BedSectorAssignment> Create(Bed bed, IrrigationSector sector,
        DateOnly validFrom, DateOnly? validUntil, IReadOnlyCollection<BedSectorAssignment> existing,
        Guid? id = null)
    {
        if (bed.GardenId != sector.GardenId)
            return Result<BedSectorAssignment>.Failure(Error.Validation("sectorId", "Setor e canteiro devem pertencer à mesma horta."));
        if (validUntil <= validFrom)
            return Result<BedSectorAssignment>.Failure(Error.Validation("validUntil", "Fim deve ser posterior ao início."));
        if (existing.Any(x => x.BedId == bed.Id &&
            (validUntil is null || x.ValidFrom < validUntil) &&
            (x.ValidUntil is null || validFrom < x.ValidUntil)))
            return Result<BedSectorAssignment>.Failure(Error.Validation("validFrom", "Vigência sobrepõe outro vínculo do canteiro."));
        if (id == Guid.Empty) return Result<BedSectorAssignment>.Failure(Error.Validation("id", "Identificador inválido."));
        return Result<BedSectorAssignment>.Success(new BedSectorAssignment
        {
            Id = id ?? Guid.CreateVersion7(),
            GardenId = bed.GardenId,
            BedId = bed.Id,
            IrrigationSectorId = sector.Id,
            ValidFrom = validFrom,
            ValidUntil = validUntil
        });
    }
}
