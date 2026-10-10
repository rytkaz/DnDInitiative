namespace DnDInitiative.Core.StatBlocks;

/// <summary>Where a stat block came from, which decides whether it is synced with a campaign.</summary>
public enum StatBlockOrigin
{
    /// <summary>Created or edited by the user.</summary>
    Custom = 0,

    /// <summary>Brought in by an import and not changed since.</summary>
    Imported = 1,
}