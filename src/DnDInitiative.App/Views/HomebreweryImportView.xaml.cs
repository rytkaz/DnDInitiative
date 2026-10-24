using System.Windows;
using System.Windows.Controls;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class HomebreweryImportView : UserControl
{
    public HomebreweryImportView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HomebreweryImportViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    // Keeps the newest output in view, like a console window.
    private void OnLogTextChanged(object sender, TextChangedEventArgs e)
    {
        ((TextBox)sender).ScrollToEnd();
    }
}
