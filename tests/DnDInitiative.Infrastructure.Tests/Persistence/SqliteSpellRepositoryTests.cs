using DnDInitiative.Core.Spells;
using DnDInitiative.Infrastructure.Spells;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

public sealed class SqliteSpellRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly SqliteSpellRepository _repository;

    public SqliteSpellRepositoryTests()
    {
        _repository = new SqliteSpellRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task SaveManyAsync_NewSpell_EveryFieldSurvivesTheRoundTrip()
    {
        var original = new Spell
        {
            Key = "fireball-xphb",
            Name = "Fireball",
            Level = "3rd-level",
            School = "Evocation",
            IsRitual = true,
            CastingTime = "1 action",
            Range = "150 feet",
            Components = "V, S, M (a ball of bat guano and sulfur)",
            Duration = "Instantaneous",
            Source = "XPHB p. 274",
            Description = "A bright streak flashes.\n\n**At Higher Levels.** More damage.",
        };

        SpellSaveResult result = await _repository.SaveManyAsync([original]);

        Assert.Equal(new SpellSaveResult(Added: 1, Updated: 0), result);
        Assert.Equal(original, await _repository.FindAsync("fireball-xphb"));
    }

    [Fact]
    public async Task SaveManyAsync_KeyAlreadyStored_ReplacesTheSpellInsteadOfDuplicatingIt()
    {
        await _repository.SaveManyAsync([Fireball(range: "150 feet"), Shield()]);

        SpellSaveResult result = await _repository.SaveManyAsync([Fireball(range: "200 feet")]);

        Assert.Equal(new SpellSaveResult(Added: 0, Updated: 1), result);
        Assert.Equal("200 feet", (await _repository.FindAsync("fireball"))?.Range);
        Assert.NotNull(await _repository.FindAsync("shield"));
    }

    [Fact]
    public async Task SaveManyAsync_SameKeyTwiceInOneCall_KeepsTheLastAndCountsItOnce()
    {
        SpellSaveResult result = await _repository.SaveManyAsync([Fireball(range: "150 feet"), Fireball(range: "200 feet")]);

        Assert.Equal(new SpellSaveResult(Added: 1, Updated: 0), result);
        Assert.Equal("200 feet", (await _repository.FindAsync("fireball"))?.Range);
    }

    [Fact]
    public async Task SaveManyAsync_KeyInUpperCase_IsStoredInLowerCase()
    {
        await _repository.SaveManyAsync([Fireball() with { Key = "Fireball-XPHB" }]);

        Assert.Equal("fireball-xphb", (await _repository.FindAsync("fireball-xphb"))?.Key);
    }

    [Theory]
    [InlineData("fireball")]
    [InlineData("  FIREBALL  ")]
    public async Task FindAsync_ByKey_IgnoresCaseAndSurroundingSpaces(string reference)
    {
        await _repository.SaveManyAsync([Fireball()]);

        Assert.Equal("Fireball", (await _repository.FindAsync(reference))?.Name);
    }

    [Theory]
    [InlineData("Detect Magic")]
    [InlineData("detect magic")]
    public async Task FindAsync_NoSuchKey_FallsBackToTheName(string reference)
    {
        await _repository.SaveManyAsync([new Spell { Key = "detect-magic-xphb", Name = "Detect Magic" }]);

        Assert.Equal("detect-magic-xphb", (await _repository.FindAsync(reference))?.Key);
    }

    [Fact]
    public async Task FindAsync_KeyAndAnotherSpellsNameBothMatch_PrefersTheKey()
    {
        await _repository.SaveManyAsync(
        [
            new Spell { Key = "shield", Name = "Shield" },
            new Spell { Key = "shield-homebrew", Name = "shield" },
        ]);

        Assert.Equal("shield", (await _repository.FindAsync("shield"))?.Key);
    }

    [Fact]
    public async Task FindAsync_TwoSpellsShareAName_ReturnsTheSameOneEveryTime()
    {
        await _repository.SaveManyAsync(
        [
            new Spell { Key = "fireball-xphb", Name = "Fireball" },
            new Spell { Key = "fireball-phb", Name = "Fireball" },
        ]);

        Assert.Equal("fireball-phb", (await _repository.FindAsync("Fireball"))?.Key);
    }

    [Theory]
    [InlineData("wish")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_NothingMatches_ReturnsNull(string reference)
    {
        await _repository.SaveManyAsync([Fireball()]);

        Assert.Null(await _repository.FindAsync(reference));
    }

    private static Spell Fireball(string range = "150 feet")
    {
        return new Spell { Key = "fireball", Name = "Fireball", Level = "3rd-level", Range = range };
    }

    private static Spell Shield()
    {
        return new Spell { Key = "shield", Name = "Shield", Level = "1st-level" };
    }
}
