using System.Windows;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}