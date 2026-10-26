using FamilyHome.Client.Data;
using FamilyHome.Client.Platform;
using FamilyHome.Shared.Sync;
using Microsoft.AspNetCore.SignalR.Client;

namespace FamilyHome.Client.Sync;

/// <summary>
/// Offline-first-Sync: Outbox pushen, Änderungen pullen, auf SignalR-Signale reagieren (Spezifikation 4.2–4.4).
/// </summary>
public sealed class SyncEngine(Func<LocalDbContext> dbFactory, IPlatformServices platform) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HubConnection? _hub;

    public Uri? ServerUrl { get; private set; }

    public async Task ConnectAsync(Uri serverUrl, CancellationToken ct = default)
    {
        ServerUrl = serverUrl;
        _hub = new HubConnectionBuilder()
            // Authentifizierung per Gerätezertifikat (mTLS), keine Tokens. Server-Sent Events statt WebSockets,
            // weil nur der HttpClient-Stack den Keystore-Schlüssel nutzen kann (docs/SICHERHEIT.md Kap. 2.1).
            // TODO Phase 0: HttpMessageHandlerFactory → mTLS-fähiger Handler aus IPlatformServices
            .WithUrl(new Uri(serverUrl, SyncHubMethods.HubPath), Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents)
            .WithAutomaticReconnect()
            .Build();

        _hub.On<long>(SyncHubMethods.ChangesAvailable, seq => { _ = SyncNowAsync(); });
        _hub.Reconnected += async _ => await SyncNowAsync();

        await _hub.StartAsync(ct);
    }

    /// <summary>Push, dann Pull. Läuft nie parallel; ein zweiter Aufruf wartet.</summary>
    public async Task<bool> SyncNowAsync(CancellationToken ct = default)
    {
        if (ServerUrl is null) return false;

        await _gate.WaitAsync(ct);
        try
        {
            await using var db = dbFactory();
            await db.Database.EnsureCreatedAsync(ct);

            // TODO Phase 0:
            //  1. Outbox lesen → POST /api/sync/push → bestätigte Einträge löschen, abgelehnte markieren/Nutzer informieren
            //  2. GET /api/sync/pull?since=LastPulledSeq in Schleife bis HasMore=false → Entitäten upserten/löschen
            //  3. Bei ResyncRequired: lokale Daten verwerfen (Outbox behalten!) und ab 0 pullen
            //  4. Erinnerungen der nächsten 14 Tage neu planen, Widgets aktualisieren
            platform.Widgets.RequestUpdate();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
        _gate.Dispose();
    }
}
