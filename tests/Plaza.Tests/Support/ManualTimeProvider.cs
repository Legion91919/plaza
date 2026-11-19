namespace Plaza.Tests.Support;

/// <summary>A clock the test controls.</summary>
public sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public static DateTimeOffset DefaultStart { get; } = new(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
