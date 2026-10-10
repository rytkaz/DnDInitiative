using DnDInitiative.App.Services;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeFilePicker : IFilePicker
{
    public string? FileToReturn { get; set; }

    public string? PickFile(string title, string filter)
    {
        return FileToReturn;
    }
}

internal sealed class InMemoryConversionSettingsStore : IConversionSettingsStore
{
    public ConversionSettings Settings { get; set; } = new();

    public Task<ConversionSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Settings);
    }

    public Task SaveAsync(ConversionSettings settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}

internal sealed class FakeStatBlockConverter : IStatBlockConverter
{
    public ConversionRequest? Request { get; private set; }

    /// <summary>Lines to report as if the converter had printed them.</summary>
    public IReadOnlyList<string> Output { get; set; } = [];

    public Exception? Failure { get; set; }

    /// <summary>Makes the conversion run until it is cancelled.</summary>
    public bool RunsUntilCancelled { get; set; }

    public async Task ConvertAsync(
        ConversionRequest request,
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

        if (Failure is not null)
        {
            throw Failure;
        }
    }
}

internal sealed class FakeConverterInstaller : IConverterInstaller
{
    /// <summary>The app's own copy, if it "has been downloaded".</summary>
    public string? Installed { get; set; }

    public int InstallCount { get; private set; }

    public string? FailureMessage { get; set; }

    public string? FindInstalled()
    {
        return Installed;
    }

    public Task<string> InstallLatestAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (FailureMessage is not null)
        {
            throw new ConversionException(FailureMessage);
        }

        InstallCount++;
        progress?.Report("Downloading converter");
        Installed = "downloaded.exe";
        return Task.FromResult(Installed);
    }
}

internal sealed class FakeSourceDataService : ISourceDataService
{
    /// <summary>When false, no folder is recognised as 5eTools data.</summary>
    public bool RecognisesFolders { get; set; } = true;

    /// <summary>Returned instead of the picked folder, as if the data were one level down.</summary>
    public string? CorrectedFolder { get; set; }

    public string? ImportedZip { get; private set; }

    public string? FailureMessage { get; set; }

    public string? FindDataFolder(string folder)
    {
        return RecognisesFolders ? CorrectedFolder ?? folder : null;
    }

    public Task<string> ImportZipAsync(
        string zipPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (FailureMessage is not null)
        {
            throw new ConversionException(FailureMessage);
        }

        ImportedZip = zipPath;
        progress?.Report("Unpacked 3 files.");
        return Task.FromResult("unpacked-data");
    }
}