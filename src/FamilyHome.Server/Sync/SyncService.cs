using FamilyHome.Server.Data;
using FamilyHome.Shared.Security;
using FamilyHome.Shared.Sync;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FamilyHome.Server.Sync;

public class SyncService(FamilyHomeDbContext db, IHubContext<SyncHub> hub, ILogger<SyncService> logger)
{
    public const int MaxPullLimit = 500;

    /// <summary>
    /// Wendet die Änderungen eines Geräts in einer Transaktion an (Spezifikation 4.2, Schritt 2).
    /// </summary>
    public async Task<PushResponse> PushAsync(Actor actor, PushRequest request, CancellationToken ct)
    {
        // TODO Phase 0:
        //  1. Transaktion öffnen, SequenceCounter-Zeile sperren (SELECT … FOR UPDATE).
        //  2. Pro ClientChange: Idempotenz prüfen (ChangeLog.ChangeId bereits vorhanden → altes Ergebnis zurückgeben).
        //  3. Entität laden, Rechte über PermissionRules prüfen.
        //  4. Patch feldweise anwenden (Upsert), Tombstone setzen (Delete) oder Zahlenfeld erhöhen (Increment).
        //  5. Seq vergeben, Entität.Seq setzen, ChangeLogEntry schreiben.
        //  6. Commit, danach Broadcast.
        logger.LogWarning("Push noch nicht implementiert ({Count} Änderungen von Gerät {Device})", request.Changes.Count, request.DeviceId);

        var latest = await LatestSeqAsync(ct);
        var results = request.Changes
            .Select(c => new ChangeResult(c.ChangeId, ChangeResultStatus.Rejected, null, "not-implemented"))
            .ToList();

        await hub.Clients.All.SendAsync(SyncHubMethods.ChangesAvailable, latest, ct);
        return new PushResponse(results, latest);
    }

    /// <summary>Liefert alle Änderungen nach <paramref name="since"/>, gefiltert nach Rechten (Spezifikation 4.2, Schritt 4).</summary>
    public async Task<PullResponse> PullAsync(Actor actor, long since, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, MaxPullLimit);

        var entries = await db.ChangeLog
            .Where(c => c.Seq > since)
            .OrderBy(c => c.Seq)
            .Take(limit + 1)
            .ToListAsync(ct);

        // TODO Phase 0: pro Eintrag den aktuellen Zustand der Entität laden, mit PermissionRules filtern
        // und als JSON in ServerChange.Entity packen. Mehrfachänderungen derselben Entität zusammenfassen.
        // TODO: ResyncRequired setzen, wenn 'since' älter ist als die Tombstone-Aufbewahrung (90 Tage).
        var changes = entries.Take(limit)
            .Select(e => new ServerChange(e.Seq, e.EntityType, e.EntityId, e.IsDelete, null))
            .ToList();

        return new PullResponse(changes, await LatestSeqAsync(ct), entries.Count > limit, ResyncRequired: false);
    }

    private async Task<long> LatestSeqAsync(CancellationToken ct) =>
        await db.SequenceCounter.Select(s => (long?)s.LastSeq).FirstOrDefaultAsync(ct) ?? 0;
}
