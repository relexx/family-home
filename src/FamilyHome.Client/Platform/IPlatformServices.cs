namespace FamilyHome.Client.Platform;

/// <summary>
/// Alles, was plattformspezifisch ist (Spezifikation 3.2). Implementiert im Android-Kopfprojekt;
/// <see cref="DesktopPlatformServices"/> ist der Fallback für die Desktop-Entwicklungsvariante.
/// </summary>
public interface IPlatformServices
{
    IReminderScheduler Reminders { get; }
    INotificationService Notifications { get; }
    IAppLauncher Launcher { get; }
    IThemeSource Theme { get; }
    IBackgroundSync BackgroundSync { get; }
    ISecureStorage SecureStorage { get; }
    IWidgetUpdater Widgets { get; }
}

public sealed record ScheduledReminder(Guid ReminderId, DateTimeOffset FireAt, string Title, string? Body, string DeepLink);

public interface IReminderScheduler
{
    /// <summary>Ersetzt alle geplanten Alarme durch die übergebenen (Fenster: nächste 14 Tage).</summary>
    Task ReplaceAllAsync(IReadOnlyCollection<ScheduledReminder> reminders);
}

public interface INotificationService
{
    Task<bool> EnsurePermissionAsync();
    void Show(string channel, string title, string? body, string? deepLink = null);
}

public interface IAppLauncher
{
    Task<bool> OpenUrlAsync(Uri uri);
    Task<bool> OpenAppAsync(string packageName);
    Task<bool> DialAsync(string phoneNumber);
    Task<bool> ComposeMailAsync(string address);
}

/// <summary>Material-3-Farbrollen vom System (Dynamic Color); null = Fallback-Palette verwenden.</summary>
public interface IThemeSource
{
    IReadOnlyDictionary<string, uint>? GetDynamicColors(bool dark);
}

public interface IBackgroundSync
{
    void EnsureScheduled();
}

public interface ISecureStorage
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

public interface IWidgetUpdater
{
    void RequestUpdate();
}
