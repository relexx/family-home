using System.Globalization;
using Android.App;
using Android.Content;
using FamilyHome.Client.Platform;
using AndroidApplication = Android.App.Application;

namespace FamilyHome.Client.Droid.Platform;

/// <summary>
/// Plant Erinnerungen als exakte Alarme (USE_EXACT_ALARM, zulässig für Kalender-Apps).
/// Wird nach jedem Sync, nach dem Booten und bei Zeitzonenwechsel mit dem 14-Tage-Fenster neu befüllt.
/// </summary>
public sealed class AlarmReminderScheduler(AndroidApplication app) : IReminderScheduler
{
    private const string PrefsName = "familyhome.alarms";
    private const string KeyScheduled = "scheduled";

    public Task ReplaceAllAsync(IReadOnlyCollection<ScheduledReminder> reminders)
    {
        var alarms = (AlarmManager)app.GetSystemService(Context.AlarmService)!;
        var prefs = app.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;

        // Alte Alarme abbrechen (Request-Codes wurden beim letzten Planen gemerkt)
        foreach (var code in prefs.GetStringSet(KeyScheduled, null) ?? [])
            alarms.Cancel(CreatePendingIntent(int.Parse(code, CultureInfo.InvariantCulture), null));

        var codes = new List<string>();
        foreach (var r in reminders.Where(r => r.FireAt > DateTimeOffset.UtcNow))
        {
            var requestCode = r.ReminderId.GetHashCode();
            alarms.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, r.FireAt.ToUnixTimeMilliseconds(), CreatePendingIntent(requestCode, r));
            codes.Add(requestCode.ToString(CultureInfo.InvariantCulture));
        }

        prefs.Edit()!.PutStringSet(KeyScheduled, codes)!.Apply();
        return Task.CompletedTask;
    }

    private PendingIntent CreatePendingIntent(int requestCode, ScheduledReminder? reminder)
    {
        var intent = new Intent(app, typeof(ReminderReceiver));
        if (reminder is not null)
        {
            intent.PutExtra(ReminderReceiver.ExtraTitle, reminder.Title);
            intent.PutExtra(ReminderReceiver.ExtraBody, reminder.Body);
            intent.PutExtra(ReminderReceiver.ExtraDeepLink, reminder.DeepLink);
        }

        return PendingIntent.GetBroadcast(app, requestCode, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }
}
