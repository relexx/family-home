using FamilyHome.Shared.Model;
using FamilyHome.Shared.Sync;
using Microsoft.EntityFrameworkCore;

namespace FamilyHome.Client.Data;

/// <summary>Lokale Offline-Kopie der Familiendaten plus Outbox (Spezifikation 4.2).</summary>
public class LocalDbContext : DbContext
{
    public static string DatabasePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FamilyHome", "familyhome.db");

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

    public DbSet<PendingChange> Outbox => Set<PendingChange>();
    public DbSet<SyncState> SyncState => Set<SyncState>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        options.UseSqlite($"Data Source={DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        foreach (var type in b.Model.GetEntityTypes().Where(t => typeof(EntityBase).IsAssignableFrom(t.ClrType)))
            b.Entity(type.ClrType).Ignore(nameof(EntityBase.IsDeleted));

        // SQLite kann DateTimeOffset nicht sortieren/vergleichen → als UTC-Ticks speichern
        b.Entity<CalendarEvent>().Property(e => e.StartUtc).HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
        b.Entity<CalendarEvent>().Property(e => e.EndUtc).HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
        b.Entity<CalendarEvent>().HasIndex(e => e.StartUtc);

        b.Entity<PendingChange>().HasKey(c => c.ChangeId);
        b.Entity<SyncState>().HasKey(s => s.Id);
    }
}

/// <summary>Ein noch nicht vom Server bestätigter Outbox-Eintrag.</summary>
public class PendingChange
{
    public Guid ChangeId { get; set; } = Guid.CreateVersion7();
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public ChangeOperation Operation { get; set; }
    public string? PatchJson { get; set; }
    public long BaseSeq { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

public class SyncState
{
    public int Id { get; set; } = 1;
    public long LastPulledSeq { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
}
