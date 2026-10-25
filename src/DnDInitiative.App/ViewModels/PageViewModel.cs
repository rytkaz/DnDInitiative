using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Base class for anything the main window can navigate to.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }

    /// <summary>A short name for places with little room, such as the page buttons in the top bar.</summary>
    public virtual string ShortTitle => Title;

    /// <summary>
    /// A description of each thing on this page that would be lost if the app closed now.
    /// </summary>
    public virtual IReadOnlyList<string> UnsavedChanges => [];

    /// <summary>
    /// Saves everything listed in <see cref="UnsavedChanges"/> that can be saved, and keeps it open.
    /// Returns a line for each thing that could not be saved, saying why.
    /// </summary>
    public virtual Task<IReadOnlyList<string>> SaveAllAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>([]);
    }

    /// <summary>
    /// Called after a different campaign was opened. Drop everything that belonged to the old
    /// campaign, unsaved edits included, and load from the new one.
    /// </summary>
    public virtual Task ReloadCampaignDataAsync()
    {
        return Task.CompletedTask;
    }
}