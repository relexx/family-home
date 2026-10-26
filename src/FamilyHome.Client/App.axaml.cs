using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FamilyHome.Client.Data;
using FamilyHome.Client.Platform;
using FamilyHome.Client.Sync;
using FamilyHome.Client.ViewModels;
using FamilyHome.Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyHome.Client;

public partial class App : Application
{
    /// <summary>
    /// Wird vom Kopfprojekt vor dem Start gesetzt, um plattformspezifische Dienste zu registrieren
    /// (Android: AlarmManager, Notifications …). Ohne Registrierung greifen die Desktop-Fallbacks.
    /// </summary>
    public static Action<IServiceCollection>? ConfigurePlatformServices { get; set; }

    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = Services.GetRequiredService<MainViewModel>() };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () => new PageNavigationHost
            {
                Page = new MainView { DataContext = Services.GetRequiredService<MainViewModel>() },
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new PageNavigationHost
            {
                Page = new MainView { DataContext = Services.GetRequiredService<MainViewModel>() },
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IPlatformServices, DesktopPlatformServices>();
        ConfigurePlatformServices?.Invoke(services); // spätere Registrierung überschreibt den Fallback

        services.AddDbContext<LocalDbContext>(ServiceLifetime.Transient);
        services.AddSingleton<Func<LocalDbContext>>(sp => () => sp.GetRequiredService<LocalDbContext>());
        services.AddSingleton<SyncEngine>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<TodayViewModel>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<ListsViewModel>();
        services.AddTransient<MoreViewModel>();

        return services.BuildServiceProvider();
    }
}
