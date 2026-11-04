namespace HortaComunitaria.Api.Infrastructure.Time;

internal static class InstantPrecision
{
    // PostgreSQL armazena microssegundos; .NET também representa frações de 100 ns.
    internal static DateTimeOffset ForPersistence(DateTimeOffset value) =>
        new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);
    internal static DateTimeOffset? ForPersistence(DateTimeOffset? value) =>
        value is null ? null : ForPersistence(value.Value);
}
