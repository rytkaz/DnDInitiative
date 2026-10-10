using DnDInitiative.Core.Parties;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.Tests.Persistence;

namespace DnDInitiative.Infrastructure.Tests.Parties;

public sealed class SqlitePartyRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var repository = new SqlitePartyRepository(_database);

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_NewParty_CanBeReadBackWithMembersInOrder()
    {
        var repository = new SqlitePartyRepository(_database);
        var party = new Party(
            Guid.NewGuid(),
            "Heroes",
            [
                new PlayerCharacter("Zed", Level: 3, MaxHp: 24, ArmorClass: 16),
                new PlayerCharacter("Aria", Level: 4),
                new PlayerCharacter("Mira", Level: 3, MaxHp: 18),
            ]);

        await repository.SaveAsync(party);

        Party loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(party.Id, loaded.Id);
        Assert.Equal("Heroes", loaded.Name);
        Assert.Equal<PlayerCharacter>(party.Members, loaded.Members);
    }

    [Fact]
    public async Task SaveAsync_ExistingId_ReplacesNameAndMembers()
    {
        var repository = new SqlitePartyRepository(_database);
        var original = new Party(
            Guid.NewGuid(),
            "Heroes",
            [new PlayerCharacter("Aria", Level: 3), new PlayerCharacter("Bren", Level: 3)]);
        await repository.SaveAsync(original);

        await repository.SaveAsync(original with { Name = "Veterans", Members = [new PlayerCharacter("Cato", Level: 5)] });

        Party loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal("Veterans", loaded.Name);
        Assert.Equal(new PlayerCharacter("Cato", Level: 5), Assert.Single(loaded.Members));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyThePartyWithThatId()
    {
        var repository = new SqlitePartyRepository(_database);
        var keep = new Party(Guid.NewGuid(), "Keep", [new PlayerCharacter("Aria", Level: 3)]);
        var remove = new Party(Guid.NewGuid(), "Remove", [new PlayerCharacter("Bren", Level: 3)]);
        await repository.SaveAsync(keep);
        await repository.SaveAsync(remove);

        await repository.DeleteAsync(remove.Id);

        Party loaded = Assert.Single(await repository.GetAllAsync());
        Assert.Equal(keep.Id, loaded.Id);
    }
}