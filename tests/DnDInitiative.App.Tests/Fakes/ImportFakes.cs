using DnDInitiative.App.Services;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeFolderPicker : IFolderPicker
{
    public string? FolderToReturn { get; set; }

    public string? PickFolder(string title)
    {
        return FolderToReturn;
    }
}

internal sealed class FakeStatBlockImporter : IStatBlockImporter
{
    public string? ImportedFolder { get; private set; }

    public Task<StatBlockImportResult> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        ImportedFolder = folderPath;
        return Task.FromResult(new StatBlockImportResult(Added: 3, Updated: 1, FilesScanned: 10));
    }
}