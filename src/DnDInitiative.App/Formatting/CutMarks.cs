using System.Windows;
using System.Windows.Media;

namespace DnDInitiative.App.Formatting;

/// <summary>
/// The thin lines in the margins of a printed page that show where to cut. There is one in
/// line with every edge of a card, outside the cards, so none is printed on a card.
/// Covers the whole page, as laid out by <see cref="SheetLayout"/>.
/// </summary>
public sealed class CutMarks : FrameworkElement
{
    private static readonly Pen MarkPen = CreatePen();

    public CutMarks()
    {
        Width = SheetLayout.PageWidth;
        Height = SheetLayout.PageHeight;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        // The outer edges of the cards, half a gap inside the grid of cells.
        double halfGap = SheetLayout.Gap / 2;
        double top = SheetLayout.MarginY + halfGap;
        double bottom = SheetLayout.MarginY + SheetLayout.GridHeight - halfGap;
        double left = SheetLayout.MarginX + halfGap;
        double right = SheetLayout.MarginX + SheetLayout.GridWidth - halfGap;
        double gap = SheetLayout.CutMarkGap;
        double length = SheetLayout.CutMarkLength;

        // Each card has two edges, so with gaps between the cards there are two marks per column and row.
        for (int column = 0; column < SheetLayout.Columns; column++)
        {
            double cardLeft = left + (column * SheetLayout.CellWidth);
            foreach (double x in new[] { cardLeft, cardLeft + CardLayout.Width })
            {
                drawingContext.DrawLine(MarkPen, new Point(x, top - gap - length), new Point(x, top - gap));
                drawingContext.DrawLine(MarkPen, new Point(x, bottom + gap), new Point(x, bottom + gap + length));
            }
        }

        for (int row = 0; row < SheetLayout.Rows; row++)
        {
            double cardTop = top + (row * SheetLayout.CellHeight);
            foreach (double y in new[] { cardTop, cardTop + CardLayout.Height })
            {
                drawingContext.DrawLine(MarkPen, new Point(left - gap - length, y), new Point(left - gap, y));
                drawingContext.DrawLine(MarkPen, new Point(right + gap, y), new Point(right + gap + length, y));
            }
        }
    }

    // Frozen, so the one pen can be shared by every page, the printed ones included.
    private static Pen CreatePen()
    {
        var pen = new Pen(Brushes.Black, 0.5);
        pen.Freeze();
        return pen;
    }
}