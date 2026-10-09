using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeConfirmation : IConfirmationService
{
    private readonly List<string> _questions = [];

    /// <summary>What the "user" answers. Yes by default, so most tests can ignore it.</summary>
    public bool Answer { get; set; } = true;

    public IReadOnlyList<string> Questions => _questions;

    public bool Confirm(string title, string message)
    {
        _questions.Add(message);
        return Answer;
    }
}