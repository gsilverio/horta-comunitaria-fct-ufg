using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;

namespace HortaComunitaria.Api.Domain.Irrigation;

/// <summary>Associação do fato ao vínculo histórico; não possui medida de consumo.</summary>
public sealed class IrrigationBed
{
    private IrrigationBed() { }
    /// <summary>Fato principal.</summary>
    public Guid IrrigationId { get; private set; }
    /// <summary>Canteiro atendido.</summary>
    public Guid BedId { get; private set; }
    /// <summary>Horta comum reforçada por FKs compostas.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Setor comum ao evento e ao vínculo.</summary>
    public Guid IrrigationSectorId { get; private set; }
    /// <summary>Vínculo histórico escolhido na data do fato.</summary>
    public Guid BedSectorAssignmentId { get; private set; }

    /// <summary>Preserva o contexto do vínculo validado, sem resolver o setor atual.</summary>
    public static Result<IrrigationBed> Create(Irrigation irrigation, BedSectorAssignment assignment) =>
        assignment.GardenId != irrigation.GardenId || assignment.IrrigationSectorId != irrigation.IrrigationSectorId
        ? Result<IrrigationBed>.Failure(Error.Validation("bedIds", "Vínculo incompatível com o evento."))
        : Result<IrrigationBed>.Success(new()
        {
            IrrigationId = irrigation.Id,
            GardenId = irrigation.GardenId,
            BedId = assignment.BedId,
            IrrigationSectorId = irrigation.IrrigationSectorId,
            BedSectorAssignmentId = assignment.Id
        });

    /// <summary>Corrige o vínculo do mesmo canteiro, preservando a chave composta do filho.</summary>
    internal void Reassign(Irrigation irrigation, BedSectorAssignment assignment)
    {
        if (irrigation.Id != IrrigationId || assignment.BedId != BedId || assignment.GardenId != GardenId ||
            assignment.IrrigationSectorId != irrigation.IrrigationSectorId)
            throw new InvalidOperationException("Agregado tentou aplicar vínculo incompatível ao filho.");
        IrrigationSectorId = assignment.IrrigationSectorId;
        BedSectorAssignmentId = assignment.Id;
    }
}
