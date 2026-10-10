using DnDInitiative.App.Formatting;

namespace DnDInitiative.App.Tests.Formatting;

public class WebLinkTests
{
    [Theory]
    [InlineData("https://homebrewery.naturalcrit.com/share/uDYpiTxHncTE")]
    [InlineData("http://example.com/rules?id=3#fever")]
    [InlineData("  HTTPS://Example.com/  ")]
    public void ToUri_WebAddress_IsAccepted(string target)
    {
        Uri? uri = WebLink.ToUri(target);

        Assert.NotNull(uri);
        Assert.True(WebLink.IsWebAddress(uri));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("spell:fireball")]
    [InlineData("rules/conditions.md#Prone")]
    [InlineData("mailto:hello@example.com")]
    [InlineData("file:///C:/Windows/notepad.exe")]
    [InlineData(@"C:\Windows\notepad.exe")]
    [InlineData("javascript:alert(1)")]
    public void ToUri_AnythingThatIsNotAWebAddress_ReturnsNull(string? target)
    {
        Assert.Null(WebLink.ToUri(target));
    }

    [Fact]
    public void IsWebAddress_NoUri_IsFalse()
    {
        Assert.False(WebLink.IsWebAddress(null));
        Assert.False(WebLink.IsWebAddress(new Uri("rules/conditions.md", UriKind.Relative)));
    }
}
