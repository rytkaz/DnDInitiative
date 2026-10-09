namespace DnDInitiative.Core.StatBlocks;

/// <summary>
/// The few fields needed to show a stat block in a list.
/// </summary>
public sealed record StatBlockSummary(Guid Id, string Name, string ChallengeRating, string Source);