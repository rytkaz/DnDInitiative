namespace DnDInitiative.Core.Campaigns;

/// <summary>A folder holding one campaign's parties, encounters, stat blocks and spellbooks.</summary>
public sealed record Campaign(string Folder, string Name);

public interface ICampaignService
{
    /// <summary>
    /// Raised when campaign files were changed by something other than this app, for example
    /// a "git pull". May be raised on a background thread.
    /// </summary>
    event EventHandler? FilesChangedExternally;

    /// <summary>The open campaign, or null before one has been opened.</summary>
    Campaign? Current { get; }

    /// <summary>Looks at a folder without changing it.</summary>
    CampaignFolderState Inspect(string folder);

    /// <summary>
    /// Opens the campaign in a folder, creating the folder and an empty campaign if needed,
    /// and remembers it for the next start.
    /// </summary>
    /// <exception cref="CampaignException">The folder holds a campaign this version cannot use.</exception>
    Task<Campaign> OpenAsync(string folder, CancellationToken cancellationToken = default);

    /// <summary>Opens the campaign used last time, or the default one if that is not possible.</summary>
    Task<Campaign> OpenLastOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies the open campaign's saved files to an empty folder and opens the copy, which is
    /// named after its folder. The original folder is left as it is.
    /// </summary>
    /// <exception cref="CampaignException">The folder is not empty, is inside the open campaign, or could not be written.</exception>
    Task<Campaign> SaveAsAsync(string folder, CancellationToken cancellationToken = default);
}

/// <summary>What a folder contains, from the point of view of opening it as a campaign.</summary>
public enum CampaignFolderState
{
    /// <summary>Nothing in it, or it does not exist yet. Opening it starts a new campaign.</summary>
    Empty,

    /// <summary>Already a campaign.</summary>
    Campaign,

    /// <summary>Holds other files and no campaign. Probably not what the user meant to pick.</summary>
    HasOtherFiles,
}