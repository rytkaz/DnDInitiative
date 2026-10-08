namespace DnDInitiative.Core.StatBlocks;

public sealed record StatBlockImportResult(int Added, int Updated, int FilesScanned);

public interface IStatBlockImporter
{
    /// <summary>
    /// Imports every stat block found in the Markdown notes under a folder.
    /// </summary>
    Task<StatBlockImportResult> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default);
}