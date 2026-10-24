using CommunityToolkit.Mvvm.Input;

namespace DnDInitiative.App.ViewModels;

/// <summary>An import that runs in a window of its own, opened from the Import menu.</summary>
public interface IImportViewModel
{
    /// <summary>The window's title.</summary>
    string Title { get; }

    /// <summary>
    /// Runs the import. The window asks before it closes while this runs, and cancels it
    /// through <see cref="IAsyncRelayCommand.Cancel"/> if the user agrees.
    /// </summary>
    IAsyncRelayCommand RunCommand { get; }
}
