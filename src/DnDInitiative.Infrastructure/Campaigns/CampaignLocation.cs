using System.Collections.Concurrent;
using System.Diagnostics;

namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>
/// Which folder holds the open campaign, and which of its files this app has just written.
/// Shared by everything that reads, writes or watches campaign files.
/// </summary>
internal sealed class CampaignLocation
{
    // How long after writing a file its change notifications are treated as our own.
    private static readonly TimeSpan OwnWriteWindow = TimeSpan.FromSeconds(2);

    private readonly ConcurrentDictionary<string, long> _ownWrites = new(StringComparer.OrdinalIgnoreCase);
    private string? _folder;

    public bool IsOpen => _folder is not null;

    public string Folder
    {
        get => _folder ?? throw new InvalidOperationException("No campaign is open.");
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _folder = Path.GetFullPath(value);
        }
    }

    /// <summary>Call just before writing, renaming or deleting a campaign file.</summary>
    public void NoteOwnWrite(string path)
    {
        _ownWrites[Path.GetFullPath(path)] = Stopwatch.GetTimestamp();
    }

    /// <summary>Whether a change to this file is one the app just made itself.</summary>
    public bool WasWrittenByApp(string path)
    {
        return _ownWrites.TryGetValue(Path.GetFullPath(path), out long writtenAt)
            && Stopwatch.GetElapsedTime(writtenAt) < OwnWriteWindow;
    }
}