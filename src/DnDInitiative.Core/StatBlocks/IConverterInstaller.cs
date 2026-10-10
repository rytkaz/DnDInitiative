namespace DnDInitiative.Core.StatBlocks;

/// <summary>Looks after the app's own copy of the converter program.</summary>
public interface IConverterInstaller
{
    /// <summary>The path of the installed converter, or null if it has not been downloaded.</summary>
    string? FindInstalled();

    /// <summary>
    /// Downloads the newest release and replaces any installed copy.
    /// </summary>
    /// <returns>The path of the installed converter.</returns>
    /// <exception cref="ConversionException">The download failed, or what arrived could not be used.</exception>
    Task<string> InstallLatestAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}