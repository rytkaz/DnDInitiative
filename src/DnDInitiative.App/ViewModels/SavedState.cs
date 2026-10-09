using System.Text.Json;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Remembers what an editor would have saved at the last save or load,
/// so it can tell whether anything has changed since.
/// </summary>
internal sealed class SavedState<T>
{
    private readonly Func<T> _current;

    // Null until the first save or load: something that was never stored is always unsaved.
    private string? _saved;

    public SavedState(Func<T> current)
    {
        _current = current;
    }

    public bool HasChanges => _saved is null || !string.Equals(_saved, Serialize(), StringComparison.Ordinal);

    public void MarkSaved()
    {
        _saved = Serialize();
    }

    // Records compare their list properties by reference, so == cannot see whether two
    // parties have the same members. Comparing the JSON compares the whole content.
    private string Serialize()
    {
        return JsonSerializer.Serialize(_current());
    }
}