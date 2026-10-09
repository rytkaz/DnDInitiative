using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DnDInitiative.Infrastructure.Persistence;

/// <summary>
/// Used only by the "dotnet ef" command-line tools, which need to create the context without running the app.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Created by the EF Core tools.")]
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new AppDbContext(options);
    }
}