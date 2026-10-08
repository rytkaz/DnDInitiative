using DnDInitiative.Core.Encounters;

namespace DnDInitiative.Core.Tests.Encounters;

public class EncounterLinkTests
{
    private static readonly Guid s_encounterId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid s_partyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void ToString_WithoutParty_IsSchemeHostAndId()
    {
        var link = new EncounterLink(s_encounterId);

        Assert.Equal("dndinitiative://encounter/11111111-2222-3333-4444-555555555555", link.ToString());
    }

    [Fact]
    public void TryParse_ReadsBackWhatToStringWrote()
    {
        var original = new EncounterLink(s_encounterId, s_partyId);

        Assert.True(EncounterLink.TryParse(original.ToString(), out EncounterLink? parsed));
        Assert.Equal(original, parsed);
    }

    [Theory]
    [InlineData("dndinitiative://encounter/11111111-2222-3333-4444-555555555555")]
    [InlineData("dndinitiative://encounter/11111111-2222-3333-4444-555555555555/")]
    [InlineData("DNDINITIATIVE://ENCOUNTER/11111111-2222-3333-4444-555555555555")]
    [InlineData("dndinitiative://encounter/11111111-2222-3333-4444-555555555555?party=not-a-guid")]
    public void TryParse_AcceptedVariations_ReadTheEncounterId(string text)
    {
        Assert.True(EncounterLink.TryParse(text, out EncounterLink? link));
        Assert.Equal(new EncounterLink(s_encounterId), link);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a link")]
    [InlineData("https://encounter/11111111-2222-3333-4444-555555555555")]
    [InlineData("dndinitiative://party/11111111-2222-3333-4444-555555555555")]
    [InlineData("dndinitiative://encounter/not-a-guid")]
    [InlineData("dndinitiative://encounter/")]
    public void TryParse_AnythingElse_IsRejected(string? text)
    {
        Assert.False(EncounterLink.TryParse(text, out EncounterLink? link));
        Assert.Null(link);
    }
}