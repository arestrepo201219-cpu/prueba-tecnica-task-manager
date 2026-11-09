namespace TaskManager.UnitTests.TestDoubles;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 11, 8, 15, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(DefaultNow)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}
