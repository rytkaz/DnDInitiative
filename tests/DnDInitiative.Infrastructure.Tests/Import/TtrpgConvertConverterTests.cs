using System.ComponentModel;

using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Import;

public sealed class TtrpgConvertConverterTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly string _dataDirectory;
    private readonly string _outputDirectory;
    private readonly string _configPath;
    private readonly FakeProcessRunner _runner = new();
    private readonly TtrpgConvertConverter _converter;

    public TtrpgConvertConverterTests()
    {
        _dataDirectory = Directory.CreateDirectory(Path.Combine(_root, "data")).FullName;
        // Where the converter keeps its notes: a fixed place inside the app's data folder.
        _outputDirectory = Path.Combine(_root, "appdata", "converter", "output");
        _configPath = Path.Combine(_root, "config", "my-config.json");
        Directory.CreateDirectory(Path.Combine(_root, "config"));
        File.WriteAllText(_configPath, "{}");

        _converter = new TtrpgConvertConverter(_runner, new StorageOptions(Path.Combine(_root, "appdata")));
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task ConvertAsync_WithConfig_PassesOutputConfigAndDataAndRunsBesideTheConfig()
    {
        await _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory, _configPath));

        string[] expected = ["-o", _outputDirectory, "-c", _configPath, _dataDirectory];
        Assert.Equal("ttrpg-convert", _runner.FileName);
        Assert.Equal(expected, _runner.Arguments.ToArray());
        Assert.Equal(Path.GetDirectoryName(_configPath), _runner.WorkingDirectory);
        Assert.True(Directory.Exists(_outputDirectory));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task ConvertAsync_WithoutConfig_LeavesTheConfigArgumentOut(string? configPath)
    {
        await _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory, configPath));

        string[] expected = ["-o", _outputDirectory, _dataDirectory];
        Assert.Equal(expected, _runner.Arguments.ToArray());
        Assert.Null(_runner.WorkingDirectory);
    }

    [Fact]
    public async Task ConvertAsync_ReportsOutputLinesWithoutColourCodes()
    {
        _runner.Output = ["\u001b[32mWriting monsters\u001b[0m", "Done"];
        var progress = new RecordingProgress();

        await _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory), progress);

        string[] expected = ["Writing monsters", "Done"];
        Assert.Equal(expected, progress.Lines.ToArray());
    }

    [Fact]
    public async Task ConvertAsync_NonZeroExitCode_ThrowsWithTheLastOutput()
    {
        _runner.ExitCode = 1;
        _runner.Output = ["Reading data", "Unable to read config"];

        ConversionException exception = await Assert.ThrowsAsync<ConversionException>(
            () => _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory)));

        Assert.Contains("exit code 1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Unable to read config", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConvertAsync_MissingDataFolder_ThrowsWithoutRunningAnything()
    {
        var request = new ConversionRequest("ttrpg-convert", Path.Combine(_root, "missing"));

        await Assert.ThrowsAsync<ConversionException>(() => _converter.ConvertAsync(request));

        Assert.Null(_runner.FileName);
    }

    [Fact]
    public async Task ConvertAsync_MissingConfigFile_ThrowsWithoutRunningAnything()
    {
        var request = new ConversionRequest("ttrpg-convert", _dataDirectory, Path.Combine(_root, "nope.json"));

        await Assert.ThrowsAsync<ConversionException>(() => _converter.ConvertAsync(request));

        Assert.Null(_runner.FileName);
    }

    [Fact]
    public async Task ConvertAsync_ProgramCannotBeStarted_ThrowsAReadableError()
    {
        _runner.StartFailure = new Win32Exception(2);

        ConversionException exception = await Assert.ThrowsAsync<ConversionException>(
            () => _converter.ConvertAsync(new ConversionRequest(@"C:\nowhere\tool.exe", _dataDirectory)));

        Assert.Contains(@"C:\nowhere\tool.exe", exception.Message, StringComparison.Ordinal);
        Assert.IsType<Win32Exception>(exception.InnerException);
    }

    [Fact]
    public async Task ConvertAsync_Cancelled_LetsTheCancellationThrough()
    {
        _runner.StartFailure = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory)));
    }

    [Fact]
    public async Task ConvertAsync_ReturnsTheFolderTheNotesWereWrittenTo()
    {
        string notesFolder = await _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory));

        Assert.Equal(_outputDirectory, notesFolder);
    }

    [Fact]
    public async Task ConvertAsync_NotesLeftByAnEarlierRun_AreRemovedFirst()
    {
        Directory.CreateDirectory(_outputDirectory);
        string staleNote = Path.Combine(_outputDirectory, "goblin.md");
        await File.WriteAllTextAsync(staleNote, "from a book that is no longer ticked");

        await _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory));

        Assert.False(File.Exists(staleNote));
        Assert.True(Directory.Exists(_outputDirectory));
    }

    [Fact]
    public async Task DiscardOutput_DeletesTheNotes()
    {
        await _converter.ConvertAsync(new ConversionRequest("ttrpg-convert", _dataDirectory));
        await File.WriteAllTextAsync(Path.Combine(_outputDirectory, "goblin.md"), "a note");

        _converter.DiscardOutput();

        Assert.False(Directory.Exists(_outputDirectory));
    }

    [Fact]
    public void DiscardOutput_NothingToDelete_DoesNothing()
    {
        _converter.DiscardOutput();

        Assert.False(Directory.Exists(_outputDirectory));
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        public int ExitCode { get; set; }

        public IReadOnlyList<string> Output { get; set; } = [];

        public Exception? StartFailure { get; set; }

        public string? FileName { get; private set; }

        public IReadOnlyList<string> Arguments { get; private set; } = [];

        public string? WorkingDirectory { get; private set; }

        public Task<int> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            string? workingDirectory,
            Action<string> onOutput,
            CancellationToken cancellationToken)
        {
            FileName = fileName;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;

            if (StartFailure is not null)
            {
                throw StartFailure;
            }

            foreach (string line in Output)
            {
                onOutput(line);
            }

            return Task.FromResult(ExitCode);
        }
    }

    // Progress<T> posts its reports to a thread pool or UI thread, which a test would have
    // to wait for. This one records them straight away.
    private sealed class RecordingProgress : IProgress<string>
    {
        public List<string> Lines { get; } = [];

        public void Report(string value)
        {
            Lines.Add(value);
        }
    }
}