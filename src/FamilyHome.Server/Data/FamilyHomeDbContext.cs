using FamilyHome.Shared.Model;
using Microsoft.EntityFrameworkCore;

namespace FamilyHome.Server.Data;

/// <summary>Datenbank eines Familien-Servers. Ein Server gehört genau einer Familie, daher gibt es keine Mandantentrennung.</summary>
public class FamilyHomeDbContext(DbContextOptions<FamilyHomeDbContext> options) : DbContext(options)
{
    public DbSet<FamilySettings> FamilySettings => Set<FamilySettings>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Passkey> Passkeys => Set<Passkey>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ExternalSource> ExternalSources => Set<ExternalSource>();
    public DbSet<ChangeLogEntry> ChangeLog => Set<ChangeLogEntry>();
    public DbSet<SequenceCounter> SequenceCounter => Set<SequenceCounter>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<CertificateAuthorityRecord> CertificateAuthority => Set<CertificateAuthorityRecord>();

    public DbSet<Person> People => Set<Person>();
    public DbSet<CalendarInfo> Calendars => Set<CalendarInfo>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<EventOverride> EventOverrides => Set<EventOverride>();
    public DbSet<EventAttendee> EventAttendees => Set<EventAttendee>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoCategory> TodoCategories => Set<TodoCategory>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<PurchaseHistory> PurchaseHistory => Set<PurchaseHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeLogEntry>().HasKey(c => c.Seq);
        modelBuilder.Entity<ChangeLogEntry>().Property(c => c.Seq).ValueGeneratedNever();
        modelBuilder.Entity<Device>().HasIndex(d => d.CertThumbprint).IsUnique();
        modelBuilder.Entity<Passkey>().HasIndex(p => p.CredentialId).IsUnique();
        modelBuilder.Entity<AuditLogEntry>().HasIndex(a => a.At);

        // Gemeinsame Konfiguration aller synchronisierten Entitäten
        foreach (var type in modelBuilder.Model.GetEntityTypes().Where(t => typeof(EntityBase).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(type.ClrType).HasIndex(nameof(EntityBase.Seq));
            modelBuilder.Entity(type.ClrType).Ignore(nameof(EntityBase.IsDeleted));
        }

        modelBuilder.Entity<CalendarEvent>().HasIndex(e => e.StartUtc);
        modelBuilder.Entity<TodoItem>().HasIndex(i => new { i.ListId, i.SortOrder });
    }
}

public class Passkey
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AccountId { get; set; }
    public byte[] CredentialId { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public uint SignCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Ein Gerät mit seinem aktuellen Gerätezertifikat (docs/SICHERHEIT.md Kap. 2.1).</summary>
public class Device
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AccountId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>SHA-256-Fingerabdruck (hex, Großbuchstaben) des aktuell gültigen Zertifikats.</summary>
    public string CertThumbprint { get; set; } = "";
    public DateTimeOffset CertNotAfter { get; set; }
    /// <summary>Ergebnis der Key Attestation, z. B. "StrongBox", "TEE", "Software", "Unknown".</summary>
    public string AttestationLevel { get; set; } = "Unknown";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset LastSeenAt { get; set; }
}

public class Invitation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string CodeHash { get; set; } = "";
    public Role Role { get; set; }
    public Guid PersonId { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }
    /// <summary>Beitritt wird erst wirksam, wenn ein Elternteil bestätigt (Vier-Augen-Prinzip).</summary>
    public DateTimeOffset? ApprovedAt { get; set; }
}

/// <summary>Externe Quelle (CalDAV/CardDAV/ICS). Das Geheimnis ist per Data Protection verschlüsselt und verlässt nie den Server.</summary>
public class ExternalSource
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public SourceKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Username { get; set; }
    public string? SecretProtected { get; set; }
    public int IntervalMinutes { get; set; } = 5;
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>Eine angenommene Änderung; Grundlage für Pull (Spezifikation 4.2).</summary>
public class ChangeLogEntry
{
    public long Seq { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public bool IsDelete { get; set; }
    public Guid? ChangeId { get; set; }
    public Guid? AccountId { get; set; }
    public DateTimeOffset At { get; set; }
}

/// <summary>Zähler für die monotone Sequenz (genau eine Zeile; beim Push per SELECT … FOR UPDATE gesperrt).</summary>
public class SequenceCounter
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public long LastSeq { get; set; }
}

/// <summary>Sicherheitsrelevante Ereignisse, nur anhängen (docs/SICHERHEIT.md Kap. 2.3).</summary>
public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTimeOffset At { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? DeviceId { get; set; }
    public string Action { get; set; } = "";
    public string? Details { get; set; }
}

/// <summary>Familien-CA. Der private Schlüssel ist per Data Protection verschlüsselt (genau eine Zeile).</summary>
public class CertificateAuthorityRecord
{
    public const int SingletonId = 1;
    public int Id { get; set; } = SingletonId;
    public string CertificatePem { get; set; } = "";
    public string PrivateKeyProtected { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
