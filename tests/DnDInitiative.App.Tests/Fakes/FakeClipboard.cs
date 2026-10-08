using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeClipboard : IClipboardService
{
    public string? Text { get; private set; }

    public bool TrySetText(string text)
    {
        Text = text;
        return true;
    }
}