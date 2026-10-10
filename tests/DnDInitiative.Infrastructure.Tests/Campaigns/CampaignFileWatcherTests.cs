using DnDInitiative.Infrastructure.Campaigns;

namespace DnDInitiative.Infrastructure.Tests.Campaigns;

// These use the real file system and real time, so they are slower than the other tests
// and allow generous margins.
public sealed class CampaignFileWatcherTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly CampaignLocation _location;
    private readonly CampaignFileWatcher _watcher;
    private readonly TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CampaignFileWatcherTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "parties"));
        _location = new CampaignLocation { Folder = _folder };
        _watcher = new CampaignFileWatcher(_location);
        _watcher.Changed += (_, _) => _changed.TrySetResult();
        _watcher.Watch(_folder);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task FileWrittenByAnotherProgram_IsReported()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "parties", "pulled.json"), "{}");

        await _changed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task FileWrittenByTheApp_IsNotReported()
    {
        string path = Path.Combine(_folder, "parties", "saved.json");
        _location.NoteOwnWrite(path);
        await File.WriteAllTextAsync(path, "{}");

        // Longer than the quiet period, so a report would have arrived by now.
        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.False(_changed.Task.IsCompleted);
    }

    [Fact]
    public async Task FileThatIsNotCampaignData_IsNotReported()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder, "notes.txt"), "not json");

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        Assert.False(_changed.Task.IsCompleted);
    }
}