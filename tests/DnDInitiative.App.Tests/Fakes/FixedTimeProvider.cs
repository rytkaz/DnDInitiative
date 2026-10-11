namespace DnDInitiative.App.Tests.Fakes;

/// <summary>A clock that shows whatever time the test sets.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }
}
