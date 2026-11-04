using System.Globalization;
using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Infrastructure.Time;

internal sealed record LocalPeriod(DateOnly From, DateOnly To, DateTimeOffset StartUtc, DateTimeOffset EndUtc)
{
    internal const string TimeZoneId = "America/Sao_Paulo";
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    internal static DateOnly LocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    internal static Result<LocalPeriod> Create(string? from, string? to)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var last) ||
            first > last || last == DateOnly.MaxValue)
            return Result<LocalPeriod>.Failure(new("irrigation.invalid_period",
                "Informe from e to em YYYY-MM-DD, em ordem válida e com fim anterior a 9999-12-31.", ErrorCategory.Validation,
                new Dictionary<string, string[]> { ["period"] = ["Período local obrigatório ou inválido."] }));
        try
        {
            return Result<LocalPeriod>.Success(new(first, last, Boundary(first), Boundary(last.AddDays(1))));
        }
        catch (ArgumentException)
        {
            return Result<LocalPeriod>.Failure(new("irrigation.invalid_period",
                "Período fora da faixa de instantes suportada.", ErrorCategory.Validation));
        }
    }

    private static DateTimeOffset Boundary(DateOnly date)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // Transições históricas podem saltar a meia-noite: a borda é o primeiro instante válido do dia.
        while (Zone.IsInvalidTime(midnight)) midnight = midnight.AddMinutes(1);
        if (Zone.IsAmbiguousTime(midnight))
            return new DateTimeOffset(midnight, Zone.GetAmbiguousTimeOffsets(midnight).Max()).ToUniversalTime();
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(midnight, Zone));
    }
}
