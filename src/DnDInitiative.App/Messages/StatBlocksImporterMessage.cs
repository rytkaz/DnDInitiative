using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Messages;

/// <summary>Sent after stat blocks were added to the library from outside the Stat Blocks page.</summary>
public sealed record StatBlocksImportedMessage(StatBlockImportResult Result);