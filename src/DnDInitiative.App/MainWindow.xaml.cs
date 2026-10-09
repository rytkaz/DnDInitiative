using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        // Most text boxes only write to their view model when they lose focus, and clicking
        // the window's close button does not take focus away. Push the text across first,
        // or an edit still being typed would be missed.
        if (Keyboard.FocusedElement is TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        if (!e.Cancel && !_viewModel.ConfirmClose())
        {
            e.Cancel = true;
        }
    }
}