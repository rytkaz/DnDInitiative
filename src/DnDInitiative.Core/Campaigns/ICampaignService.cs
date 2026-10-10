namespace DnDInitiative.Core.Campaigns;

/// <summary>A folder holding one campaign's parties, encounters and stat blocks.</summary>
public sealed record Campaign(string Folder, string Name);

public interface ICampaignService
{
    /// <summary>The open campaign, or null before one has been opened.</summary>
    Campaign? Current { get; }

    /// <summary>
    /// Opens the campaign in a folder, creating the folder and an empty campaign if needed,
    /// and remembers it for the next start.
    /// </summary>
    /// <exception cref="CampaignException">The folder holds a campaign this version cannot use.</exception>
    Task<Campaign> OpenAsync(string folder, CancellationToken cancellationToken = default);

    /// <summary>Opens the campaign used last time, or the default one if that is not possible.</summary>
    Task<Campaign> OpenLastOrDefaultAsync(CancellationToken cancellationToken = default);
}