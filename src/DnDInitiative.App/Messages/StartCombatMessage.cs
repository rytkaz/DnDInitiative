using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Parties;

namespace DnDInitiative.App.Messages;

/// <summary>
/// Asks the tracker to begin a new combat, and the shell to show it.
/// </summary>
public sealed record StartCombatMessage(string Name, IReadOnlyList<CombatantBlueprint> Creatures, Party? Party);