using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DnDInitiative.App.Services;

using DnDInitiative.Core.Conditions;
using DnDInitiative.Core.Spells;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// The popup that opens when a link to a spell or a condition is clicked: what it shows, and
/// whether it is open. There is one for the whole app, because such links appear on every page
/// that shows a stat block, and in the popup itself.
/// </summary>
public sealed partial class ReferenceViewerViewModel : ObservableObject
{
    private readonly ISpellRepository _spells;
    private readonly IConditionRepository _conditions;

    // Increased for every lookup, so a slow one cannot replace a newer one.
    private int _requestVersion;

    [ObservableProperty]
    private IReferenceCard? _details;

    [ObservableProperty]
    private bool _isOpen;

    public ReferenceViewerViewModel(ISpellRepository spells, IConditionRepository conditions, IUiStateStore uiState)
    {
        _spells = spells;
        _conditions = conditions;
        CardWidth = new PanelSize(uiState, "Reference.CardWidth", defaultValue: 460, minimum: 320, maximum: 1000);

        // The card is only as tall as its text, up to this height; then it scrolls.
        CardMaxHeight = new PanelSize(uiState, "Reference.CardMaxHeight", defaultValue: 620, minimum: 240, maximum: 1400);
    }

    public PanelSize CardWidth { get; }

    public PanelSize CardMaxHeight { get; }

    /// <param name="reference">What the link said: a spell's key, or its name.</param>
    // Concurrent on purpose: a second link can be clicked while the first lookup is still running.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowSpellAsync(string? reference)
    {
        return ShowAsync(reference, async text =>
            await _spells.FindAsync(text) is { } spell
                ? new SpellDetailsViewModel(spell)
                : SpellDetailsViewModel.NotFound(text));
    }

    /// <param name="name">The condition's name, as the link gave it.</param>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowConditionAsync(string? name)
    {
        return ShowAsync(name, async text =>
            await _conditions.FindAsync(text) is { } condition
                ? new ConditionDetailsViewModel(condition)
                : ConditionDetailsViewModel.NotFound(text));
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    // A link to something that was never imported still opens the popup, to say so.
    // Doing nothing would look like a broken link.
    private async Task ShowAsync(string? reference, Func<string, Task<IReferenceCard>> load)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        int version = ++_requestVersion;
        IReferenceCard card = await load(reference.Trim());

        if (version != _requestVersion)
        {
            return;
        }

        Details = card;
        IsOpen = true;
    }
}
