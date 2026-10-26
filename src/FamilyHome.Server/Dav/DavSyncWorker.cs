namespace FamilyHome.Server.Dav;

/// <summary>
/// Synchronisiert CalDAV/CardDAV-Quellen bidirektional und ICS-Abos lesend (Spezifikation 4.5).
/// </summary>
public class DavSyncWorker(ILogger<DavSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Tick);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // TODO Phase 1:
            //  - fällige ExternalSources laden (LastSyncAt + IntervalMinutes)
            //  - CalDAV: REPORT sync-collection (RFC 6578) mit gespeichertem sync-token, Fallback ETag-Vergleich
            //  - geänderte VEVENTs mit Ical.Net parsen, auf CalendarEvent/EventOverride abbilden, X-FAMILYHOME-*-Properties lesen
            //  - lokale Änderungen an DAV-gebundenen Objekten per PUT + If-Match zurückschreiben; bei 412 neu holen und mergen
            //  - jede Änderung über denselben Pfad wie ein Client-Push in den ChangeLog schreiben (eigener System-Actor)
            logger.LogTrace("DAV-Sync-Tick (noch nicht implementiert)");
        }
    }
}
