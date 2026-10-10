namespace DnDInitiative.Core.StatBlocks;

/// <summary>A book or adventure in the 5eTools data, such as ("XMM", "Monster Manual (2024)").</summary>
public sealed record SourceBook(string Id, string Name);

/// <summary>
/// A homebrew file in 5eTools format: one JSON file that holds one or more books of its own.
/// </summary>
/// <param name="Path">Full path of the file.</param>
/// <param name="Sources">The books the file declares, such as ("FleeMortals", "Flee, Mortals!").</param>
/// <param name="MonsterCount">How many monsters the file holds.</param>
public sealed record HomebrewFile(string Path, IReadOnlyList<SourceBook> Sources, int MonsterCount);

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

    /// <summary>Reads what a homebrew file contains, without importing anything.</summary>
    /// <exception cref="ConversionException">
    /// The file cannot be read, is not a 5eTools homebrew file, or holds no monsters.
    /// </exception>
    Task<HomebrewFile> ReadHomebrewFileAsync(string filePath, CancellationToken cancellationToken = default);
}