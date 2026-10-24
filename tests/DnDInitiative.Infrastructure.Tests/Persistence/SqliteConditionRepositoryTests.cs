using DnDInitiative.Core.Conditions;
using DnDInitiative.Infrastructure.Conditions;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

public sealed class SqliteConditionRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly SqliteConditionRepository _repository;

    public SqliteConditionRepositoryTests()
    {
        _repository = new SqliteConditionRepository(_database);
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task SaveManyAsync_EveryFieldSurvivesTheRoundTrip()
    {
        var original = new Condition { Name = "Grappled", Source = "XPHB p. 362", Description = "- **Speed 0.** Your Speed is 0." };

        await _repository.SaveManyAsync([original]);

        Assert.Equal(original, await _repository.FindAsync("Grappled"));
    }

    [Theory]
    [InlineData("grappled")]
    [InlineData("  GRAPPLED ")]
    public async Task FindAsync_IgnoresCaseAndSurroundingSpaces(string name)
    {
        await _repository.SaveManyAsync([new Condition { Name = "Grappled" }]);

        Assert.Equal("Grappled", (await _repository.FindAsync(name))?.Name);
    }

    [Fact]
    public async Task SaveManyAsync_SameNameInAnotherCase_ReplacesTheStoredCondition()
    {
        await _repository.SaveManyAsync([new Condition { Name = "Prone", Description = "Old." }]);

        await _repository.SaveManyAsync([new Condition { Name = "prone", Description = "New." }, new Condition { Name = "Blinded" }]);

        Assert.Equal("New.", (await _repository.FindAsync("Prone"))?.Description);
        Assert.NotNull(await _repository.FindAsync("Blinded"));
    }

    [Theory]
    [InlineData("Petrified")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_NothingMatches_ReturnsNull(string name)
    {
        await _repository.SaveManyAsync([new Condition { Name = "Prone" }]);

        Assert.Null(await _repository.FindAsync(name));
    }
}
