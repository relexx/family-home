namespace FamilyHome.Shared.Model;

public class CalendarInfo : EntityBase
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#6750A4";
    /// <summary>null = Server-Kalender, sonst die konfigurierte externe Quelle (CalDAV/ICS).</summary>
    public Guid? SourceId { get; set; }
    /// <summary>href der DAV-Collection, falls extern.</summary>
    public string? ExternalHref { get; set; }
    public SyncDirection Direction { get; set; } = SyncDirection.Bidirectional;
    public bool IsVisibleByDefault { get; set; } = true;
}

public class EventCategory : EntityBase
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#6750A4";
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

public class CalendarEvent : EntityBase
{
    public Guid CalendarId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Location { get; set; }

    /// <summary>Lokale Wandzeit in <see cref="TimeZoneId"/>; bei ganztägigen Terminen nur das Datum relevant.</summary>
    public DateTime StartLocal { get; set; }
    public DateTime EndLocal { get; set; }
    public string TimeZoneId { get; set; } = "Europe/Berlin";
    public bool IsAllDay { get; set; }

    /// <summary>Denormalisiert für Bereichsabfragen; bei Serien der Start der ersten Instanz.</summary>
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }

    /// <summary>RFC-5545-RRULE ohne "RRULE:"-Präfix, z. B. "FREQ=WEEKLY;BYDAY=TU".</summary>
    public string? RRule { get; set; }
    public List<DateTime> ExDates { get; set; } = [];

    public Guid? CategoryId { get; set; }
    public bool IsPrivate { get; set; }
    public Guid? DropOffPersonId { get; set; }
    public Guid? PickUpPersonId { get; set; }

    public string? ExternalUid { get; set; }
    public string? ExternalEtag { get; set; }
}

/// <summary>Ausnahme einer Serieninstanz (RECURRENCE-ID). Nicht gesetzte Felder übernehmen die Serie.</summary>
public class EventOverride : EntityBase
{
    public Guid EventId { get; set; }
    public DateTime RecurrenceId { get; set; }
    public bool IsCancelled { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public DateTime? StartLocal { get; set; }
    public DateTime? EndLocal { get; set; }
}

public class EventAttendee : EntityBase
{
    public Guid EventId { get; set; }
    public Guid PersonId { get; set; }
}

public class Reminder : EntityBase
{
    public ReminderOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    /// <summary>Minuten vor Beginn bzw. Fälligkeit. Entweder Offset oder <see cref="AbsoluteAt"/>.</summary>
    public int? OffsetMinutes { get; set; }
    public DateTimeOffset? AbsoluteAt { get; set; }
    /// <summary>Gesetzt = persönliche Erinnerung nur für diesen Account.</summary>
    public Guid? AccountId { get; set; }
}
