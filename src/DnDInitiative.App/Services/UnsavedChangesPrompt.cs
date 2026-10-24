using System.Windows;

using DnDInitiative.App.Views;

namespace DnDInitiative.App.Services;

public sealed class UnsavedChangesPrompt : IUnsavedChangesPrompt
{
    public UnsavedChangesChoice Ask(string title, string message)
    {
        var dialog = new UnsavedChangesDialog(title, message);

        // Centred on the window and kept in front of it, as ConfirmationService does with its message box.
        Window? owner = Application.Current?.MainWindow;
        if (owner is { IsLoaded: true })
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
        return dialog.Choice;
    }
}
