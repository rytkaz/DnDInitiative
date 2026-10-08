using System.IO;
using System.Windows;

using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Core.Parties;
using DnDInitiative.Core.StatBlocks;
using DnDInitiative.Infrastructure.Encounters;
using DnDInitiative.Infrastructure.Import;
using DnDInitiative.Infrastructure.Parties;
using DnDInitiative.Infrastructure.StatBlocks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DnDInitiative.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        ConfigureServices(builder.Services);

        IHost host = builder.Build();
        Exit += (_, _) => host.Dispose();

        host.Services.GetRequiredService<MainWindow>().Show();

        if (Environment.ProcessPath is { } executablePath)
        {
            UriSchemeRegistrar.Register(EncounterLink.Scheme, executablePath);
        }

        // When Windows launches the app for a link, the link is the first argument.
        if (e.Args.Length > 0)
        {
            await host.Services.GetRequiredService<EncounterLinkHandler>().HandleAsync(e.Args[0]);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Domain services
        services.AddSingleton(Random.Shared);
        services.AddSingleton<IDiceRoller, RandomDiceRoller>();
        services.AddSingleton<InitiativeRoller>();
        services.AddSingleton<CombatantFactory>();
        services.AddSingleton<IStatBlockImporter, MarkdownStatBlockImporter>();
        services.AddSingleton<IFolderPicker, FolderPicker>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<EncounterLinkHandler>();

        // Storage
        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DnDInitiative");

        services.AddSingleton<IPartyRepository>(_ => new JsonPartyRepository(Path.Combine(dataDirectory, "parties.json")));
        services.AddSingleton<IStatBlockRepository>(_ => new JsonStatBlockRepository(Path.Combine(dataDirectory, "statblocks.json")));
        services.AddSingleton<IEncounterRepository>(_ => new JsonEncounterRepository(Path.Combine(dataDirectory, "encounters.json")));

        // View models
        services.AddSingleton<InitiativeTrackerViewModel>();
        services.AddSingleton<StatBlocksViewModel>();
        services.AddSingleton<EncounterBuilderViewModel>();
        services.AddSingleton<PartySetupViewModel>();
        services.AddSingleton<MainViewModel>();
        // Windows
        services.AddSingleton<MainWindow>();
    }
}