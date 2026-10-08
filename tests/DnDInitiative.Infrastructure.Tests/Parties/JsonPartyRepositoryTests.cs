using DnDInitiative.Core.Parties;
using DnDInitiative.Infrastructure.Parties;

namespace DnDInitiative.Infrastructure.Tests.Parties;

public sealed class JsonPartyRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private string FilePath => Path.Combine(_directory, "parties.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetAllAsync_FileDoesNotExist_ReturnsEmptyList()
    {
        var repository = new JsonPartyRepository(FilePath);

        IReadOnlyList<Party> parties = await repository.GetAllAsync();

        Assert.Empty(parties);
    }

    [Fact]
    public async Task SaveAsync_NewParty_CanBeReadBackFromDisk()
    {
        Party party = CreateParty("Heroes");
        await new JsonPartyRepository(FilePath).SaveAsync(party);

        IReadOnlyList<Party> parties = await new JsonPartyRepository(FilePath).GetAllAsync();

        Party loaded = Assert.Single(parties);
        Assert.Equal(party.Id, loaded.Id);
        Assert.Equal("Heroes", loaded.Name);
        Assert.Equal<PlayerCharacter>(party.Members, loaded.Members);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_ReplacesStoredParty()
    {
        var repository = new JsonPartyRepository(FilePath);
        Party original = CreateParty("Heroes");
        await repository.SaveAsync(original);

        await repository.SaveAsync(original with { Name = "Renamed" });

        Party loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal("Renamed", loaded.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyThePartyWithThatId()
    {
        var repository = new JsonPartyRepository(FilePath);
        Party keep = CreateParty("Keep");
        Party remove = CreateParty("Remove");
        await repository.SaveAsync(keep);
        await repository.SaveAsync(remove);

        await repository.DeleteAsync(remove.Id);

        Party loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(keep.Id, loaded.Id);
    }

    private static Party CreateParty(string name)
    {
        return new Party(
            Guid.NewGuid(),
            name,
            [new PlayerCharacter("Aria", Level: 3, MaxHp: 24, ArmorClass: 16), new PlayerCharacter("Bren", Level: 3)]);
    }
}