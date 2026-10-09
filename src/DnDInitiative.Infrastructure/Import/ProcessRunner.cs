using System.Diagnostics;
using System.Text;

namespace DnDInitiative.Infrastructure.Import;

internal sealed class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,

            // Start the program directly instead of through the Windows shell.
            // Required for capturing its output.
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };

        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // Added one by one so .NET quotes each argument; paths with spaces stay in one piece.
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        // Data is null once, when the stream closes.
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Also waits until both output streams have been read to the end.
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelling only stops the waiting. The program itself has to be stopped by force,
            // together with anything it started.
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // It finished by itself in the meantime.
            }

            // Do not return while it is still shutting down and holding files open.
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        return process.ExitCode;
    }
}