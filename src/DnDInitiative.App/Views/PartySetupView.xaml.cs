using System.Windows;
using System.Windows.Controls;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class PartySetupView : UserControl
{
    public PartySetupView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PartySetupViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}