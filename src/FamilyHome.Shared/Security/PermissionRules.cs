using FamilyHome.Shared.Model;

namespace FamilyHome.Shared.Security;

/// <summary>Wer ist angemeldet – genug Kontext, um die Rechte aus Spezifikation Kap. 5.2 auszuwerten.</summary>
public sealed record Actor(Guid AccountId, Guid PersonId, Role Role)
{
    public bool IsParent => Role == Role.Parent;
}

/// <summary>
/// Rechte-Regeln, gemeinsam genutzt von Server (Durchsetzung) und App (UI ein-/ausblenden).
/// </summary>
public static class PermissionRules
{
    public static bool CanSeeEvent(Actor actor, CalendarEvent ev, IReadOnlyCollection<Guid> attendeePersonIds) =>
        actor.IsParent || !ev.IsPrivate || ev.CreatedBy == actor.AccountId || attendeePersonIds.Contains(actor.PersonId);

    public static bool CanEditEvent(Actor actor, CalendarEvent ev, IReadOnlyCollection<Guid> attendeePersonIds) =>
        actor.IsParent || ev.CreatedBy == actor.AccountId || attendeePersonIds.Contains(actor.PersonId);

    public static bool CanDeleteEvent(Actor actor, CalendarEvent ev) =>
        actor.IsParent || ev.CreatedBy == actor.AccountId;

    public static bool CanSeeList(Actor actor, TodoList list) =>
        actor.IsParent || !list.ParentsOnly;

    public static bool CanAddItem(Actor actor, TodoList list) =>
        actor.IsParent || list.Kind == TodoListKind.Shopping || list.CreatedBy == actor.AccountId;

    public static bool CanCheckItem(Actor actor, TodoList list) => CanSeeList(actor, list);

    public static bool CanManageListsAndCategories(Actor actor) => actor.IsParent;

    public static bool CanManageSourcesAccountsDevices(Actor actor) => actor.IsParent;
}
