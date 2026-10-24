using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class HomebreweryImportViewModelTests
{
    private readonly FakeHomebreweryImporter _importer = new();
    private readonly InMemoryHomebrewerySettingsStore _settingsStore = new();
    private readonly FakeFilePicker _filePicker = new();
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly HomebreweryImportViewModel _viewModel;
    private StatBlocksImportedMessage? _received;

    public HomebreweryImportViewModelTests()
    {
        _messenger.Register<HomebreweryImportViewModelTests, StatBlocksImportedMessage>(
            this,
            (recipient, message) => recipient._received = message);

        _viewModel = new HomebreweryImportViewModel(_importer, _settingsStore, _filePicker, _messenger);
    }

    [Fact]
    public void Source_StartsWithTheDefaultName()
    {
        Assert.Equal(HomebrewerySettings.DefaultSource, _viewModel.Source);
    }

    [Fact]
    public async Task LoadCommand_ShowsTheStoredChoices()
    {
        _settingsStore.Settings = new HomebrewerySettings { PdfPath = "catalog.pdf", Links = "a\nb", Source = "My Monsters" };

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("catalog.pdf", _viewModel.PdfPath);
        Assert.Equal("a\nb", _viewModel.Links);
        Assert.Equal("My Monsters", _viewModel.Source);
    }

    [Fact]
    public async Task LoadCommand_SecondTime_KeepsWhatWasTyped()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.PdfPath = "typed.pdf";

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("typed.pdf", _viewModel.PdfPath);
    }

    [Fact]
    public void RunCommand_NeedsAPdfOrALinkAndASource()
    {
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.Links = "https://homebrewery.naturalcrit.com/share/abc";
        Assert.True(_viewModel.RunCommand.CanExecute(null));

        _viewModel.Source = " ";
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.Source = "Mine";
        _viewModel.Links = "";
        _viewModel.PdfPath = "catalog.pdf";
        Assert.True(_viewModel.RunCommand.CanExecute(null));
    }

    [Fact]
    public void BrowsePdfCommand_FilePicked_FillsInThePath()
    {
        _filePicker.FileToReturn = @"C:\Monsters\catalog.pdf";

        _viewModel.BrowsePdfCommand.Execute(null);

        Assert.Equal(@"C:\Monsters\catalog.pdf", _viewModel.PdfPath);
    }

    [Fact]
    public void BrowsePdfCommand_DialogCancelled_KeepsWhatWasThere()
    {
        _viewModel.PdfPath = "before.pdf";

        _viewModel.BrowsePdfCommand.Execute(null);

        Assert.Equal("before.pdf", _viewModel.PdfPath);
    }

    [Fact]
    public async Task RunCommand_PassesThePdfTheLinksAndTheSource()
    {
        _viewModel.PdfPath = "\"C:\\Monsters\\catalog.pdf\"";
        _viewModel.Links = "https://homebrewery.naturalcrit.com/share/one\r\n\r\n  https://homebrewery.naturalcrit.com/share/two https://homebrewery.naturalcrit.com/share/three";
        _viewModel.Source = "  Conflux Creatures ";

        await _viewModel.RunCommand.ExecuteAsync(null);

        HomebreweryImportRequest request = Assert.IsType<HomebreweryImportRequest>(_importer.Request);
        Assert.Equal(@"C:\Monsters\catalog.pdf", request.PdfPath);
        Assert.Equal("Conflux Creatures", request.Source);
        string[] expectedLinks =
        [
            "https://homebrewery.naturalcrit.com/share/one",
            "https://homebrewery.naturalcrit.com/share/two",
            "https://homebrewery.naturalcrit.com/share/three",
        ];
        Assert.Equal(expectedLinks, request.Links.ToArray());
    }

    [Fact]
    public async Task RunCommand_SavesTheChoicesForNextTime()
    {
        _viewModel.PdfPath = "catalog.pdf";
        _viewModel.Source = "Mine";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("catalog.pdf", _settingsStore.Settings.PdfPath);
        Assert.Equal("Mine", _settingsStore.Settings.Source);
    }

    [Fact]
    public async Task RunCommand_Finished_TellsTheLibraryAndSummarises()
    {
        _viewModel.PdfPath = "catalog.pdf";
        _importer.Result = new HomebreweryImportResult
        {
            DocumentsRead = 5,
            FailedLinks = ["https://homebrewery.naturalcrit.com/share/gone"],
            StatBlocks = new StatBlockImportResult(Added: 7, Updated: 2, FilesScanned: 5),
            ConditionsImported = 3,
            ConditionsKept = 4,
        };

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(_importer.Result.StatBlocks, _received?.Result);
        Assert.Contains("7 new and updated 2 stat blocks and 3 conditions, from 5 documents", _viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("4 conditions were left as the Player's Handbook defines them", _viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("1 documents could not be downloaded", _viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task RunCommand_NothingFound_SaysSo()
    {
        _viewModel.PdfPath = "catalog.pdf";
        _importer.Result = new HomebreweryImportResult { DocumentsRead = 3 };

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("Done, but the 3 documents read contain no stat blocks or conditions.", _viewModel.StatusMessage);
    }

    [Fact]
    public async Task RunCommand_ProgressLines_GoToTheLog()
    {
        _viewModel.PdfPath = "catalog.pdf";
        _importer.Output = ["Downloading 2 Homebrewery documents...", "Could not download x"];

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(
            "Downloading 2 Homebrewery documents..." + Environment.NewLine + "Could not download x" + Environment.NewLine,
            _viewModel.Log);
    }

    [Fact]
    public async Task RunCommand_ImporterRefuses_ShowsItsMessage()
    {
        _viewModel.PdfPath = "catalog.pdf";
        _importer.Failure = new ConversionException("'catalog.pdf' could not be read as a PDF.");

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("'catalog.pdf' could not be read as a PDF.", _viewModel.StatusMessage);
        Assert.Null(_received);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task RunCancelCommand_StopsTheRun()
    {
        _viewModel.PdfPath = "catalog.pdf";
        _importer.RunsUntilCancelled = true;

        Task run = _viewModel.RunCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsRunning);
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.RunCancelCommand.Execute(null);
        await run;

        Assert.Equal("Cancelled.", _viewModel.StatusMessage);
        Assert.Null(_received);
        Assert.False(_viewModel.IsRunning);
    }
}
