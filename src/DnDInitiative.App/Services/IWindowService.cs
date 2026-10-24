namespace DnDInitiative.App.Services;

/// <summary>The imports that the Import menu opens, each in its own window.</summary>
public enum ImportKind
{
    FiveETools,
    Homebrewery,
    MartialExploits,
}

/// <summary>Opens the app's secondary windows, so view models never create a window themselves.</summary>
public interface IWindowService
{
    /// <summary>Opens the window for this import, or brings it to the front if it is already open.</summary>
    void ShowImport(ImportKind kind);

    /// <summary>
    /// Closes every window this service opened, cancelling a running import without asking.
    /// For when the main window closes and the app goes with it.
    /// </summary>
    void CloseAll();
}
