using System.Diagnostics;

namespace FamilyHome.Client.Platform;

/// <summary>Einfache Fallbacks für die Desktop-Entwicklungsvariante. Nicht für Produktion gedacht.</summary>
public sealed class DesktopPlatformServices : IPlatformServices,
    IReminderScheduler, INotificationService, IAppLauncher, IThemeSource, IBackgroundSync, ISecureStorage, IWidgetUpdater
{
    private readonly Dictionary<string, string> _secrets = [];

    public IReminderScheduler Reminders => this;
    public INotificationService Notifications => this;
    public IAppLauncher Launcher => this;
    public IThemeSource Theme => this;
    public IBackgroundSync BackgroundSync => this;
    public ISecureStorage SecureStorage => this;
    public IWidgetUpdater Widgets => this;

    public Task ReplaceAllAsync(IReadOnlyCollection<ScheduledReminder> reminders)
    {
        Debug.WriteLine($"[Desktop] {reminders.Count} Erinnerungen geplant (nicht ausgelöst)");
        return Task.CompletedTask;
    }

    public Task<bool> EnsurePermissionAsync() => Task.FromResult(true);

    public void Show(string channel, string title, string? body, string? deepLink = null) =>
        Debug.WriteLine($"[Desktop] Notification [{channel}] {title}: {body}");

    public Task<bool> OpenUrlAsync(Uri uri)
    {
        Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        return Task.FromResult(true);
    }

    public Task<bool> OpenAppAsync(string packageName) => Task.FromResult(false);
    public Task<bool> DialAsync(string phoneNumber) => Task.FromResult(false);
    public Task<bool> ComposeMailAsync(string address) => OpenUrlAsync(new Uri($"mailto:{address}"));

    public IReadOnlyDictionary<string, uint>? GetDynamicColors(bool dark) => null;

    public void EnsureScheduled() { }

    public Task<string?> GetAsync(string key) => Task.FromResult(_secrets.GetValueOrDefault(key));

    public Task SetAsync(string key, string value)
    {
        _secrets[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _secrets.Remove(key);
        return Task.CompletedTask;
    }

    public void RequestUpdate() { }
}
