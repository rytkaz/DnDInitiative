using System.Globalization;
using System.IO;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Imports creatures and conditions from Homebrewery documents, found through the links in a
/// PDF catalog and through links typed in by hand.
/// </summary>
public sealed partial class HomebreweryImportViewModel : PageViewModel
{
    private const string PdfFilter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";

    // Links can be pasted one per line or several on a line.
    private static readonly char[] s_linkSeparators = ['\r', '\n', ' ', '\t'];

    private readonly IHomebreweryImporter _importer;
    private readonly IHomebrewerySettingsStore _settingsStore;
    private readonly IFilePicker _filePicker;
    private readonly IMessenger _messenger;
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _pdfPath = "";

    /// <summary>Extra Homebrewery links, as typed.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _links = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _source = HomebrewerySettings.DefaultSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isRunning;

    [ObservableProperty]
    private string _log = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public HomebreweryImportViewModel(
        IHomebreweryImporter importer,
        IHomebrewerySettingsStore settingsStore,
        IFilePicker filePicker,
        IMessenger messenger,
        IUiStateStore uiState)
    {
        _importer = importer;
        _settingsStore = settingsStore;
        _filePicker = filePicker;
        _messenger = messenger;
        LinksBoxHeight = new PanelSize(uiState, "HomebreweryImport.Links", defaultValue: 72, minimum: 40, maximum: 500);
    }

    /// <summary>Height of the box for extra links.</summary>
    public PanelSize LinksBoxHeight { get; }

    public override string Title => "Homebrewery Import";

    public override string ShortTitle => "Homebrew";

    /// <summary>The opposite of <see cref="IsRunning"/>, for disabling the inputs during a run.</summary>
    public bool IsIdle => !IsRunning;

    // The source is part of every imported stat block's identity, so it cannot be left empty.
    private bool CanRun => !string.IsNullOrWhiteSpace(Source)
        && (!string.IsNullOrWhiteSpace(PdfPath) || !string.IsNullOrWhiteSpace(Links));

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Read once, so what was typed but not yet used survives leaving the page.
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;

        HomebrewerySettings settings = await _settingsStore.LoadAsync();
        PdfPath = settings.PdfPath;
        Links = settings.Links;
        Source = settings.Source;
    }

    [RelayCommand]
    private void BrowsePdf()
    {
        if (_filePicker.PickFile("Select a PDF that links to Homebrewery documents", PdfFilter) is { } path)
        {
            PdfPath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun), IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        HomebrewerySettings settings = CurrentSettings();

        IsRunning = true;
        Log = "";
        StatusMessage = "Importing...";

        try
        {
            // Saved first, so the choices are remembered even if the run fails or is cancelled.
            await _settingsStore.SaveAsync(settings, cancellationToken);

            var request = new HomebreweryImportRequest
            {
                PdfPath = settings.PdfPath,
                Links = settings.Links.Split(s_linkSeparators, StringSplitOptions.RemoveEmptyEntries),
                Source = settings.Source,
            };

            HomebreweryImportResult result = await _importer.ImportAsync(request, new UiProgress<string>(AppendToLog), cancellationToken);
            _messenger.Send(new StatBlocksImportedMessage(result.StatBlocks));
            StatusMessage = Describe(result);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        catch (ConversionException exception)
        {
            // Written for the user by the importer; shown as it is.
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

    private static string Describe(HomebreweryImportResult result)
    {
        StatBlockImportResult statBlocks = result.StatBlocks;
        var text = new StringBuilder();

        if (statBlocks.Added + statBlocks.Updated + result.ConditionsImported + result.ConditionsKept == 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"Done, but the {result.DocumentsRead} documents read contain no stat blocks or conditions.");
        }
        else
        {
            text.Append(CultureInfo.InvariantCulture, $"Done. Imported {statBlocks.Added} new and updated {statBlocks.Updated} stat blocks and {result.ConditionsImported} conditions, from {result.DocumentsRead} documents.");
        }

        if (result.ConditionsKept > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $" {result.ConditionsKept} conditions were left as the Player's Handbook defines them.");
        }

        if (result.FailedLinks.Count > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $" {result.FailedLinks.Count} documents could not be downloaded; the log below lists them.");
        }

        return text.ToString();
    }

    private void AppendToLog(string line)
    {
        Log += line + Environment.NewLine;
    }

    private HomebrewerySettings CurrentSettings()
    {
        return new HomebrewerySettings
        {
            // Explorer's "Copy as path" wraps the path in quotes; accept that when it is pasted in.
            PdfPath = PdfPath.Trim().Trim('"'),
            Links = Links.Trim(),
            Source = Source.Trim(),
        };
    }
}
