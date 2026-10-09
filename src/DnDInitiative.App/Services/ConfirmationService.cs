using System.Windows;

namespace DnDInitiative.App.Services;

public sealed class ConfirmationService : IConfirmationService
{
    public bool Confirm(string title, string message)
    {
        // "No" is the default button, so pressing Enter by reflex never destroys anything.
        const MessageBoxButton Buttons = MessageBoxButton.YesNo;
        const MessageBoxImage Icon = MessageBoxImage.Warning;
        const MessageBoxResult Default = MessageBoxResult.No;

        // With an owner the dialog is centred on the window and stays in front of it.
        // Without one it can end up behind the app, which matters when a link from
        // Obsidian brings the app forward and asks straight away.
        Window? owner = Application.Current?.MainWindow;
        MessageBoxResult result = owner is { IsLoaded: true }
            ? MessageBox.Show(owner, message, title, Buttons, Icon, Default)
            : MessageBox.Show(message, title, Buttons, Icon, Default);

        return result == MessageBoxResult.Yes;
    }
}