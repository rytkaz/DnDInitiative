using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.Persistence;
using DnDInitiative.Infrastructure.StatBlocks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DnDInitiative.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers storage and import services, keeping their data under the given folder.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataDirectory, "dndinitiative.db"),
        }.ToString();

        services.AddSingleton(new StorageOptions(dataDirectory));
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddSingleton<IPartyRepository, SqlitePartyRepository>();
        // One object, reachable three ways: as itself (for the campaign repository to wrap),
        // as the app's stat block store, and as the importer's view of the library.
        services.AddSingleton<SqliteStatBlockRepository>();
        services.AddSingleton<IStatBlockRepository>(provider => provider.GetRequiredService<SqliteStatBlockRepository>());
        services.AddSingleton<IStatBlockLibrary>(provider => provider.GetRequiredService<SqliteStatBlockRepository>());

        services.AddSingleton<IEncounterRepository, SqliteEncounterRepository>();
        services.AddSingleton<IStatBlockImporter, MarkdownStatBlockImporter>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IStatBlockConverter, TtrpgConvertConverter>();
        services.AddSingleton<IConversionSettingsStore, JsonConversionSettingsStore>();

        // Campaign folder storage
        services.AddSingleton<CampaignLocation>();
        services.AddSingleton<JsonPartyRepository>();
        services.AddSingleton<JsonEncounterRepository>();
        services.AddSingleton<CampaignStatBlockRepository>();

        services.AddSingleton<LegacyJsonImporter>();
        services.AddSingleton<IStorageInitializer, StorageInitializer>();

        return services;
    }
}