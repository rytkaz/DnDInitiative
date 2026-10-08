using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDInitiative.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private PageViewModel _currentPage;

    public MainViewModel(
        InitiativeTrackerViewModel initiativeTracker,
        StatBlocksViewModel statBlocks,
        EncounterBuilderViewModel encounterBuilder,
        PartySetupViewModel partySetup)
    {
        Pages = [initiativeTracker, statBlocks, encounterBuilder, partySetup];
        _currentPage = initiativeTracker;
    }

    public IReadOnlyList<PageViewModel> Pages { get; }

    public PageViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }
}