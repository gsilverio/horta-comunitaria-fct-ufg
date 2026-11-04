using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
namespace HortaComunitaria.Api.Domain.Gardens;
/// <summary>Área efetiva e dimensões do canteiro no intervalo diário [início, fim).</summary>
public sealed class BedConfiguration
{
    private BedConfiguration() { }
    /// <summary>Identidade da configuração histórica.</summary>
    public Guid Id { get; private set; }
    /// <summary>Contexto reforçado por FK composta.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Canteiro configurado.</summary>
    public Guid BedId { get; private set; }
    /// <summary>Primeira data incluída.</summary>
    public DateOnly ValidFrom { get; private set; }
    /// <summary>Primeira data excluída.</summary>
    public DateOnly? ValidUntil { get; private set; }
    /// <summary>Comprimento conhecido em metros.</summary>
    public decimal? LengthMeters { get; private set; }
    /// <summary>Largura conhecida em metros.</summary>
    public decimal? WidthMeters { get; private set; }
    /// <summary>Área cultivável informada, sem obrigar formato retangular.</summary>
    public decimal CultivableAreaM2 { get; private set; }
    /// <summary>Cria uma configuração válida sem sobreposição.</summary>
    public static Result<BedConfiguration> Create(Guid gardenId, Guid bedId, DateOnly validFrom,
        decimal? lengthMeters, decimal? widthMeters, decimal cultivableAreaM2)
    {
        var error = RecordRules.Number(lengthMeters, "lengthMeters", positive: true) ??
            RecordRules.Number(widthMeters, "widthMeters", positive: true) ??
            RecordRules.Number(cultivableAreaM2, "cultivableAreaM2", positive: true);
        if (error is not null) return Result<BedConfiguration>.Failure(error);
        if (gardenId == Guid.Empty || bedId == Guid.Empty || validFrom == default)
            return Result<BedConfiguration>.Failure(Error.Validation("id", "Informe referências e início válidos."));
        return Result<BedConfiguration>.Success(new()
        {
            Id = Guid.CreateVersion7(),
            GardenId = gardenId,
            BedId = bedId,
            ValidFrom = validFrom,
            LengthMeters = lengthMeters,
            WidthMeters = widthMeters,
            CultivableAreaM2 = cultivableAreaM2
        });
    }
    /// <summary>Encerra a configuração por sucessão sem modificar medidas anteriores.</summary>
    public Result<Unit> Close(DateOnly until)
    {
        if (ValidUntil is not null || until <= ValidFrom)
            return Result<Unit>.Failure(Error.Validation("validFrom", "Sucessão deve começar após a configuração aberta."));
        ValidUntil = until;
        return Result<Unit>.Success(new());
    }
}
