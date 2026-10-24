namespace DnDInitiative.App.Services;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel,
}

/// <summary>Asks what to do with unsaved changes before something would discard them.</summary>
public interface IUnsavedChangesPrompt
{
    /// <returns>Save first, go ahead without saving, or stay where things are.</returns>
    UnsavedChangesChoice Ask(string title, string message);
}
