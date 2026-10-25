using DnDInitiative.Core.Abilities;
using DnDInitiative.Core.Campaigns;
using DnDInitiative.Core.Conditions;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Homebrewery;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.Spellbooks;
using DnDInitiative.Core.Spells;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Abilities;
using DnDInitiative.Infrastructure.Campaigns;
using DnDInitiative.Infrastructure.Conditions;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.GmBinder;
using DnDInitiative.Infrastructure.Homebrewery;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.Persistence;
using DnDInitiative.Infrastructure.Spellbooks;
using DnDInitiative.Infrastructure.Spells;
using DnDInitiative.Infrastructure.StatBlocks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DnDInitiative.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers storage and import services.
    /// </summary>
    /// <param name="dataDirectory">Where this computer's own data lives: the database and settings.</param>
    /// <param name="defaultCampaignDirectory">The campaign folder to create when none has been chosen yet.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string dataDirectory,
        string defaultCampaignDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCampaignDirectory);

        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataDirectory, "dndinitiative.db"),
        }.ToString();

        services.AddSingleton(new StorageOptions(dataDirectory));
        services.AddSingleton(new CampaignOptions(defaultCampaignDirectory));
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

        // The database. It holds the imported stat block library, and whatever is still
        // waiting to be moved into a campaign folder.
        services.AddSingleton<SqlitePartyRepository>();
        services.AddSingleton<SqliteEncounterRepository>();
        services.AddSingleton<SqliteStatBlockRepository>();
        services.AddSingleton<IStatBlockLibrary>(provider => provider.GetRequiredService<SqliteStatBlockRepository>());
        services.AddSingleton<SqliteSpellRepository>();
        services.AddSingleton<IConditionRepository, SqliteConditionRepository>();
        services.AddSingleton<SqliteAbilityRepository>();

        // The campaign folder. This is what the rest of the app reads and writes.
        services.AddSingleton<CampaignLocation>();
        services.AddSingleton<CampaignFileWatcher>();
        services.AddSingleton<ICampaignService, CampaignService>();
        services.AddSingleton<JsonPartyRepository>();
        services.AddSingleton<JsonEncounterRepository>();
        services.AddSingleton<CampaignStatBlockRepository>();
        services.AddSingleton<ISpellbookRepository, JsonSpellbookRepository>();
        services.AddSingleton<CampaignSpellRepository>();
        services.AddSingleton<CampaignAbilityRepository>();
        services.AddSingleton<ISpellRepository>(provider => provider.GetRequiredService<CampaignSpellRepository>());
        services.AddSingleton<ICustomSpellRepository>(provider => provider.GetRequiredService<CampaignSpellRepository>());
        services.AddSingleton<IAbilityRepository>(provider => provider.GetRequiredService<CampaignAbilityRepository>());
        services.AddSingleton<ICustomAbilityRepository>(provider => provider.GetRequiredService<CampaignAbilityRepository>());
        services.AddSingleton<IPartyRepository>(provider => provider.GetRequiredService<JsonPartyRepository>());
        services.AddSingleton<IEncounterRepository>(provider => provider.GetRequiredService<JsonEncounterRepository>());
        services.AddSingleton<IStatBlockRepository>(provider => provider.GetRequiredService<CampaignStatBlockRepository>());

        // Importing
        services.AddSingleton<IStatBlockImporter, MarkdownStatBlockImporter>();
        services.AddSingleton<ISpellImporter, MarkdownSpellImporter>();
        services.AddSingleton<IConditionImporter, MarkdownConditionImporter>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IStatBlockConverter, TtrpgConvertConverter>();
        services.AddSingleton<IConversionSettingsStore, JsonConversionSettingsStore>();
        services.AddSingleton<ISourceDataService, SourceDataService>();
        services.AddSingleton<IConverterConfigWriter, ConverterConfigWriter>();
        // One HttpClient for the app's lifetime. GitHub's API rejects requests without a User-Agent.
        services.AddSingleton(_ =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DnDInitiative");
            return http;
        });
        services.AddSingleton<IConverterInstaller, GitHubConverterInstaller>();
        services.AddSingleton<IHomebreweryImporter, HomebreweryImporter>();
        services.AddSingleton<IHomebrewerySettingsStore, JsonHomebrewerySettingsStore>();
        services.AddSingleton<IMartialExploitImporter, MartialExploitImporter>();

        // Start-up
        services.AddSingleton<LegacyDatabaseMover>();
        services.AddSingleton<IStorageInitializer, StorageInitializer>();

        return services;
    }
}