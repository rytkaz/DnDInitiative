namespace DnDInitiative.Core.Homebrewery;

/// <summary>The choices the user last made on the Homebrewery import page.</summary>
public sealed record HomebrewerySettings
{
    /// <summary>What the source box starts with the first time.</summary>
    public const string DefaultSource = "Conflux Creatures";

    public string PdfPath { get; init; } = "";

    /// <summary>The extra links, as typed: one per line.</summary>
    public string Links { get; init; } = "";

    public string Source { get; init; } = DefaultSource;
}

public interface IHomebrewerySettingsStore
{
    /// <summary>Returns the stored settings, or the defaults if none were stored yet.</summary>
    Task<HomebrewerySettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(HomebrewerySettings settings, CancellationToken cancellationToken = default);
}
