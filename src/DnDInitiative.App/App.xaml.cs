using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.Messaging;

using DnDInitiative.App.Logging;
using DnDInitiative.App.Services;
using DnDInitiative.App.ViewModels;
using DnDInitiative.Core.Combat;
using DnDInitiative.Core.Dice;
using DnDInitiative.Core.Encounters;
using DnDInitiative.Infrastructure;
using DnDInitiative.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace DnDInitiative.App;

public partial class App : Application
{
    private const string AppName = "DnDInitiative";

    private bool _isShowingError;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // When Windows launches the app for a link, the link is the first argument.
        string link = e.Args.FirstOrDefault() ?? "";

        var singleInstance = new SingleInstance(AppName);
        Exit += (_, _) => singleInstance.Dispose();

        if (!singleInstance.IsFirstInstance)
        {
            // Another copy is running: hand it the link and leave.
            await singleInstance.SendToFirstInstanceAsync(link);
            Shutdown();
            return;
        }

        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);
        string logDirectory = Path.Combine(dataDirectory, "logs");

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        ConfigureLogging(builder.Services, logDirectory);
        ConfigureServices(builder.Services, dataDirectory);

        IHost host = builder.Build();
        Exit += (_, _) => host.Dispose();

        ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
        RegisterGlobalExceptionHandlers(logger, logDirectory);
        AppLog.Started(logger);

        // The database must be up to date before any screen asks it for data.
        // If this fails, the handler above reports it and closes the app.
        await host.Services.GetRequiredService<IStorageInitializer>().InitializeAsync();

        var window = host.Services.GetRequiredService<MainWindow>();
        window.Show();

        if (Environment.ProcessPath is { } executablePath)
        {
            UriSchemeRegistrar.Register(EncounterLink.Scheme, executablePath);
        }

        var linkHandler = host.Services.GetRequiredService<EncounterLinkHandler>();

        // Runs for the life of the app; each later launch arrives here.
        singleInstance.ListenAsync(async message =>
        {
            BringToFront(window);
            await linkHandler.HandleAsync(message);
        }).Forget();

        await linkHandler.HandleAsync(link);
    }

    private static void ConfigureLogging(IServiceCollection services, string logDirectory)
    {
        services.AddSerilog(configuration => configuration
            .MinimumLevel.Information()
            // Framework and EF Core chatter is only interesting when something is wrong.
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.File(
                Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatProvider: CultureInfo.InvariantCulture));
    }

    private static void ConfigureServices(IServiceCollection services, string dataDirectory)
    {
        // Domain services
        services.AddSingleton(Random.Shared);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDiceRoller, RandomDiceRoller>();
        services.AddSingleton<InitiativeRoller>();
        services.AddSingleton<CombatantFactory>();

        // Storage and import. Campaigns live in Documents, where the user can find them and put them in git.
        string defaultCampaignDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DnD Initiative",
            "My Campaign");
        services.AddInfrastructure(dataDirectory, defaultCampaignDirectory);

        // App services
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IFolderPicker, FolderPicker>();
        services.AddSingleton<IFilePicker, FilePicker>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IUnsavedChangesPrompt, UnsavedChangesPrompt>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ILinkOpener, BrowserLinkOpener>();
        services.AddSingleton<IUiStateStore>(new JsonUiStateStore(Path.Combine(dataDirectory, "ui-state.json")));
        services.AddSingleton<EncounterLinkHandler>();
        services.AddSingleton<IWindowService, WindowService>();

        // View models
        services.AddSingleton<InitiativeTrackerViewModel>();
        services.AddSingleton<StatBlocksViewModel>();
        services.AddSingleton<EncounterBuilderViewModel>();
        services.AddSingleton<PartySetupViewModel>();
        services.AddSingleton<ImportViewModel>();
        services.AddSingleton<HomebreweryImportViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ReferenceViewerViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    private void RegisterGlobalExceptionHandlers(ILogger<App> logger, string logDirectory)
    {
        // 1. Anything thrown on the UI thread that nothing else caught.
        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.UnhandledException(logger, args.Exception, "the UI thread");
            args.Handled = true;
            ReportError(args.Exception, logDirectory);
        };

        // 2. A failed task that nobody awaited. Raised late, when the task is garbage-collected.
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.UnhandledException(logger, args.Exception, "a background task");
            args.SetObserved();
        };

        // 3. Anything on another thread. The process cannot be saved at this point, only recorded.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppLog.FatalException(logger, exception);
            }
        };
    }

    private void ReportError(Exception exception, string logDirectory)
    {
        // One dialog at a time: a problem that repeats must not bury the screen in message boxes.
        if (_isShowingError)
        {
            return;
        }

        bool hasStarted = MainWindow is { IsLoaded: true };
        string consequence = hasStarted
            ? "The app will keep running, but the last action may not have completed."
            : "The app could not start and will now close.";

        _isShowingError = true;
        try
        {
            MessageBox.Show(
                $"Something went wrong:\n\n{exception.Message}\n\n{consequence}\n\nDetails were saved to:\n{logDirectory}",
                "DnD Initiative",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isShowingError = false;
        }

        if (!hasStarted)
        {
            Shutdown(1);
        }
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();

        // Windows often refuses to let a background app take focus and only
        // flashes its taskbar button. Toggling Topmost raises the window anyway.
        window.Topmost = true;
        window.Topmost = false;
    }
}