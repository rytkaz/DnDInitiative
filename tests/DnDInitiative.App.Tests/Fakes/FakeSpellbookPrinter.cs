using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;

namespace DnDInitiative.App.Tests.Fakes;

internal sealed class FakeSpellbookPrinter : ISpellbookPrinter
{
    /// <summary>What Print returns: whether the user went ahead in the print dialog.</summary>
    public bool Answer { get; set; } = true;

    public List<IReadOnlyList<SpellbookPageViewModel>> Printed { get; } = [];

    public bool Print(string title, IReadOnlyList<SpellbookPageViewModel> pages)
    {
        if (Answer)
        {
            Printed.Add(pages);
        }

        return Answer;
    }
}