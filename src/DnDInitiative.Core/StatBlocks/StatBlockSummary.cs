namespace DnDInitiative.Core.StatBlocks;

/// <summary>The few fields a list needs, so it does not have to load whole stat blocks.</summary>
public sealed record StatBlockSummary(
    Guid Id,
    string Name,
    string ChallengeRating,
    string Source,
    StatBlockOrigin Origin = StatBlockOrigin.Imported)
{
    /// <summary>Made or edited by the user, and therefore part of the campaign folder.</summary>
    public bool IsOwn => Origin == StatBlockOrigin.Custom;
}