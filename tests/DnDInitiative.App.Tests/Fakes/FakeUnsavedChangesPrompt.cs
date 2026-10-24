using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeUnsavedChangesPrompt : IUnsavedChangesPrompt
{
    private readonly List<string> _questions = [];

    /// <summary>What the "user" answers. Don't save by default, so most tests can ignore it.</summary>
    public UnsavedChangesChoice Answer { get; set; } = UnsavedChangesChoice.Discard;

    public IReadOnlyList<string> Questions => _questions;

    public UnsavedChangesChoice Ask(string title, string message)
    {
        _questions.Add(message);
        return Answer;
    }
}
