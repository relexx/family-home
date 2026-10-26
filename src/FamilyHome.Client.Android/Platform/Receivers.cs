using Android.App;
using Android.Content;

namespace FamilyHome.Client.Droid.Platform;

[BroadcastReceiver(Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public const string ExtraTitle = "title";
    public const string ExtraBody = "body";
    public const string ExtraDeepLink = "deeplink";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;
        NotificationChannels.Show(context, NotificationChannels.Events,
            intent.GetStringExtra(ExtraTitle) ?? "FamilyHome",
            intent.GetStringExtra(ExtraBody),
            intent.GetStringExtra(ExtraDeepLink));
    }
}

/// <summary>Plant Alarme nach Neustart bzw. Zeitzonenwechsel neu.</summary>
[BroadcastReceiver(Exported = true, Enabled = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionTimezoneChanged, Intent.ActionMyPackageReplaced])]
public class RescheduleReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // TODO: lokale DB öffnen, Erinnerungen der nächsten 14 Tage berechnen, AlarmReminderScheduler.ReplaceAllAsync
        //       (über goAsync() bzw. einen einmaligen WorkManager-Job, da Receiver nur ~10 s laufen dürfen)
    }
}

public static class NotificationChannels
{
    public const string Events = "events";
    public const string Tasks = "tasks";
    public const string Birthdays = "birthdays";
    public const string Sync = "sync";

    public static void EnsureCreated(Context context)
    {
        var nm = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        // TODO: Kanalnamen lokalisieren (Strings.resx)
        nm.CreateNotificationChannel(new NotificationChannel(Events, "Termine", NotificationImportance.High));
        nm.CreateNotificationChannel(new NotificationChannel(Tasks, "Aufgaben", NotificationImportance.Default));
        nm.CreateNotificationChannel(new NotificationChannel(Birthdays, "Geburtstage", NotificationImportance.Default));
        nm.CreateNotificationChannel(new NotificationChannel(Sync, "Sync-Hinweise", NotificationImportance.Low));
    }

    public static void Show(Context context, string channel, string title, string? body, string? deepLink)
    {
        var uri = Android.Net.Uri.Parse(deepLink ?? "familyhome://today")
            ?? throw new InvalidOperationException("Ungültiger Deep-Link für die Benachrichtigung.");
        var open = new Intent(Intent.ActionView, uri, context, typeof(MainActivity));
        var pending = PendingIntent.GetActivity(context, 0, open, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notification = new Notification.Builder(context, channel)
            .SetSmallIcon(Resource.Drawable.Icon)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetContentIntent(pending)
            .SetAutoCancel(true)
            .Build();

        var nm = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        nm.Notify(System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue), notification);
    }
}
