using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FamilyHome.Server.Sync;

/// <summary>
/// Live-Kanal: überträgt keine Daten, nur das Signal "es gibt Neues" (Spezifikation 4.2, Schritt 3).
/// Die Daten selbst holt der Client per Pull, damit Rechte-Filter und Cursor an einer Stelle bleiben.
/// Ein Server gehört genau einer Familie, daher gehen Signale an alle verbundenen Geräte.
/// </summary>
[Authorize]
public class SyncHub : Hub;
