using System.Windows;
using System.Windows.Controls;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Views;

public partial class EncounterBuilderView : UserControl
{
    public EncounterBuilderView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EncounterBuilderViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}