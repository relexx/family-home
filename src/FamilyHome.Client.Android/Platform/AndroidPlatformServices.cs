using Android.App;
using Android.Content;
using FamilyHome.Client.Platform;
using AndroidApplication = Android.App.Application;
using Uri = System.Uri;

namespace FamilyHome.Client.Droid.Platform;

/// <summary>Android-Implementierung der Plattformdienste (Spezifikation 3.2 und 8).</summary>
public sealed class AndroidPlatformServices(AndroidApplication app) : IPlatformServices,
    INotificationService, IAppLauncher, IThemeSource, IBackgroundSync, ISecureStorage, IWidgetUpdater
{
    public IReminderScheduler Reminders { get; } = new AlarmReminderScheduler(app);
    public INotificationService Notifications => this;
    public IAppLauncher Launcher => this;
    public IThemeSource Theme => this;
    public IBackgroundSync BackgroundSync => this;
    public ISecureStorage SecureStorage => this;
    public IWidgetUpdater Widgets => this;

    // --- Notifications -------------------------------------------------------------------------

    public Task<bool> EnsurePermissionAsync()
    {
        // TODO: Laufzeitabfrage POST_NOTIFICATIONS über die aktuelle Activity (ActivityResultLauncher)
        var nm = (NotificationManager)app.GetSystemService(Context.NotificationService)!;
        return Task.FromResult(nm.AreNotificationsEnabled());
    }

    public void Show(string channel, string title, string? body, string? deepLink = null) =>
        NotificationChannels.Show(app, channel, title, body, deepLink);

    // --- Externe Apps --------------------------------------------------------------------------

    public Task<bool> OpenUrlAsync(Uri uri) => StartAsync(new Intent(Intent.ActionView, Android.Net.Uri.Parse(uri.ToString())));

    public Task<bool> OpenAppAsync(string packageName)
    {
        var intent = app.PackageManager?.GetLaunchIntentForPackage(packageName);
        return intent is null ? Task.FromResult(false) : StartAsync(intent);
    }

    public Task<bool> DialAsync(string phoneNumber) =>
        StartAsync(new Intent(Intent.ActionDial, Android.Net.Uri.Parse($"tel:{phoneNumber}")));

    public Task<bool> ComposeMailAsync(string address) =>
        StartAsync(new Intent(Intent.ActionSendto, Android.Net.Uri.Parse($"mailto:{address}")));

    private Task<bool> StartAsync(Intent intent)
    {
        try
        {
            intent.AddFlags(ActivityFlags.NewTask);
            app.StartActivity(intent);
            return Task.FromResult(true);
        }
        catch (ActivityNotFoundException)
        {
            return Task.FromResult(false);
        }
    }

    // --- Dynamic Color ---------------------------------------------------------------------------

    public IReadOnlyDictionary<string, uint>? GetDynamicColors(bool dark)
    {
        // TODO: Material-3-Rollen aus den System-Tonpaletten ableiten
        // (Android.Resource.Color.SystemAccent1_*/SystemNeutral1_* → MdPrimary, MdSurface, …)
        return null;
    }

    // --- Hintergrund-Sync ------------------------------------------------------------------------

    public void EnsureScheduled()
    {
        // TODO: PeriodicWorkRequest (15 min, NetworkType.Connected) über AndroidX.Work enqueuen (UniquePeriodicWork "sync")
    }

    // --- Secure Storage --------------------------------------------------------------------------

    // TODO: Werte mit einem Android-Keystore-Schlüssel (AES/GCM) verschlüsseln; vorerst nur App-private SharedPreferences.
    private ISharedPreferences Prefs => app.GetSharedPreferences("familyhome.secure", FileCreationMode.Private)!;

    public Task<string?> GetAsync(string key) => Task.FromResult(Prefs.GetString(key, null));

    public Task SetAsync(string key, string value)
    {
        Prefs.Edit()!.PutString(key, value)!.Apply();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        Prefs.Edit()!.Remove(key)!.Apply();
        return Task.CompletedTask;
    }

    // --- Widgets ---------------------------------------------------------------------------------

    public void RequestUpdate()
    {
        // TODO Phase 2: AppWidgetManager.NotifyAppWidgetViewDataChanged für "Heute" und "Einkaufsliste"
    }
}
