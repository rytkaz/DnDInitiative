namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>
/// Which folder holds the open campaign. Shared by everything that reads or writes campaign files.
/// </summary>
internal sealed class CampaignLocation
{
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
}