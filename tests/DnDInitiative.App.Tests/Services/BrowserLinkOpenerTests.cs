using DnDInitiative.App.Services;

namespace DnDInitiative.App.Tests.Services;

public class BrowserLinkOpenerTests
{
    // Only the refusals are tested: the other path would open a browser on whoever runs the tests.
    [Theory]
    [InlineData("file:///C:/Windows/notepad.exe")]
    [InlineData("mailto:hello@example.com")]
    [InlineData("spell:fireball")]
    public void TryOpen_NotAWebAddress_RefusesWithoutStartingAnything(string address)
    {
        Assert.False(new BrowserLinkOpener().TryOpen(new Uri(address)));
    }
}
