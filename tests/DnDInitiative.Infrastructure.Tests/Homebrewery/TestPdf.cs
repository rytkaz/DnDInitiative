using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

/// <summary>Writes small PDFs with clickable links, like a catalog printed from the Homebrewery.</summary>
internal static class TestPdf
{
    /// <summary>Writes one page with each entry's text, and a link over it, one below the other.</summary>
    public static void Write(string path, params (string Text, string Url)[] entries)
    {
        using var builder = new PdfDocumentBuilder();
        PdfPageBuilder page = builder.AddPage(PageSize.Letter);
        PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

        double baseline = 700;
        foreach ((string text, string url) in entries)
        {
            page.AddText(text, 12, new PdfPoint(72, baseline), font);
            page.AddLink(url, new PdfRectangle(70, baseline - 4, 300, baseline + 14));
            baseline -= 24;
        }

        File.WriteAllBytes(path, builder.Build());
    }
}
