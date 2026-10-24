using DnDInitiative.Infrastructure.Homebrewery;

namespace DnDInitiative.Infrastructure.Tests.Homebrewery;

public class HomebreweryLinksTests
{
    [Theory]
    [InlineData("https://homebrewery.naturalcrit.com/share/aBc123_-xYz9", "aBc123_-xYz9")]
    [InlineData("http://homebrewery.naturalcrit.com/share/aBc123", "aBc123")]
    [InlineData("homebrewery.naturalcrit.com/share/aBc123", "aBc123")]
    [InlineData("https://HOMEBREWERY.naturalcrit.com/source/aBc123", "aBc123")]
    [InlineData("https://homebrewery.naturalcrit.com/download/aBc123", "aBc123")]
    [InlineData("https://homebrewery.naturalcrit.com/print/aBc123?dialog=true", "aBc123")]
    [InlineData("https://homebrewery.naturalcrit.com/share/aBc123#p2", "aBc123")]
    [InlineData("  https://homebrewery.naturalcrit.com/share/1LongGoogleDriveId-_x  ", "1LongGoogleDriveId-_x")]
    public void ShareId_LinkToADocument_ReturnsItsId(string link, string expected)
    {
        Assert.Equal(expected, HomebreweryLinks.ShareId(link));
    }

    [Theory]
    [InlineData("https://homebrewery.naturalcrit.com/edit/aBc123")]
    [InlineData("https://homebrewery.naturalcrit.com/")]
    [InlineData("https://www.reddit.com/r/bettermonsters/")]
    [InlineData("https://example.com/share/aBc123")]
    [InlineData("")]
    public void ShareId_AnyOtherLink_ReturnsNull(string link)
    {
        Assert.Null(HomebreweryLinks.ShareId(link));
    }

    [Fact]
    public void ShareId_NoLink_ReturnsNull()
    {
        Assert.Null(HomebreweryLinks.ShareId(null));
    }

    [Fact]
    public void SourceUri_PointsAtThePlainTextDownload()
    {
        Assert.Equal(
            new Uri("https://homebrewery.naturalcrit.com/download/aBc123"),
            HomebreweryLinks.SourceUri("aBc123"));
    }
}
