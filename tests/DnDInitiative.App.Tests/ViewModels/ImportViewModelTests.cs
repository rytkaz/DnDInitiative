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
    private StatBlocksImportedMessage? _received;

    public ImportViewModelTests()
    {
        _messenger.Register<ImportViewModelTests, StatBlocksImportedMessage>(
            this,
            (recipient, message) => recipient._received = message);

        _viewModel = new ImportViewModel(_settingsStore, _converter, _importer, _folderPicker, _filePicker, _messenger);
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
    public void RunCommand_NeedsProgramDataAndOutputButNotAConfig()
    {
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.ToolPath = "tool.exe";
        _viewModel.DataDirectory = "data";
        Assert.False(_viewModel.RunCommand.CanExecute(null));

        _viewModel.OutputDirectory = "output";
        Assert.True(_viewModel.RunCommand.CanExecute(null));
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

    private void FillInPaths()
    {
        _viewModel.ToolPath = "tool.exe";
        _viewModel.ConfigPath = "config.json";
        _viewModel.DataDirectory = "data";
        _viewModel.OutputDirectory = "output";
    }
}