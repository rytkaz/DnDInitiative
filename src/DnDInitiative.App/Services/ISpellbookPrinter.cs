using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Services;

/// <summary>Prints a spellbook's pages, one A4 sheet each.</summary>
public interface ISpellbookPrinter
{
    /// <summary>Shows a preview of the pages, and from there asks where to print and prints.</summary>
    /// <param name="title">The name of the print job, which "Microsoft Print to PDF" also suggests as the file name.</param>
    /// <returns>False if the user closed the preview or cancelled the print dialog.</returns>
    bool Print(string title, IReadOnlyList<SpellbookPageViewModel> pages);
}