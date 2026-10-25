using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using DnDInitiative.App.Formatting;
using DnDInitiative.App.ViewModels;
using DnDInitiative.App.Views;

namespace DnDInitiative.App.Services;

/// <summary>
/// Prints through the Windows print dialog. Each page is the same sheet view the Spellbook page
/// shows, drawn in real units, so the cards come out at 63 × 88 mm when printed at 100%.
/// Choosing "Microsoft Print to PDF" in the dialog makes a PDF.
/// </summary>
/// <remarks>
/// The Windows 11 print dialog can only preview for apps built on its newer printing API, and
/// says "This app doesn't support print preview" for a WPF one. So the pages are previewed in a
/// window of our own first, and the dialog is opened from its Print button.
/// </remarks>
public sealed class SpellbookPrinter : ISpellbookPrinter
{
    public bool Print(string title, IReadOnlyList<SpellbookPageViewModel> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        FixedDocument document = CreateDocument(pages);
        var viewer = new DocumentViewer { Document = document };
        var window = new Window
        {
            Title = $"Print preview: {title}",
            Content = viewer,
            Owner = Application.Current?.MainWindow,
            Width = 900,
            Height = 1000,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        // The viewer's own Print button would open a dialog without the A4 suggestion. Catching
        // the command on its way down to the viewer replaces that with the one below.
        bool printed = false;
        var printBinding = new CommandBinding(ApplicationCommands.Print);
        printBinding.PreviewExecuted += (_, e) =>
        {
            e.Handled = true;
            if (PrintWithDialog(document, title))
            {
                printed = true;
                window.Close();
            }
        };
        window.CommandBindings.Add(printBinding);

        // A whole page in view to begin with, as it will be printed.
        window.Loaded += (_, _) => viewer.FitToHeight();

        window.ShowDialog();
        return printed;
    }

    private static bool PrintWithDialog(FixedDocument document, string title)
    {
        var dialog = new PrintDialog();

        // Only a suggestion: the dialog still lets the user pick another paper size.
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
        dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        dialog.PrintDocument(document.DocumentPaginator, title);
        return true;
    }

    private static FixedDocument CreateDocument(IReadOnlyList<SpellbookPageViewModel> pages)
    {
        var size = new Size(SheetLayout.PageWidth, SheetLayout.PageHeight);
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = size;

        foreach (SpellbookPageViewModel page in pages)
        {
            var sheet = new SpellbookSheetView { DataContext = page, IsEditable = false };
            var fixedPage = new FixedPage { Width = size.Width, Height = size.Height };
            fixedPage.Children.Add(sheet);

            // A page that is never shown on screen has to be laid out by hand before it is printed.
            fixedPage.Measure(size);
            fixedPage.Arrange(new Rect(size));
            fixedPage.UpdateLayout();

            document.Pages.Add(new PageContent { Child = fixedPage });
        }

        return document;
    }
}