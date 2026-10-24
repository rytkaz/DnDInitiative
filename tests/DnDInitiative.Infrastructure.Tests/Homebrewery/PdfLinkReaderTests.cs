using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Homebrewery;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public sealed class PdfLinkReaderTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void ReadLinks_CatalogPdf_ReturnsEveryLinkInOrder()
    {
        string path = Path.Combine(_directory, "catalog.pdf");
        TestPdf.Write(
            path,
            ("Marsh Lurkers", "https://homebrewery.naturalcrit.com/share/lurkers0001"),
            ("Cinder Drakes", "https://homebrewery.naturalcrit.com/share/drakes00001"),
            ("Our forum", "https://example.com/forum"));

        IReadOnlyList<PdfLink> links = PdfLinkReader.ReadLinks(path);

        string[] expected =
        [
            "https://homebrewery.naturalcrit.com/share/lurkers0001",
            "https://homebrewery.naturalcrit.com/share/drakes00001",
            "https://example.com/forum",
        ];
        Assert.Equal(expected, links.Select(link => link.Uri).ToArray());
        Assert.Contains("Lurkers", links[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadLinks_FileIsNotAPdf_ThrowsAMessageForTheUser()
    {
        string path = Path.Combine(_directory, "notes.pdf");
        File.WriteAllText(path, "These are notes, not a PDF.");

        ConversionException exception = Assert.Throws<ConversionException>(() => PdfLinkReader.ReadLinks(path));

        Assert.Equal("'notes.pdf' could not be read as a PDF.", exception.Message);
    }

    [Fact]
    public void ReadLinks_FileIsMissing_ThrowsAMessageForTheUser()
    {
        string path = Path.Combine(_directory, "missing.pdf");

        ConversionException exception = Assert.Throws<ConversionException>(() => PdfLinkReader.ReadLinks(path));

        Assert.Contains("does not exist", exception.Message, StringComparison.Ordinal);
    }
}
