using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;

namespace DnDInitiative.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IRecipient<StartCombatMessage>
{
    private readonly InitiativeTrackerViewModel _initiativeTracker;
    private readonly IConfirmationService _confirmation;
    private PageViewModel _currentPage;

    public MainViewModel(
        InitiativeTrackerViewModel initiativeTracker,
        StatBlocksViewModel statBlocks,
        EncounterBuilderViewModel encounterBuilder,
        PartySetupViewModel partySetup,
        IMessenger messenger,
        IConfirmationService confirmation)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        _initiativeTracker = initiativeTracker;
        _confirmation = confirmation;
        Pages = [initiativeTracker, statBlocks, encounterBuilder, partySetup];
        _currentPage = initiativeTracker;
        messenger.Register<StartCombatMessage>(this);
    }

    /// <summary>Returns false if the user chose to stay because of unsaved changes.</summary>
    public bool ConfirmClose()
    {
        List<string> unsaved = Pages.SelectMany(page => page.UnsavedChanges).ToList();
        if (unsaved.Count == 0)
        {
            return true;
        }

        string blankLine = Environment.NewLine + Environment.NewLine;
        string message = "These have unsaved changes that will be lost:"
            + blankLine
            + string.Join(Environment.NewLine, unsaved)
            + blankLine
            + "Close anyway?";

        return _confirmation.Confirm("Unsaved changes", message);
    }

    public IReadOnlyList<PageViewModel> Pages { get; }

    public PageViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public void Receive(StartCombatMessage message)
    {
        CurrentPage = _initiativeTracker;
    }
}