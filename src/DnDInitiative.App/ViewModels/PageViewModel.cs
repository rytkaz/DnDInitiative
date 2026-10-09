using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Base class for anything the main window can navigate to.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }

    /// <summary>
    /// A description of each thing on this page that would be lost if the app closed now.
    /// </summary>
    public virtual IReadOnlyList<string> UnsavedChanges => [];
}