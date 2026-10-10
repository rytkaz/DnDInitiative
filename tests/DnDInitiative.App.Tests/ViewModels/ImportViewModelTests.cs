using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.App.ViewModels;
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
    private StatBlocksImportedMessage? _received;

    public ImportViewModelTests()
    {
        _messenger.Register<ImportViewModelTests, StatBlocksImportedMessage>(
            this,
            (recipient, message) => recipient._received = message);

        _viewModel = new ImportViewModel(_settingsStore, _converter, _importer, _folderPicker, _filePicker, _messenger, _installer, _sourceData);
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
    public void RunCommand_NeedsOnlyTheDataAndOutputFolders()
    {
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.DataDirectory = "data";
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.OutputDirectory = "output";
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
    public async Task RunCommand_SavesThePathsConvertsThenImportsTheOutput()
    {
        FillInPaths();

        await _viewModel.RunCommand.ExecuteAsync(null);

        Assert.Equal("tool.exe", _settingsStore.Settings.ToolPath);
        Assert.Equal(new ConversionRequest("tool.exe", "data", "output", "config.json"), _converter.Request);
        Assert.Equal("output", _importer.ImportedFolder);
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

    private void FillInPaths()
    {
        _viewModel.ToolPath = "tool.exe";
        _viewModel.ConfigPath = "config.json";
        _viewModel.DataDirectory = "data";
        _viewModel.OutputDirectory = "output";
    }
}