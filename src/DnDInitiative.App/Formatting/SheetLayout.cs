namespace DnDInitiative.App.Formatting;

/// <summary>
/// How cards are arranged on a printed A4 page: 3 × 3 cards with a 2 mm gap between them,
/// centred on the page. That leaves 8.5 mm at the sides and 14.3 mm above and below, so the
/// cut marks stay clear of the 5 mm most printers cannot print on. All sizes are WPF units
/// (1/96 inch).
/// </summary>
public static class SheetLayout
{
    private const double UnitsPerMillimetre = 96 / 25.4;

    public const double PageWidth = 210 * UnitsPerMillimetre;
    public const double PageHeight = 297 * UnitsPerMillimetre;

    public const int Columns = 3;
    public const int Rows = 3;
    public const int CardsPerPage = Columns * Rows;

    public const double Gap = 2 * UnitsPerMillimetre;

    /// <summary>
    /// The room for one card: the card and half a gap on every side. The grid of cells is
    /// therefore half a gap wider than the cards all round.
    /// </summary>
    public const double CellWidth = CardLayout.Width + Gap;
    public const double CellHeight = CardLayout.Height + Gap;

    public const double GridWidth = Columns * CellWidth;
    public const double GridHeight = Rows * CellHeight;

    /// <summary>From the edge of the page to the edge of the grid of cells, not of the cards.</summary>
    public const double MarginX = (PageWidth - GridWidth) / 2;
    public const double MarginY = (PageHeight - GridHeight) / 2;

    /// <summary>The space between a cut mark and the cards, so a slightly crooked cut does not show it.</summary>
    public const double CutMarkGap = 1 * UnitsPerMillimetre;

    public const double CutMarkLength = 3 * UnitsPerMillimetre;
}