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

    /// <summary>
    /// Called after a different campaign was opened. Drop everything that belonged to the old
    /// campaign, unsaved edits included, and load from the new one.
    /// </summary>
    public virtual Task ReloadCampaignDataAsync()
    {
        return Task.CompletedTask;
    }
}