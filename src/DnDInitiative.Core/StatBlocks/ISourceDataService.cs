namespace DnDInitiative.Core.StatBlocks;

/// <summary>A book or adventure in the 5eTools data, such as ("XMM", "Monster Manual (2024)").</summary>
public sealed record SourceBook(string Id, string Name);

/// <summary>Finds and unpacks the 5eTools data the converter reads.</summary>
public interface ISourceDataService
{
    /// <summary>
    /// Locates the data folder at or just inside a folder the user picked.
    /// </summary>
    /// <returns>The folder that contains the bestiary, or null if this is not 5eTools data.</returns>
    string? FindDataFolder(string folder);

    /// <summary>
    /// Unpacks the data from a zip file into this computer's data folder, replacing any earlier copy.
    /// </summary>
    /// <returns>The unpacked data folder.</returns>
    /// <exception cref="ConversionException">The file is not a zip, or holds no 5eTools data.</exception>
    Task<string> ImportZipAsync(
        string zipPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the books and adventures in a data folder that contain monsters, by name.</summary>
    Task<IReadOnlyList<SourceBook>> GetMonsterSourcesAsync(string dataFolder, CancellationToken cancellationToken = default);
}