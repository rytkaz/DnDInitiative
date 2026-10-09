using DnDInitiative.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DnDInitiative.Infrastructure.Tests.Persistence;

/// <summary>
/// A real SQLite database in a temporary folder, created by running the migrations.
/// </summary>
internal sealed class TestDatabase : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDatabase()
    {
        Directory.CreateDirectory(_directory);

        // Pooling keeps the file open after a context is disposed, which would stop the folder being deleted.
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_directory, "test.db"),
            Pooling = false,
        }.ToString();

        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options;

        using AppDbContext db = CreateDbContext();
        db.Database.Migrate();
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}