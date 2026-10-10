namespace DnDInitiative.Core.StatBlocks;

/// <summary>The paths the user last used for a conversion.</summary>
public sealed record ConversionSettings
{
    public string ToolPath { get; init; } = "";

    public string ConfigPath { get; init; } = "";

    public string DataDirectory { get; init; } = "";

    public string OutputDirectory { get; init; } = "";

    public IReadOnlyList<string> Sources { get; init; } = [];
}

public interface IConversionSettingsStore
{
    /// <summary>Returns the stored settings, or empty ones if none were stored yet.</summary>
    Task<ConversionSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ConversionSettings settings, CancellationToken cancellationToken = default);
}