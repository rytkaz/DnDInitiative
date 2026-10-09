using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;

using DnDInitiative.Infrastructure.Import;

namespace DnDInitiative.Infrastructure.Tests.Import;

// These run real programs that ship with Windows, so they are Windows-only, like the app.
public class ProcessRunnerTests
{
    private readonly ProcessRunner _runner = new();

    [Fact]
    public async Task RunAsync_ReportsBothOutputStreamsAndTheExitCode()
    {
        var lines = new ConcurrentQueue<string>();

        int exitCode = await _runner.RunAsync(
            "cmd.exe",
            ["/c", "echo out& echo err 1>&2& exit 3"],
            workingDirectory: null,
            lines.Enqueue,
            CancellationToken.None);

        Assert.Equal(3, exitCode);
        Assert.Contains(lines, line => line.Trim() == "out");
        Assert.Contains(lines, line => line.Trim() == "err");
    }

    [Fact]
    public async Task RunAsync_Cancelled_StopsTheProgramAndThrows()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var stopwatch = Stopwatch.StartNew();

        // Left alone, this ping would take about thirty seconds.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _runner.RunAsync(
            "ping.exe",
            ["-n", "30", "127.0.0.1"],
            workingDirectory: null,
            _ => { },
            cancellation.Token));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task RunAsync_ProgramDoesNotExist_Throws()
    {
        await Assert.ThrowsAsync<Win32Exception>(() => _runner.RunAsync(
            "no-such-program-dndinitiative.exe",
            [],
            workingDirectory: null,
            _ => { },
            CancellationToken.None));
    }
}