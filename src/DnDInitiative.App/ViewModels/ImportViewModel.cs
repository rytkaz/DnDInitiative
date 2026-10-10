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

    private readonly IConversionSettingsStore _settingsStore;
    private readonly IStatBlockConverter _converter;
    private readonly IStatBlockImporter _importer;
    private readonly IFolderPicker _folderPicker;
    private readonly IFilePicker _filePicker;
    private readonly IMessenger _messenger;
    private readonly IConverterInstaller _installer;
    private bool _isLoaded;

    [ObservableProperty]
    private string _toolPath = "";

    [ObservableProperty]
    private string _configPath = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _dataDirectory = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _outputDirectory = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(UpdateConverterCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _log = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public ImportViewModel(
        IConversionSettingsStore settingsStore,
        IStatBlockConverter converter,
        IStatBlockImporter importer,
        IFolderPicker folderPicker,
        IFilePicker filePicker,
        IMessenger messenger,
        IConverterInstaller installer)
    {
        _settingsStore = settingsStore;
        _converter = converter;
        _importer = importer;
        _folderPicker = folderPicker;
        _filePicker = filePicker;
        _messenger = messenger;
        _installer = installer;
    }

    public override string Title => "5eTools Import";

    /// <summary>The opposite of <see cref="IsRunning"/>, for disabling the inputs during a run.</summary>
    public bool IsIdle => !IsRunning;

    // The program and the configuration file are optional. Without a program path,
    // the app uses its own copy of the converter.
    private bool CanRun => !string.IsNullOrWhiteSpace(DataDirectory) && !string.IsNullOrWhiteSpace(OutputDirectory);

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
        OutputDirectory = settings.OutputDirectory;
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
    private void BrowseData()
    {
        if (_folderPicker.PickFolder("Select the 5eTools data folder") is { } path)
        {
            DataDirectory = path;
        }
    }

    [RelayCommand]
    private void BrowseOutput()
    {
        if (_folderPicker.PickFolder("Select the folder the notes are written to") is { } path)
        {
            OutputDirectory = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var settings = new ConversionSettings
        {
            ToolPath = Clean(ToolPath),
            ConfigPath = Clean(ConfigPath),
            DataDirectory = Clean(DataDirectory),
            OutputDirectory = Clean(OutputDirectory),
        };

        IsRunning = true;
        Log = "";
        StatusMessage = "Converting...";

        try
        {
            // Saved first, so the paths are remembered even if the run fails or is cancelled.
            await _settingsStore.SaveAsync(settings, cancellationToken);

            var progress = new UiProgress<string>(AppendToLog);
            string toolPath = await ResolveToolAsync(settings.ToolPath, progress, cancellationToken);

            var request = new ConversionRequest(
                toolPath,
                settings.DataDirectory,
                settings.OutputDirectory,
                settings.ConfigPath);
            await _converter.ConvertAsync(request, progress, cancellationToken);

            StatusMessage = "Importing the converted notes...";
            StatBlockImportResult result = await _importer.ImportFolderAsync(settings.OutputDirectory, cancellationToken);

            _messenger.Send(new StatBlocksImportedMessage(result));
            StatusMessage = Describe(result);
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
        if (result.Added + result.Updated == 0)
        {
            return "The conversion finished, but its output contains no stat blocks. "
                + "Check that the configuration's monster template writes a statblock.";
        }

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
}