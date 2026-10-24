using System.IO;

using DnDInitiative.Core.Campaigns;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeCampaignService : ICampaignService
{
    public Campaign? Current { get; set; } = new("C:\\campaigns\\first", "First");

    /// <summary>What <see cref="Inspect"/> reports for any folder.</summary>
    public CampaignFolderState FolderState { get; set; } = CampaignFolderState.Campaign;

    /// <summary>Makes the next open or Save As fail with this message.</summary>
    public string? FailureMessage { get; set; }

    /// <summary>The folder the campaign was last copied to by Save As, if any.</summary>
    public string? SavedAsFolder { get; private set; }

    public event EventHandler? FilesChangedExternally;

    public CampaignFolderState Inspect(string folder)
    {
        return FolderState;
    }

    public Task<Campaign> OpenAsync(string folder, CancellationToken cancellationToken = default)
    {
        if (FailureMessage is not null)
        {
            throw new CampaignException(FailureMessage);
        }

        // Named after the folder, like a newly created campaign.
        Current = new Campaign(folder, Path.GetFileName(folder));
        return Task.FromResult(Current);
    }

    public Task<Campaign> SaveAsAsync(string folder, CancellationToken cancellationToken = default)
    {
        SavedAsFolder = folder;
        return OpenAsync(folder, cancellationToken);
    }

    public Task<Campaign> OpenLastOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Current ?? new Campaign("C:\\campaigns\\default", "Default"));
    }

    /// <summary>Stands in for the file watcher noticing a change.</summary>
    public void RaiseFilesChanged()
    {
        FilesChangedExternally?.Invoke(this, EventArgs.Empty);
    }
}