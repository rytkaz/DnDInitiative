using System.IO;

using DnDInitiative.Core.Campaigns;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeCampaignService : ICampaignService
{
    public Campaign? Current { get; set; } = new("C:\\campaigns\\first", "First");

    /// <summary>What <see cref="Inspect"/> reports for any folder.</summary>
    public CampaignFolderState FolderState { get; set; } = CampaignFolderState.Campaign;

    /// <summary>Makes the next open fail with this message.</summary>
    public string? FailureMessage { get; set; }

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

    public Task<Campaign> OpenLastOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Current ?? new Campaign("C:\\campaigns\\default", "Default"));
    }
}