using DnDInitiative.Core.Parties;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Parties;

using Microsoft.Extensions.Logging.Abstractions;

namespace DnDInitiative.Infrastructure.Tests.Campaigns;

public sealed class JsonPartyRepositoryTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dndinitiative-tests-").FullName;
    private readonly JsonPartyRepository _repository;

    public JsonPartyRepositoryTests()
    {
        var location = new CampaignLocation { Folder = _folder };
        _repository = new JsonPartyRepository(location, NullLogger<JsonPartyRepository>.Instance);
    }

    private string PartiesFolder => Path.Combine(_folder, "parties");

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task GetAllAsync_EmptyCampaign_ReturnsNothing()
    {
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task SaveAsync_ThenGetAllAsync_ReturnsTheSameParty()
    {
        var party = new Party(
            Guid.NewGuid(),
            "Heroes",
            [new PlayerCharacter("Aria", Level: 3, MaxHp: 24), new PlayerCharacter("Bren", Level: 2)]);

        await _repository.SaveAsync(party);

        Party loaded = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal(party.Id, loaded.Id);
        Assert.Equal("Heroes", loaded.Name);
        Assert.Equal(party.Members.ToArray(), loaded.Members.ToArray());
    }

    [Fact]
    public async Task SaveAsync_WritesAReadableFileNamedAfterTheParty()
    {
        await _repository.SaveAsync(new Party(Guid.NewGuid(), "Aria's Heroes", [new PlayerCharacter("Aria", Level: 3)]));

        string text = await File.ReadAllTextAsync(Path.Combine(PartiesFolder, "aria-s-heroes.json"));
        Assert.Contains("  \"name\": \"Aria's Heroes\",\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("maxHp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
        Assert.EndsWith("}\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_PartyRenamed_RenamesItsFile()
    {
        var party = new Party(Guid.NewGuid(), "Heroes", []);
        await _repository.SaveAsync(party);

        await _repository.SaveAsync(party with { Name = "Villains" });

        Assert.Equal(["villains.json"], FileNamesIn(PartiesFolder));
        Assert.Equal("Villains", Assert.Single(await _repository.GetAllAsync()).Name);
    }

    [Fact]
    public async Task SaveAsync_TwoPartiesWithTheSameName_GetSeparateFiles()
    {
        await _repository.SaveAsync(new Party(Guid.NewGuid(), "Heroes", []));
        await _repository.SaveAsync(new Party(Guid.NewGuid(), "Heroes", []));

        Assert.Equal(["heroes-2.json", "heroes.json"], FileNamesIn(PartiesFolder));
        Assert.Equal(2, (await _repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task SaveAsync_SamePartyAgain_KeepsItsFileName()
    {
        var party = new Party(Guid.NewGuid(), "Heroes", []);
        await _repository.SaveAsync(party);

        await _repository.SaveAsync(party with { Members = [new PlayerCharacter("Aria", Level: 1)] });

        Assert.Equal(["heroes.json"], FileNamesIn(PartiesFolder));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFile()
    {
        var party = new Party(Guid.NewGuid(), "Heroes", []);
        await _repository.SaveAsync(party);

        await _repository.DeleteAsync(party.Id);

        Assert.Empty(FileNamesIn(PartiesFolder));
    }

    [Fact]
    public async Task GetAllAsync_FileAddedOutsideTheApp_IsPickedUp()
    {
        // What a "git pull" does: a file appears that this app never wrote.
        Directory.CreateDirectory(PartiesFolder);
        await File.WriteAllTextAsync(
            Path.Combine(PartiesFolder, "from-another-machine.json"),
            """{ "id": "11111111-2222-3333-4444-555555555555", "name": "Pulled", "members": [] }""");

        Assert.Equal("Pulled", Assert.Single(await _repository.GetAllAsync()).Name);
    }

    [Theory]
    [InlineData("<<<<<<< HEAD")]
    [InlineData("""{ "id": "11111111-2222-3333-4444-555555555555", "name": "No members" }""")]
    public async Task GetAllAsync_BrokenFile_IsSkippedAndNeverOverwritten(string brokenContent)
    {
        Directory.CreateDirectory(PartiesFolder);
        string brokenPath = Path.Combine(PartiesFolder, "heroes.json");
        await File.WriteAllTextAsync(brokenPath, brokenContent);

        // A party whose natural file name is the broken file's name.
        await _repository.SaveAsync(new Party(Guid.NewGuid(), "Heroes", []));

        Assert.Equal("Heroes", Assert.Single(await _repository.GetAllAsync()).Name);
        Assert.Equal(brokenContent, await File.ReadAllTextAsync(brokenPath));
        Assert.Equal(["heroes-2.json", "heroes.json"], FileNamesIn(PartiesFolder));
    }

    private static string[] FileNamesIn(string folder)
    {
        return Directory.EnumerateFiles(folder)
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}