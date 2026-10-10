using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class InMemoryUiStateStore : IUiStateStore
{
    public UiState State { get; set; } = new();

    public int SaveCount { get; private set; }

    public UiState Load()
    {
        return State;
    }

    public void Save(UiState state)
    {
        State = state;
        SaveCount++;
    }
}
