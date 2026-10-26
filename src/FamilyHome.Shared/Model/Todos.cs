namespace FamilyHome.Shared.Model;

public class TodoList : EntityBase
{
    public string Name { get; set; } = "";
    public TodoListKind Kind { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsArchived { get; set; }
    public bool ParentsOnly { get; set; }
    public int SortOrder { get; set; }
    public Guid? LinkedEventId { get; set; }
}

public class TodoCategory : EntityBase
{
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    /// <summary>Reihenfolge im Laden (Einkaufsliste wird danach gruppiert).</summary>
    public int StoreSortOrder { get; set; }
}

public class TodoItem : EntityBase
{
    public Guid ListId { get; set; }
    public string Title { get; set; } = "";
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Note { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset? DoneAt { get; set; }
    public Guid? DoneBy { get; set; }
    public Guid? AssigneePersonId { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public Guid? LinkedEventId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Historie für "Zuletzt gekauft"-Vorschläge und Autovervollständigung.</summary>
public class PurchaseHistory : EntityBase
{
    public string Title { get; set; } = "";
    public Guid? CategoryId { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset LastUsedAt { get; set; }
    public int UseCount { get; set; }
}
