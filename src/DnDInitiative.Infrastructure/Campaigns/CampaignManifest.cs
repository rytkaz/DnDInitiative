namespace DnDInitiative.Infrastructure.Campaigns;

/// <summary>The contents of campaign.json, the file that marks a folder as a campaign.</summary>
internal sealed record CampaignManifest(int FormatVersion, string Name);