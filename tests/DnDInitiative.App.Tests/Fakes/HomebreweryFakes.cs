using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeHomebreweryImporter : IHomebreweryImporter
{
    public HomebreweryImportRequest? Request { get; private set; }

    /// <summary>What every import reports having done.</summary>
    public HomebreweryImportResult Result { get; set; } = new()
    {
        DocumentsRead = 5,
        StatBlocks = new StatBlockImportResult(Added: 7, Updated: 2, FilesScanned: 5),
        ConditionsImported = 3,
    };

    /// <summary>Lines to report as if the importer had written them.</summary>
    public IReadOnlyList<string> Output { get; set; } = [];

    public Exception? Failure { get; set; }

    /// <summary>Makes the import run until it is cancelled.</summary>
    public bool RunsUntilCancelled { get; set; }

    public async Task<HomebreweryImportResult> ImportAsync(
        HomebreweryImportRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Request = request;

        foreach (string line in Output)
        {
            progress?.Report(line);
        }

        if (RunsUntilCancelled)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return Failure is null ? Result : throw Failure;
    }
}

internal sealed class InMemoryHomebrewerySettingsStore : IHomebrewerySettingsStore
{
    public HomebrewerySettings Settings { get; set; } = new();

    public Task<HomebrewerySettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Settings);
    }

    public Task SaveAsync(HomebrewerySettings settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}
