using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Core.Homebrewery;

/// <summary>What to import from the Homebrewery, and under which source name.</summary>
public sealed record HomebreweryImportRequest
{
    /// <summary>A PDF whose links point at Homebrewery documents, such as a creature catalog. Optional.</summary>
    public string PdfPath { get; init; } = "";

    /// <summary>Homebrewery links typed in by hand, read along with the PDF's.</summary>
    public IReadOnlyList<string> Links { get; init; } = [];

    /// <summary>
    /// The source every imported stat block and condition is filed under. Part of a stat block's
    /// identity, so it has to stay the same from one import to the next.
    /// </summary>
    public required string Source { get; init; }
}

public sealed record HomebreweryImportResult
{
    /// <summary>How many documents were downloaded and read.</summary>
    public int DocumentsRead { get; init; }

    /// <summary>The links of the documents that could not be downloaded.</summary>
    public IReadOnlyList<string> FailedLinks { get; init; } = [];

    public StatBlockImportResult StatBlocks { get; init; } = new(Added: 0, Updated: 0, FilesScanned: 0);

    /// <summary>How many conditions were added or replaced.</summary>
    public int ConditionsImported { get; init; }

    /// <summary>How many conditions were left alone because the library has the official rules for them.</summary>
    public int ConditionsKept { get; init; }
}

public interface IHomebreweryImporter
{
    /// <summary>
    /// Downloads the linked Homebrewery documents and imports the stat blocks and conditions in them.
    /// </summary>
    /// <exception cref="ConversionException">The PDF cannot be read, or there are no Homebrewery links to import.</exception>
    Task<HomebreweryImportResult> ImportAsync(
        HomebreweryImportRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
