namespace FamilyHome.Shared.Model;

/// <summary>Einstellungen der Familie. Ein Server gehört genau einer Familie, daher gibt es genau einen Datensatz.</summary>
public class FamilySettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string Name { get; set; } = "";
    /// <summary>IANA-Zeitzone, z. B. "Europe/Berlin".</summary>
    public string TimeZoneId { get; set; } = "Europe/Berlin";
    public FederalState FederalState { get; set; } = FederalState.BY;
}

/// <summary>
/// Eine Person: Familienmitglied oder (ab Phase 2) Kontakt im People-Graph.
/// </summary>
public class Person : EntityBase
{
    public string DisplayName { get; set; } = "";
    /// <summary>Farbe als #RRGGBB, z. B. für Termine und Filter.</summary>
    public string Color { get; set; } = "#6750A4";
    public DateOnly? Birthday { get; set; }
    public bool IsFamilyMember { get; set; }
    public Guid? AccountId { get; set; }
}

/// <summary>
/// Account eines Familienmitglieds. Es gibt kein Passwort: Anmeldung per Passkey,
/// Geräte-Identität per Zertifikat (docs/SICHERHEIT.md Kap. 2).
/// </summary>
public class Account
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string DisplayName { get; set; } = "";
    public Role Role { get; set; }
    public Guid PersonId { get; set; }
    /// <summary>Für Eltern immer true; für Kinder von Eltern abschaltbar.</summary>
    public bool PasskeyRequired { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
