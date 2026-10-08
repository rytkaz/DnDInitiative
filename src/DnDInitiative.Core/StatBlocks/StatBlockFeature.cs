namespace DnDInitiative.Core.StatBlocks;

/// <summary>
/// One named entry in a stat block: a trait, action, bonus action, reaction or legendary action.
/// </summary>
public sealed record StatBlockFeature(string Name, string Description);