using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.Core.StatBlocks;

using Serilog;

namespace DnDInitiative.App.ViewModels;

public sealed partial class ImportViewModel : PageViewModel
{
    private const string ToolFilter = "ttrpg-convert (*.exe)|*.exe|All files (*.*)|*.*";
    private const string ConfigFilter = "Configuration (*.json)|*.json|All files (*.*)|*.*";
    private const string ZipFilter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*";
    private const string HomebrewFilter = "5eTools homebrew (*.json)|*.json|All files (*.*)|*.*";
    private const string NotDataFolderMessage =
        "That folder does not look like 5eTools data. Pick the 'data' folder, the one that contains 'bestiary'.";
    private const double MinSourcesPanelHeight = 100;
    private const double MaxSourcesPanelHeight = 800;

    private readonly IConversionSettingsStore _settingsStore;
    private readonly IStatBlockConverter _converter;
    private readonly IStatBlockImporter _importer;
    private readonly IFolderPicker _folderPicker;
    private readonly IFilePicker _filePicker;
    private readonly IMessenger _messenger;
    private readonly IConverterInstaller _installer;
    private readonly ISourceDataService _sourceData;
    private readonly IConverterConfigWriter _configWriter;
    private readonly IUiStateStore _uiState;
    private bool _isLoaded;

    [ObservableProperty]
    private string _toolPath = "";

    [ObservableProperty]
    private string _configPath = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _dataDirectory = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(UpdateConverterCommand))]
    [NotifyCanExecuteChangedFor(nameof(UseDataZipCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _log = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _sourceSearchText = "";

    // The ticks from the last session, kept until the book list has been read from the data folder.
    private IReadOnlyList<string> _savedSources = [];

    private double _sourcesPanelHeight = 190;

    /// <summary>The books in the chosen data folder that contain monsters.</summary>
    public ObservableCollection<SourceOptionViewModel> Sources { get; } = [];

    /// <summary>Homebrew files converted along with the ticked books. Every monster in them is imported.</summary>
    public ObservableCollection<HomebrewFileViewModel> HomebrewFiles { get; } = [];

    /// <summary>Height of the book list, changed by dragging its lower edge. Kept within sensible limits.</summary>
    public double SourcesPanelHeight
    {
        get => _sourcesPanelHeight;
        set => SetProperty(ref _sourcesPanelHeight, Math.Clamp(value, MinSourcesPanelHeight, MaxSourcesPanelHeight));
    }

    public ImportViewModel(
        IConversionSettingsStore settingsStore,
        IStatBlockConverter converter,
        IStatBlockImporter importer,
        IFolderPicker folderPicker,
        IFilePicker filePicker,
        IMessenger messenger,
        IConverterInstaller installer,
        ISourceDataService sourceData,
        IConverterConfigWriter configWriter,
        IUiStateStore uiState)
    {
        ArgumentNullException.ThrowIfNull(uiState);

        _settingsStore = settingsStore;
        _converter = converter;
        _importer = importer;
        _folderPicker = folderPicker;
        _filePicker = filePicker;
        _messenger = messenger;
        _installer = installer;
        _sourceData = sourceData;
        _configWriter = configWriter;
        _uiState = uiState;

        // Through the property, not the field, so a size saved by hand in the file is still clamped.
        if (uiState.Load().SourcesPanelHeight is { } savedHeight)
        {
            SourcesPanelHeight = savedHeight;
        }
    }

    /// <summary>
    /// Remembers the book list's height for the next run. Called when a drag ends rather than
    /// from the property setter, which runs for every pixel the mouse moves.
    /// </summary>
    public void SaveLayout()
    {
        _uiState.Save(_uiState.Load() with { SourcesPanelHeight = SourcesPanelHeight });
    }

    public override string Title => "5eTools Import";

    public override string ShortTitle => "Import";

    /// <summary>The opposite of <see cref="IsRunning"/>, for disabling the inputs during a run.</summary>
    public bool IsIdle => !IsRunning;

    // Only the data is required. Without a program path the app uses its own copy of the
    // converter, and without a configuration file it writes one for the ticked books. Homebrew
    // needs the data too: its creatures refer to spells, items and other creatures in the books.
    private bool CanRun => !string.IsNullOrWhiteSpace(DataDirectory);

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Read once, so paths typed but not yet used survive leaving the page.
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;

        ConversionSettings settings = await _settingsStore.LoadAsync();
        ToolPath = settings.ToolPath;
        ConfigPath = settings.ConfigPath;
        DataDirectory = settings.DataDirectory;
        _savedSources = settings.Sources;
        await RefreshSourcesAsync();
        await LoadHomebrewFilesAsync(settings.HomebrewFiles);
    }

    // Each saved file is read again, which both fills in its label and checks it is still there.
    private async Task LoadHomebrewFilesAsync(IReadOnlyList<string> paths)
    {
        var unreadable = new List<string>();

        foreach (string path in paths)
        {
            try
            {
                HomebrewFiles.Add(new HomebrewFileViewModel(await _sourceData.ReadHomebrewFileAsync(path)));
            }
            catch (ConversionException)
            {
                unreadable.Add(Path.GetFileName(path));
            }
        }

        if (unreadable.Count > 0)
        {
            // Left out rather than shown as broken: the converter stops at a file it cannot open.
            StatusMessage = "These homebrew files can no longer be read and were left out of the list: "
                + string.Join(", ", unreadable) + ".";
        }
    }

    [RelayCommand]
    private async Task AddHomebrewAsync()
    {
        if (_filePicker.PickFile("Select a 5eTools homebrew file", HomebrewFilter) is not { } path)
        {
            return;
        }

        try
        {
            HomebrewFile file = await _sourceData.ReadHomebrewFileAsync(path);

            // Compared after reading, because the service returns the full path whatever was picked.
            if (HomebrewFiles.Any(existing => string.Equals(existing.Path, file.Path, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "That homebrew file is already in the list.";
                return;
            }

            HomebrewFiles.Add(new HomebrewFileViewModel(file));
            StatusMessage = "";

            // Saved straight away, like the unpacked data: the list should not depend on a run to be kept.
            await _settingsStore.SaveAsync(CurrentSettings());
        }
        catch (ConversionException exception)
        {
            // Not a homebrew file, or not readable. The service's message says which.
            StatusMessage = exception.Message;
        }
    }

    // The file to remove arrives as the command's parameter: each row's button passes its own item.
    [RelayCommand]
    private async Task RemoveHomebrewAsync(HomebrewFileViewModel? file)
    {
        if (file is null || !HomebrewFiles.Remove(file))
        {
            return;
        }

        await _settingsStore.SaveAsync(CurrentSettings());
    }

    [RelayCommand]
    private void BrowseTool()
    {
        if (_filePicker.PickFile("Select the ttrpg-convert program", ToolFilter) is { } path)
        {
            ToolPath = path;
        }
    }

    [RelayCommand]
    private void BrowseConfig()
    {
        if (_filePicker.PickFile("Select the converter's configuration file", ConfigFilter) is { } path)
        {
            ConfigPath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseDataAsync()
    {
        if (_folderPicker.PickFolder("Select the 5eTools data folder") is not { } picked)
        {
            return;
        }

        // Picking the folder above "data" is an easy mistake; quietly use the right one.
        string? dataFolder = _sourceData.FindDataFolder(picked);
        DataDirectory = dataFolder ?? picked;
        StatusMessage = dataFolder is null ? NotDataFolderMessage : "";
        await RefreshSourcesAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // Check before starting, so a wrong folder gets a plain sentence instead of the converter's error output.
        string? dataFolder = _sourceData.FindDataFolder(Clean(DataDirectory));
        if (dataFolder is null)
        {
            StatusMessage = NotDataFolderMessage;
            return;
        }

        DataDirectory = dataFolder;
        ConversionSettings settings = CurrentSettings();

        IsRunning = true;
        Log = "";
        StatusMessage = "Converting...";

        try
        {
            // Saved first, so the paths are remembered even if the run fails or is cancelled.
            await _settingsStore.SaveAsync(settings, cancellationToken);

            var progress = new UiProgress<string>(AppendToLog);
            string toolPath = await ResolveToolAsync(settings.ToolPath, progress, cancellationToken);

            // The user's own configuration wins. Otherwise write one for the ticked books and the homebrew.
            string configPath = settings.ConfigPath.Length > 0
                ? settings.ConfigPath
                : await _configWriter.WriteAsync(settings.Sources, settings.HomebrewFiles, cancellationToken);

            var request = new ConversionRequest(toolPath, settings.DataDirectory, configPath);
            string notesFolder = await _converter.ConvertAsync(request, progress, cancellationToken);

            StatusMessage = "Importing the converted notes...";
            StatBlockImportResult result = await _importer.ImportFolderAsync(notesFolder, cancellationToken);
            _messenger.Send(new StatBlocksImportedMessage(result));

            if (result.Added + result.Updated == 0)
            {
                // Something is wrong with what the converter wrote. Keep it, so it can be looked at.
                StatusMessage = "The conversion finished, but its output contains no stat blocks. "
                    + $"The notes it wrote were kept in {notesFolder} so you can check them.";
            }
            else
            {
                // The stat blocks are in the library now; the notes have done their job.
                _converter.DiscardOutput();
                StatusMessage = Describe(result);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        catch (ConversionException exception)
        {
            // Written for the user by the converter service; shown as it is.
            StatusMessage = exception.Message;
        }
        catch (IOException exception)
        {
            StatusMessage = "Import failed: " + exception.Message;
        }
        catch (UnauthorizedAccessException exception)
        {
            StatusMessage = "Import failed: " + exception.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    private static string Describe(StatBlockImportResult result)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Done. Imported {result.Added} new and updated {result.Updated}, from {result.FilesScanned} files.");
    }

    // Explorer's "Copy as path" wraps the path in quotes; accept that when it is pasted in.
    private static string Clean(string path)
    {
        return path.Trim().Trim('"');
    }

    private void AppendToLog(string line)
    {
        Log += line + Environment.NewLine;
    }

    // A path the user typed wins. Otherwise the app's own copy, downloaded if this is the first time.
    private async Task<string> ResolveToolAsync(string chosenPath, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (chosenPath.Length > 0)
        {
            return chosenPath;
        }

        return _installer.FindInstalled() ?? await _installer.InstallLatestAsync(progress, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task UpdateConverterAsync()
    {
        IsRunning = true;
        Log = "";
        StatusMessage = "Downloading the latest converter...";

        try
        {
            await _installer.InstallLatestAsync(new UiProgress<string>(AppendToLog));
            StatusMessage = "The converter is up to date.";
        }
        catch (ConversionException exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task UseDataZipAsync()
    {
        if (_filePicker.PickFile("Select the 5eTools zip file you downloaded", ZipFilter) is not { } zipPath)
        {
            return;
        }

        IsRunning = true;
        Log = "";
        StatusMessage = "Unpacking the data...";

        try
        {
            DataDirectory = await _sourceData.ImportZipAsync(zipPath, new UiProgress<string>(AppendToLog));
            await RefreshSourcesAsync();
            // Saved straight away: the unpacked data stays on disk, so the path should survive too.
            await _settingsStore.SaveAsync(CurrentSettings());
            StatusMessage = "The data is unpacked and ready. You can delete the zip file.";
        }
        catch (ConversionException exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    private ConversionSettings CurrentSettings()
    {
        return new ConversionSettings
        {
            ToolPath = Clean(ToolPath),
            ConfigPath = Clean(ConfigPath),
            DataDirectory = Clean(DataDirectory),
            Sources = SelectedSourceIds(),
            HomebrewFiles = HomebrewFiles.Select(file => file.Path).ToList(),
        };
    }

    // Both buttons act on the books the search is showing, so "search, then select all"
    // ticks just the matches. With an empty search that is every book.
    [RelayCommand]
    private void SelectAllSources()
    {
        foreach (SourceOptionViewModel source in Sources.Where(source => source.IsVisible))
        {
            source.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSources()
    {
        foreach (SourceOptionViewModel source in Sources.Where(source => source.IsVisible))
        {
            source.IsSelected = false;
        }
    }

    partial void OnSourceSearchTextChanged(string value)
    {
        ApplySourceSearch();
    }

    private void ApplySourceSearch()
    {
        string searchText = SourceSearchText.Trim();
        foreach (SourceOptionViewModel source in Sources)
        {
            source.IsVisible = source.Matches(searchText);
        }
    }

    private IReadOnlyList<string> SelectedSourceIds()
    {
        // Before the list has been read, the saved ticks are still the truth.
        return Sources.Count > 0
            ? Sources.Where(source => source.IsSelected).Select(source => source.Id).ToList()
            : _savedSources;
    }

    // Rebuilds the list from the data folder, keeping whatever was ticked.
    private async Task RefreshSourcesAsync()
    {
        var selected = new HashSet<string>(SelectedSourceIds(), StringComparer.OrdinalIgnoreCase);
        Sources.Clear();

        if (_sourceData.FindDataFolder(Clean(DataDirectory)) is not { } dataFolder)
        {
            return;
        }

        foreach (SourceBook book in await _sourceData.GetMonsterSourcesAsync(dataFolder))
        {
            Sources.Add(new SourceOptionViewModel(book, selected.Contains(book.Id)));
        }

        // A search typed before the list was rebuilt still applies to the new list.
        ApplySourceSearch();
    }
}