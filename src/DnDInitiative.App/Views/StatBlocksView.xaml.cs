using System.Windows;
using System.Windows.Controls;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class StatBlocksView : UserControl
{
    public StatBlocksView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StatBlocksViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}