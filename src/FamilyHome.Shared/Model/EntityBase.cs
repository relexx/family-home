namespace FamilyHome.Shared.Model;

/// <summary>
/// Basis aller synchronisierten Entitäten (Spezifikation Kap. 4 und 6).
/// IDs werden auf dem Client als UUIDv7 erzeugt, damit Objekte offline angelegt und verknüpft werden können.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Vom Server vergebene, streng monotone Sequenznummer der letzten Änderung. 0 = noch nie synchronisiert.</summary>
    public long Seq { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }

    /// <summary>Tombstone: gesetzt = gelöscht. Wird nach 90 Tagen serverseitig bereinigt.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}
