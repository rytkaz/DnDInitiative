using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Spells;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.ViewModels;

public class ImportViewModelTests
{
    private readonly InMemoryConversionSettingsStore _settingsStore = new();
    private readonly FakeStatBlockConverter _converter = new();
    private readonly FakeStatBlockImporter _importer = new();
    private readonly FakeFolderPicker _folderPicker = new();
    private readonly FakeFilePicker _filePicker = new();
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly ImportViewModel _viewModel;
    private readonly FakeConverterInstaller _installer = new();
    private readonly FakeSourceDataService _sourceData = new();
    private readonly FakeConverterConfigWriter _configWriter = new();
    private readonly InMemoryUiStateStore _uiState = new();
    private readonly FakeSpellImporter _spellImporter = new();
    private readonly FakeConditionImporter _conditionImporter = new();
    private StatBlocksImportedMessage? _received;

    public ImportViewModelTests()
    {
        _messenger.Register<ImportViewModelTests, StatBlocksImportedMessage>(
            this,
            (recipient, message) => recipient._received = message);

        _viewModel = new ImportViewModel(_settingsStore, _converter, _importer, _folderPicker, _filePicker, _messenger, _installer, _sourceData, _configWriter, _uiState, _spellImporter, _conditionImporter);
    }

    [Fact]
    public async Task LoadCommand_ShowsTheStoredPaths()
    {
        _settingsStore.Settings = new ConversionSettings { ToolPath = "tool.exe", DataDirectory = "data" };

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("tool.exe", _viewModel.ToolPath);
        Assert.Equal("data", _viewModel.DataDirectory);
    }

    [Fact]
    public void RunCommand_NeedsOnlyTheDataFolder()
    {
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.DataDirectory = "data";
        Assert.True(_viewModel.RunCommand.CanExecute(null));
    }

    [Fact]
    public async Task RunCommand_NoProgramChosenAndNoneInstalled_DownloadsItFirst()
    {
        FillInPaths();
        _viewModel.ToolPath = "";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(1, _installer.InstallCount);
        Assert.Equal("downloaded.exe", _converter.Request?.ToolPath);
        Assert.StartsWith("Downloading converter", _viewModel.Log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_NoProgramChosenButOneInstalled_UsesItWithoutDownloading()
    {
        FillInPaths();
        _viewModel.ToolPath = "";
        _installer.Installed = "installed.exe";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(0, _installer.InstallCount);
        Assert.Equal("installed.exe", _converter.Request?.ToolPath);
    }

    [Fact]
    public async Task RunCommand_DownloadFails_ShowsTheReasonAndDoesNotConvert()
    {
        FillInPaths();
        _viewModel.ToolPath = "";
        _installer.FailureMessage = "Check your internet connection.";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("Check your internet connection.", _viewModel.StatusMessage);
        Assert.Null(_converter.Request);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task UpdateConverterCommand_DownloadsEvenWhenOneIsInstalled()
    {
        _installer.Installed = "installed.exe";

        await _viewModel.UpdateConverterCommand.ExecuteAsync(null);

        Assert.Equal(1, _installer.InstallCount);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task RunCommand_SavesThePathsConvertsThenImportsWhatTheConverterWrote()
    {
        FillInPaths();

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("tool.exe", _settingsStore.Settings.ToolPath);
        Assert.Equal(new ConversionRequest("tool.exe", "data", "config.json"), _converter.Request);
        Assert.Equal("converted-notes", _importer.ImportedFolder);
        Assert.Equal(3, _received?.Result.Added);
        Assert.Contains("Imported 3 new", _viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task RunCommand_PathsPastedWithQuotesAndSpaces_AreCleanedUp()
    {
        FillInPaths();
        _viewModel.ToolPath = "  \"C:\\My Tools\\ttrpg-convert.exe\" ";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\My Tools\ttrpg-convert.exe", _converter.Request?.ToolPath);
    }

    [Fact]
    public async Task RunCommand_ShowsTheConvertersOutputInTheLog()
    {
        FillInPaths();
        _converter.Output = ["Reading data", "Writing notes"];

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(
            "Reading data" + Environment.NewLine + "Writing notes" + Environment.NewLine,
            _viewModel.Log);
    }

    [Fact]
    public async Task RunCommand_ConversionFails_ShowsTheReasonAndDoesNotImport()
    {
        FillInPaths();
        _converter.Failure = new ConversionException("The 5eTools data folder was not found.");

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("The 5eTools data folder was not found.", _viewModel.StatusMessage);
        Assert.Null(_importer.ImportedFolder);
        Assert.Null(_received);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task RunCancelCommand_StopsTheRunAndDoesNotImport()
    {
        FillInPaths();
        _converter.RunsUntilCancelled = true;

        Task run = _viewModel.RunCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsRunning);
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.RunCancelCommand.Execute(null);
        await run;

        Assert.Equal("Cancelled.", _viewModel.StatusMessage);
        Assert.Null(_importer.ImportedFolder);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public void BrowseCommands_FillInTheChosenPaths()
    {
        _filePicker.FileToReturn = @"C:\cli\ttrpg-convert.exe";
        _folderPicker.FolderToReturn = @"C:\cli\data";

        _viewModel.BrowseToolCommand.Execute(null);
        _viewModel.BrowseDataCommand.Execute(null);

        Assert.Equal(@"C:\cli\ttrpg-convert.exe", _viewModel.ToolPath);
        Assert.Equal(@"C:\cli\data", _viewModel.DataDirectory);
    }

    [Fact]
    public void BrowseCommands_DialogCancelled_KeepWhatWasThere()
    {
        _viewModel.ToolPath = "tool.exe";

        _viewModel.BrowseToolCommand.Execute(null);

        Assert.Equal("tool.exe", _viewModel.ToolPath);
    }

    [Fact]
    public async Task RunCommand_FolderIsNotFiveToolsData_ExplainsAndDoesNotStart()
    {
        FillInPaths();
        _sourceData.RecognisesFolders = false;

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Contains("bestiary", _viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Null(_converter.Request);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task RunCommand_FolderAboveTheDataWasChosen_UsesTheRealDataFolder()
    {
        FillInPaths();
        _sourceData.CorrectedFolder = @"picked\data";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(@"picked\data", _converter.Request?.DataDirectory);
        Assert.Equal(@"picked\data", _viewModel.DataDirectory);
    }

    [Fact]
    public async Task UseDataZipCommand_ZipChosen_UnpacksItAndRemembersTheFolder()
    {
        _filePicker.FileToReturn = @"C:\Downloads\5etools.zip";

        await _viewModel.UseDataZipCommand.ExecuteAsync(null);

        Assert.Equal(@"C:\Downloads\5etools.zip", _sourceData.ImportedZip);
        Assert.Equal("unpacked-data", _viewModel.DataDirectory);
        Assert.Equal("unpacked-data", _settingsStore.Settings.DataDirectory);
        Assert.False(_viewModel.IsRunning);
    }

    [Fact]
    public async Task UseDataZipCommand_WrongZip_ShowsWhyAndKeepsTheCurrentFolder()
    {
        _viewModel.DataDirectory = "data";
        _filePicker.FileToReturn = @"C:\Downloads\holiday.zip";
        _sourceData.FailureMessage = "That zip does not contain 5eTools data.";

        await _viewModel.UseDataZipCommand.ExecuteAsync(null);

        Assert.Equal("That zip does not contain 5eTools data.", _viewModel.StatusMessage);
        Assert.Equal("data", _viewModel.DataDirectory);
    }

    [Fact]
    public async Task UseDataZipCommand_DialogCancelled_DoesNothing()
    {
        _filePicker.FileToReturn = null;

        await _viewModel.UseDataZipCommand.ExecuteAsync(null);

        Assert.Null(_sourceData.ImportedZip);
    }

    [Fact]
    public async Task LoadCommand_ListsTheBooksInTheDataFolderAndRestoresTheTicks()
    {
        _settingsStore.Settings = new ConversionSettings { DataDirectory = "data", Sources = ["XMM"] };
        _sourceData.Sources = [new SourceBook("MM", "Monster Manual"), new SourceBook("XMM", "Monster Manual (2024)")];

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.Sources.Count);
        Assert.False(_viewModel.Sources[0].IsSelected);
        Assert.True(_viewModel.Sources[1].IsSelected);
        Assert.Equal("Monster Manual (2024) (XMM)", _viewModel.Sources[1].Label);
    }

    [Fact]
    public async Task RunCommand_NoConfigurationChosen_WritesOneForTheTickedBooksAndRemembersThem()
    {
        _settingsStore.Settings = new ConversionSettings { DataDirectory = "data" };
        _sourceData.Sources = [new SourceBook("MM", "Monster Manual"), new SourceBook("XMM", "Monster Manual (2024)")];
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.Sources[1].IsSelected = true;

        await _viewModel.RunCommand.ExecuteAsync(null);

        string[] expected = ["XMM"];
        Assert.Equal(expected, _configWriter.WrittenFor?.ToArray());
        Assert.Equal("generated-config.json", _converter.Request?.ConfigPath);
        Assert.Equal(expected, _settingsStore.Settings.Sources.ToArray());
    }

    [Fact]
    public async Task RunCommand_OwnConfigurationChosen_UsesItAndWritesNothing()
    {
        FillInPaths();

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("config.json", _converter.Request?.ConfigPath);
        Assert.Null(_configWriter.WrittenFor);
    }

    [Fact]
    public async Task SelectAllAndClearSourcesCommands_TickAndUntickEveryBook()
    {
        _settingsStore.Settings = new ConversionSettings { DataDirectory = "data" };
        _sourceData.Sources = [new SourceBook("MM", "Monster Manual"), new SourceBook("XMM", "Monster Manual (2024)")];
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SelectAllSourcesCommand.Execute(null);
        Assert.All(_viewModel.Sources, source => Assert.True(source.IsSelected));

        _viewModel.ClearSourcesCommand.Execute(null);
        Assert.All(_viewModel.Sources, source => Assert.False(source.IsSelected));
    }

    [Fact]
    public async Task SourceSearchText_HidesBooksThatDoNotMatchByNameOrAbbreviation()
    {
        await LoadThreeBooksAsync();

        _viewModel.SourceSearchText = "monster";
        bool[] expected = [true, true, false];
        Assert.Equal(expected, _viewModel.Sources.Select(source => source.IsVisible).ToArray());

        _viewModel.SourceSearchText = " xmm ";
        bool[] expectedAfterNarrowing = [false, true, false];
        Assert.Equal(expectedAfterNarrowing, _viewModel.Sources.Select(source => source.IsVisible).ToArray());

        _viewModel.SourceSearchText = "";
        Assert.All(_viewModel.Sources, source => Assert.True(source.IsVisible));
    }

    [Fact]
    public async Task SelectAllSourcesCommand_WhileSearching_TicksOnlyTheMatches()
    {
        await LoadThreeBooksAsync();
        _viewModel.SourceSearchText = "monster";

        _viewModel.SelectAllSourcesCommand.Execute(null);

        bool[] expected = [true, true, false];
        Assert.Equal(expected, _viewModel.Sources.Select(source => source.IsSelected).ToArray());
    }

    [Fact]
    public async Task ClearSourcesCommand_WhileSearching_KeepsTheTicksOnHiddenBooks()
    {
        await LoadThreeBooksAsync();
        _viewModel.SelectAllSourcesCommand.Execute(null);
        _viewModel.SourceSearchText = "monster";

        _viewModel.ClearSourcesCommand.Execute(null);

        bool[] expected = [false, false, true];
        Assert.Equal(expected, _viewModel.Sources.Select(source => source.IsSelected).ToArray());
    }

    [Fact]
    public async Task RunCommand_WhileSearching_StillImportsTickedBooksTheSearchIsHiding()
    {
        await LoadThreeBooksAsync();
        _viewModel.SelectAllSourcesCommand.Execute(null);
        _viewModel.SourceSearchText = "volo";

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(3, _configWriter.WrittenFor?.Count);
    }

    [Theory]
    [InlineData(300, 300)]
    [InlineData(10, 100)]
    [InlineData(5000, 800)]
    public void SourcesPanelHeight_IsKeptWithinLimits(double requested, double expected)
    {
        _viewModel.SourcesPanelHeight = requested;

        Assert.Equal(expected, _viewModel.SourcesPanelHeight);
    }

    [Fact]
    public void Constructor_UsesTheSavedSourcesPanelHeight()
    {
        _uiState.State = new UiState { SourcesPanelHeight = 420 };

        var viewModel = new ImportViewModel(_settingsStore, _converter, _importer, _folderPicker, _filePicker, _messenger, _installer, _sourceData, _configWriter, _uiState, _spellImporter, _conditionImporter);

        Assert.Equal(420, viewModel.SourcesPanelHeight);
    }

    [Fact]
    public void SaveLayout_SavesTheSourcesPanelHeightAndKeepsTheRestOfTheState()
    {
        _uiState.State = new UiState { IsWindowMaximized = true, StatBlockPanelWidth = 500 };

        _viewModel.SourcesPanelHeight = 300;
        Assert.Equal(0, _uiState.SaveCount);

        _viewModel.SaveLayout();

        Assert.Equal(new UiState { IsWindowMaximized = true, StatBlockPanelWidth = 500, SourcesPanelHeight = 300 }, _uiState.State);
    }

    [Fact]
    public async Task RunCommand_StatBlocksImported_ThrowsTheNotesAway()
    {
        FillInPaths();

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(1, _converter.DiscardCount);
    }

    [Fact]
    public async Task RunCommand_ImportsTheSpellsFromTheSameNotesAndReportsThem()
    {
        FillInPaths();
        _spellImporter.Result = new SpellSaveResult(Added: 300, Updated: 20);

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("converted-notes", _spellImporter.ImportedFolder);
        Assert.Contains("320 spells", _viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_ImportsTheConditionsFromTheSameNotesAndReportsThem()
    {
        FillInPaths();
        _conditionImporter.Result = 15;

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("converted-notes", _conditionImporter.ImportedFolder);
        Assert.Contains("15 conditions", _viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_OnlySpellsImported_StillCountsAsASuccessfulRun()
    {
        FillInPaths();
        _importer.Result = new StatBlockImportResult(Added: 0, Updated: 0, FilesScanned: 12);
        _spellImporter.Result = new SpellSaveResult(Added: 5, Updated: 0);

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(1, _converter.DiscardCount);
        Assert.Contains("5 spells", _viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_NoConfigurationChosen_AsksForTheSpellsOfEveryBookThatHasThem()
    {
        _settingsStore.Settings = new ConversionSettings { DataDirectory = "data" };
        _sourceData.SpellSources = ["PHB", "XPHB"];
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.RunCommand.ExecuteAsync(null);

        string[] expected = ["PHB", "XPHB"];
        Assert.Equal(expected, _configWriter.Options?.SpellSources.ToArray());
    }

    [Fact]
    public async Task RunCommand_NothingImported_KeepsTheNotesAndSaysWhereTheyAre()
    {
        FillInPaths();
        _importer.Result = new StatBlockImportResult(Added: 0, Updated: 0, FilesScanned: 12);

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(0, _converter.DiscardCount);
        Assert.Contains("converted-notes", _viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_ConversionFails_LeavesTheNotesForTheNextRunToClearAway()
    {
        FillInPaths();
        _converter.Failure = new ConversionException("The converter stopped.");

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal(0, _converter.DiscardCount);
    }

    [Fact]
    public async Task AddHomebrewCommand_FilePicked_ListsItAndSavesItStraightAway()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _filePicker.FileToReturn = "brew.json";

        await _viewModel.AddHomebrewCommand.ExecuteAsync(null);

        HomebrewFileViewModel file = Assert.Single(_viewModel.HomebrewFiles);
        Assert.Equal("brew.json", file.Path);
        Assert.Equal("Brew Book (Brew) - 12 monsters", file.Label);
        string[] expected = ["brew.json"];
        Assert.Equal(expected, _settingsStore.Settings.HomebrewFiles.ToArray());
    }

    [Fact]
    public async Task AddHomebrewCommand_PickerCancelled_ChangesNothing()
    {
        _filePicker.FileToReturn = null;

        await _viewModel.AddHomebrewCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.HomebrewFiles);
    }

    [Fact]
    public async Task AddHomebrewCommand_SameFileTwice_KeepsOneAndSaysSo()
    {
        _filePicker.FileToReturn = "brew.json";
        await _viewModel.AddHomebrewCommand.ExecuteAsync(null);
        _filePicker.FileToReturn = "BREW.json";

        await _viewModel.AddHomebrewCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.HomebrewFiles);
        Assert.Contains("already", _viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddHomebrewCommand_NotAHomebrewFile_ShowsWhyAndAddsNothing()
    {
        _sourceData.UnreadableHomebrewFiles.Add("notes.json");
        _filePicker.FileToReturn = "notes.json";

        await _viewModel.AddHomebrewCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.HomebrewFiles);
        Assert.Contains("notes.json", _viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Empty(_settingsStore.Settings.HomebrewFiles);
    }

    [Fact]
    public async Task RemoveHomebrewCommand_RemovesTheFileAndSaves()
    {
        _settingsStore.Settings = new ConversionSettings { HomebrewFiles = ["one.json", "two.json"] };
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.RemoveHomebrewCommand.ExecuteAsync(_viewModel.HomebrewFiles[0]);

        string[] expected = ["two.json"];
        Assert.Equal(expected, _viewModel.HomebrewFiles.Select(file => file.Path).ToArray());
        Assert.Equal(expected, _settingsStore.Settings.HomebrewFiles.ToArray());
    }

    [Fact]
    public async Task LoadCommand_SavedHomebrewFileIsGone_LeavesItOutAndSaysSo()
    {
        _settingsStore.Settings = new ConversionSettings { HomebrewFiles = ["kept.json", "gone.json"] };
        _sourceData.UnreadableHomebrewFiles.Add("gone.json");

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("kept.json", Assert.Single(_viewModel.HomebrewFiles).Path);
        Assert.Contains("gone.json", _viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCommand_NoConfigurationChosen_WritesOneThatIncludesTheHomebrewFiles()
    {
        _settingsStore.Settings = new ConversionSettings { DataDirectory = "data", HomebrewFiles = ["brew.json"] };
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.RunCommand.ExecuteAsync(null);

        string[] expected = ["brew.json"];
        Assert.Equal(expected, _configWriter.WrittenHomebrew?.ToArray());
        Assert.Equal(expected, _settingsStore.Settings.HomebrewFiles.ToArray());
    }

    private async Task LoadThreeBooksAsync()
    {
        _settingsStore.Settings = new ConversionSettings { DataDirectory = "data" };
        _sourceData.Sources =
        [
            new SourceBook("MM", "Monster Manual"),
            new SourceBook("XMM", "Monster Manual (2024)"),
            new SourceBook("VGM", "Volo's Guide"),
        ];
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void FillInPaths()
    {
        _viewModel.ToolPath = "tool.exe";
        _viewModel.ConfigPath = "config.json";
        _viewModel.DataDirectory = "data";
    }
}