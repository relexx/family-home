namespace FamilyHome.Shared.Model;

public enum Role
{
    Parent = 0,
    Child = 1,
}

public enum TodoListKind
{
    General = 0,
    Shopping = 1,
    Packing = 2,
    Project = 3,
}

public enum SourceKind
{
    /// <summary>Daten liegen nativ im FamilyHome-Server.</summary>
    Server = 0,
    CalDav = 1,
    CardDav = 2,
    /// <summary>Abonniertes ICS (z. B. Schulferien), immer nur lesend.</summary>
    IcsSubscription = 3,
}

public enum SyncDirection
{
    Bidirectional = 0,
    ReadOnly = 1,
}

public enum ReminderOwnerType
{
    Event = 0,
    TodoItem = 1,
}

/// <summary>Bundesländer nach ISO 3166-2:DE (ohne Präfix "DE-").</summary>
public enum FederalState
{
    BW, BY, BE, BB, HB, HH, HE, MV, NI, NW, RP, SL, SN, ST, SH, TH,
}
