using System.Windows;

using DnDInitiative.App.Services;

namespace DnDInitiative.App.Views;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    /// <summary>The button the user chose. Closing the dialog any other way counts as Cancel.</summary>
    public UnsavedChangesChoice Choice { get; private set; } = UnsavedChangesChoice.Cancel;

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        Choose(UnsavedChangesChoice.Save);
    }

    private void OnDiscardClick(object sender, RoutedEventArgs e)
    {
        Choose(UnsavedChangesChoice.Discard);
    }

    private void Choose(UnsavedChangesChoice choice)
    {
        Choice = choice;
        DialogResult = true;
    }
}
