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

    /// <summary>How many times the notes were thrown away.</summary>
    public int DiscardCount { get; private set; }

    public void DiscardOutput()
    {
        DiscardCount++;
    }

    public async Task<string> ConvertAsync(
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

        return "converted-notes";
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

    public IReadOnlyList<SourceBook> Sources { get; set; } = [];

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

    public Task<IReadOnlyList<SourceBook>> GetMonsterSourcesAsync(string dataFolder, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Sources);
    }

    /// <summary>The books that "contain spells".</summary>
    public IReadOnlyList<string> SpellSources { get; set; } = [];

    public Task<IReadOnlyList<string>> GetSpellSourcesAsync(string dataFolder, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SpellSources);
    }

    /// <summary>Paths that are refused, as if they were missing or not homebrew.</summary>
    public HashSet<string> UnreadableHomebrewFiles { get; } = [];

    public Task<HomebrewFile> ReadHomebrewFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (UnreadableHomebrewFiles.Contains(filePath))
        {
            throw new ConversionException($"'{filePath}' is not a 5eTools homebrew file.");
        }

        return Task.FromResult(new HomebrewFile(filePath, [new SourceBook("Brew", "Brew Book")], MonsterCount: 12));
    }
}

internal sealed class FakeConverterConfigWriter : IConverterConfigWriter
{
    /// <summary>What the last generated configuration was written for, or null if none was.</summary>
    public ConverterConfigOptions? Options { get; private set; }

    public IReadOnlyCollection<string>? WrittenFor => Options?.Sources;

    public IReadOnlyCollection<string>? WrittenHomebrew => Options?.HomebrewFiles;

    public Task<string> WriteAsync(ConverterConfigOptions options, CancellationToken cancellationToken = default)
    {
        Options = options;
        return Task.FromResult("generated-config.json");
    }
}
