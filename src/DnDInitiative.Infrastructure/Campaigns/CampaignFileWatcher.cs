namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>
/// Reports when files in a campaign folder are changed by something other than this app.
/// </summary>
internal sealed class CampaignFileWatcher : IDisposable
{
    // A burst of changes (one "git pull") is reported once, this long after the last of them.
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(500);

    private static readonly string GitFolder = $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}";

    private readonly CampaignLocation _location;
    private readonly Timer _timer;
    private FileSystemWatcher? _watcher;

    public CampaignFileWatcher(CampaignLocation location)
    {
        _location = location;

        // Created stopped. Every relevant change restarts the countdown.
        _timer = new Timer(_ => Changed?.Invoke(this, EventArgs.Empty), null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>Raised on a background thread.</summary>
    public event EventHandler? Changed;

    /// <summary>Starts watching a folder, and stops watching the previous one.</summary>
    public void Watch(string folder)
    {
        _watcher?.Dispose();
        _timer.Change(Timeout.Infinite, Timeout.Infinite);

        var watcher = new FileSystemWatcher(folder, "*.json")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        };
        watcher.Created += OnFileEvent;
        watcher.Changed += OnFileEvent;
        watcher.Deleted += OnFileEvent;
        watcher.Renamed += OnRenamed;

        // Windows gives up counting when too much changes at once. Something changed, so say so.
        watcher.Error += (_, _) => RestartCountdown();

        watcher.EnableRaisingEvents = true;
        _watcher = watcher;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _timer.Dispose();
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (IsExternalChange(e.FullPath))
        {
            RestartCountdown();
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (IsExternalChange(e.FullPath) || IsExternalChange(e.OldFullPath))
        {
            RestartCountdown();
        }
    }

    private bool IsExternalChange(string path)
    {
        return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            && !path.Contains(GitFolder, StringComparison.OrdinalIgnoreCase)
            && !_location.WasWrittenByApp(path);
    }

    private void RestartCountdown()
    {
        _timer.Change(QuietPeriod, Timeout.InfiniteTimeSpan);
    }
}