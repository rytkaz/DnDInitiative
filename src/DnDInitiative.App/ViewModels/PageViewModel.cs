using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDInitiative.App.ViewModels;

/// <summary>
/// Base class for anything the main window can navigate to.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }
}