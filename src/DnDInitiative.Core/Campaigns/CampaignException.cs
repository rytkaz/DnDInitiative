namespace DnDInitiative.Core.Campaigns;

/// <summary>A campaign could not be opened, for a reason worth showing to the user.</summary>
public sealed class CampaignException : Exception
{
    public CampaignException()
    {
    }

    public CampaignException(string message)
        : base(message)
    {
    }

    public CampaignException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}