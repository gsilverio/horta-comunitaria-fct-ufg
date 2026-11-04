namespace HortaComunitaria.Api.IntegrationTests;

public sealed class FixedTimeProvider : TimeProvider
{
    public static DateTimeOffset Now { get; } = new(2026, 10, 8, 18, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}
