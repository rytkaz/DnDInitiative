using DnDInitiative.Core.StatBlocks;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Exceptions;

namespace DnDInitiative.Infrastructure.Homebrewery;

/// <summary>A clickable link in a PDF: where it points, and the words it sits on.</summary>
internal sealed record PdfLink(string Uri, string Text);

/// <summary>Reads the web links out of a PDF, such as a catalog whose entries link to Homebrewery documents.</summary>
internal static class PdfLinkReader
{
    /// <summary>Returns every link to a web address, page by page, in the order the PDF stores them.</summary>
    /// <exception cref="ConversionException">The file is missing or is not a readable PDF.</exception>
    public static IReadOnlyList<PdfLink> ReadLinks(string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        if (!File.Exists(pdfPath))
        {
            throw new ConversionException($"The PDF '{pdfPath}' does not exist.");
        }

        try
        {
            using PdfDocument document = PdfDocument.Open(pdfPath);

            var links = new List<PdfLink>();
            foreach (Page page in document.GetPages())
            {
                foreach (Hyperlink hyperlink in page.GetHyperlinks())
                {
                    if (hyperlink.Uri.Length > 0)
                    {
                        links.Add(new PdfLink(hyperlink.Uri.Trim(), hyperlink.Text.Trim()));
                    }
                }
            }

            return links;
        }
        catch (PdfDocumentFormatException exception)
        {
            throw new ConversionException($"'{Path.GetFileName(pdfPath)}' could not be read as a PDF.", exception);
        }
        catch (PdfDocumentEncryptedException exception)
        {
            throw new ConversionException($"'{Path.GetFileName(pdfPath)}' is password protected.", exception);
        }
        catch (IOException exception)
        {
            throw new ConversionException($"'{Path.GetFileName(pdfPath)}' could not be opened: {exception.Message}", exception);
        }
    }
}
