using System.Text.Json;

namespace FamilyHome.Shared.Sync;

// Sync-Protokoll, siehe Spezifikation Kap. 4.

public enum ChangeOperation
{
    /// <summary>Anlegen oder feldweises Aktualisieren (Patch enthält nur geänderte Felder).</summary>
    Upsert = 0,
    Delete = 1,
    /// <summary>Additive Änderung eines Zahlenfelds (z. B. Menge beim Zusammenführen von Duplikaten).</summary>
    Increment = 2,
}

/// <summary>Eine lokale Änderung, wie sie aus der Outbox an den Server geht.</summary>
public sealed record ClientChange(
    Guid ChangeId,
    string EntityType,
    Guid EntityId,
    ChangeOperation Operation,
    JsonElement? Patch,
    long BaseSeq,
    DateTimeOffset ClientTimestamp);

public sealed record PushRequest(Guid DeviceId, IReadOnlyList<ClientChange> Changes);

public enum ChangeResultStatus
{
    Accepted = 0,
    /// <summary>Angenommen, aber mit anderen Änderungen zusammengeführt – Client soll pullen.</summary>
    Merged = 1,
    Rejected = 2,
}

public sealed record ChangeResult(Guid ChangeId, ChangeResultStatus Status, long? Seq, string? Reason);

public sealed record PushResponse(IReadOnlyList<ChangeResult> Results, long LatestSeq);

/// <summary>Eine Änderung vom Server: vollständiger aktueller Zustand der Entität (oder Tombstone).</summary>
public sealed record ServerChange(
    long Seq,
    string EntityType,
    Guid EntityId,
    bool IsDeleted,
    JsonElement? Entity);

public sealed record PullResponse(IReadOnlyList<ServerChange> Changes, long LatestSeq, bool HasMore, bool ResyncRequired);

public static class SyncHubMethods
{
    public const string HubPath = "/hubs/sync";
    /// <summary>Server → Client: neue Änderungen bis einschließlich <c>long seq</c> verfügbar.</summary>
    public const string ChangesAvailable = nameof(ChangesAvailable);
    public const string SourceSyncStatus = nameof(SourceSyncStatus);
}

public static class EntityTypes
{
    public const string Person = "person";
    public const string Calendar = "calendar";
    public const string EventCategory = "eventCategory";
    public const string Event = "event";
    public const string EventOverride = "eventOverride";
    public const string EventAttendee = "eventAttendee";
    public const string Reminder = "reminder";
    public const string TodoList = "todoList";
    public const string TodoCategory = "todoCategory";
    public const string TodoItem = "todoItem";
    public const string PurchaseHistory = "purchaseHistory";
}
