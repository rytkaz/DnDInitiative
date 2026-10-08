using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;

namespace DnDInitiative.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IRecipient<StartCombatMessage>
{
    private readonly InitiativeTrackerViewModel _initiativeTracker;
    private PageViewModel _currentPage;

    public MainViewModel(
        InitiativeTrackerViewModel initiativeTracker,
        StatBlocksViewModel statBlocks,
        EncounterBuilderViewModel encounterBuilder,
        PartySetupViewModel partySetup,
        IMessenger messenger)
    {
        ArgumentNullException.ThrowIfNull(messenger);

        _initiativeTracker = initiativeTracker;
        Pages = [initiativeTracker, statBlocks, encounterBuilder, partySetup];
        _currentPage = initiativeTracker;
        messenger.Register<StartCombatMessage>(this);
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