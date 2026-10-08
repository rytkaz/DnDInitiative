using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Messages;
using DnDInitiative.App.Services;
using DnDInitiative.App.Tests.Fakes;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;

namespace DnDInitiative.App.Tests.Services;

public class EncounterLinkHandlerTests
{
    private readonly StatBlock _goblin = new() { Id = Guid.NewGuid(), Name = "Goblin" };
    private readonly Party _party = new(Guid.NewGuid(), "Heroes", [new PlayerCharacter("Aria", Level: 1)]);
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly MessageRecorder _recorder = new();

    public EncounterLinkHandlerTests()
    {
        _messenger.Register<StartCombatMessage>(_recorder);
    }

    [Fact]
    public async Task HandleAsync_LinkToStoredEncounter_SendsItsCreaturesAndParty()
    {
        var encounter = new Encounter(Guid.NewGuid(), "Ambush", [new EncounterCreature(_goblin.Id, 2)]);
        EncounterLinkHandler handler = CreateHandler(encounter);

        bool handled = await handler.HandleAsync(new EncounterLink(encounter.Id, _party.Id).ToString());

        Assert.True(handled);
        StartCombatMessage message = Assert.IsType<StartCombatMessage>(_recorder.Received);
        Assert.Equal("Ambush", message.Name);
        Assert.Equal(2, message.Creatures.Count);
        Assert.Same(_party, message.Party);
    }

    [Fact]
    public async Task HandleAsync_LinkWithoutParty_SendsNoParty()
    {
        var encounter = new Encounter(Guid.NewGuid(), "Ambush", []);
        EncounterLinkHandler handler = CreateHandler(encounter);

        await handler.HandleAsync(new EncounterLink(encounter.Id).ToString());

        Assert.Null(Assert.IsType<StartCombatMessage>(_recorder.Received).Party);
    }

    [Theory]
    [InlineData("not a link")]
    [InlineData("dndinitiative://encounter/11111111-2222-3333-4444-555555555555")]
    public async Task HandleAsync_NotALinkOrUnknownEncounter_DoesNothing(string text)
    {
        EncounterLinkHandler handler = CreateHandler();

        bool handled = await handler.HandleAsync(text);

        Assert.False(handled);
        Assert.Null(_recorder.Received);
    }

    private EncounterLinkHandler CreateHandler(params Encounter[] encounters)
    {
        return new EncounterLinkHandler(
            new InMemoryEncounterRepository(encounters),
            new InMemoryStatBlockRepository(_goblin),
            new InMemoryPartyRepository(_party),
            _messenger);
    }
}