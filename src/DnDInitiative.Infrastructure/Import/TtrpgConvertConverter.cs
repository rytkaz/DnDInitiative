using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.Infrastructure.Import;

internal sealed partial class TtrpgConvertConverter : IStatBlockConverter
{
    // How much of the converter's output is quoted when it fails.
    private const int ErrorTailLines = 15;

    private readonly IProcessRunner _processRunner;

    public TtrpgConvertConverter(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task ConvertAsync(
        ConversionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? configPath = string.IsNullOrWhiteSpace(request.ConfigPath) ? null : request.ConfigPath;

        // Checked here so the user gets a plain sentence instead of the converter's stack trace.
        if (string.IsNullOrWhiteSpace(request.ToolPath))
        {
            throw new ConversionException("Choose the ttrpg-convert program first.");
        }

        if (!Directory.Exists(request.DataDirectory))
        {
            throw new ConversionException($"The 5eTools data folder was not found: {request.DataDirectory}");
        }

        if (configPath is not null && !File.Exists(configPath))
        {
            throw new ConversionException($"The configuration file was not found: {configPath}");
        }

        Directory.CreateDirectory(request.OutputDirectory);

        // Output lines arrive from two background threads at once, hence the concurrent collection.
        var tail = new ConcurrentQueue<string>();

        void OnOutput(string rawLine)
        {
            string line = AnsiEscape().Replace(rawLine, "");
            progress?.Report(line);

            tail.Enqueue(line);
            while (tail.Count > ErrorTailLines)
            {
                tail.TryDequeue(out _);
            }
        }

        // Paths inside the configuration file (templates, homebrew) are relative to the folder
        // the converter runs in, so run it next to that file, as you would from a terminal.
        string? workingDirectory = configPath is null ? null : Path.GetDirectoryName(Path.GetFullPath(configPath));

        int exitCode;
        try
        {
            exitCode = await _processRunner
                .RunAsync(request.ToolPath, BuildArguments(request, configPath), workingDirectory, OnOutput, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Win32Exception exception)
        {
            // What Windows reports when the file is missing or is not a program.
            throw new ConversionException(
                $"Could not start '{request.ToolPath}'. Check that it points at the ttrpg-convert program.",
                exception);
        }

        if (exitCode != 0)
        {
            throw new ConversionException(string.Create(
                CultureInfo.InvariantCulture,
                $"ttrpg-convert failed (exit code {exitCode}).{Environment.NewLine}{string.Join(Environment.NewLine, tail)}"));
        }
    }

    private static List<string> BuildArguments(ConversionRequest request, string? configPath)
    {
        // Full paths, because the working directory may not be the one they were relative to.
        List<string> arguments = ["-o", Path.GetFullPath(request.OutputDirectory)];

        if (configPath is not null)
        {
            arguments.Add("-c");
            arguments.Add(Path.GetFullPath(configPath));
        }

        arguments.Add(Path.GetFullPath(request.DataDirectory));
        return arguments;
    }

    // Colour codes some console programs print; they would show up as garbage in a text box.
    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]")]
    private static partial Regex AnsiEscape();
}