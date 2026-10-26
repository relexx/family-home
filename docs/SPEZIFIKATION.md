# FamilyHome – Spezifikation

Stand: 2026-10-26 · Status: **Entwurf zur Review**

FamilyHome ist eine Familien-App für Android mit eigenem Server und Live-Sync. Das Projekt ist **Open Source (Apache-2.0)**: Jede Familie betreibt ihren **eigenen Server** aus dem offiziellen Docker-Image, und die App ist kostenlos. Sie bündelt Kalender, Todos/Einkaufslisten, ein Beziehungsnetz der Kinder und ihrer Freunde, Links und Familieninfos. **Ein Messenger ist ausdrücklich nicht Teil der App.**

---

## 1. Entscheidungen (aus dem Interview)

| Thema | Entscheidung |
|---|---|
| Plattform | Android, Mindestversion **Android 13 (API 33)**, Ziel-API 36 |
| App-Stack | **Avalonia 12** (C#/XAML, MVVM mit CommunityToolkit.Mvvm), Android-Kopfprojekt für Plattformfunktionen, Desktop-Kopfprojekt nur für schnelle Entwicklung |
| Server | **ASP.NET Core 10** + **SignalR** (Live-Sync) + **EF Core** + **PostgreSQL**, als Docker-Container |
| Gemeinsamer Code | `FamilyHome.Shared`: Modelle, DTOs, Sync-Verträge, Wiederholungslogik |
| Datenquellen | Kalender und Kontakte **getrennt konfigurierbar**: eigener Server oder CalDAV/CardDAV. DAV wird **bidirektional vom Server** synchronisiert; die App spricht nur mit dem eigenen Server |
| Offline | **Voll offline-fähig**: lokale SQLite-Kopie und Ausgangswarteschlange (Outbox) |
| Nutzer | Eigene Accounts im Server. **Eltern = Admin**, **Kinder = eingeschränkte Rechte**. Geräte werden per QR-Code bzw. Einladungscode eingeladen |
| Erinnerungen | **Lokal auf dem Gerät geplant** (AlarmManager), keine Google-Dienste |
| Design | **Material 3** mit **Dynamic Color** (Material You), Hell/Dunkel automatisch |
| Sprache | **Deutsch + Englisch** von Anfang an (resx) |
| Betriebsmodell | **Open Source, Apache-2.0** (Server, Shared und App in einem öffentlichen Repo). **Eine Familie pro Server**, selbst gehostet aus dem offiziellen, signierten Docker-Image. Zentrales Hosting ist für Version 1 nicht geplant |
| Verteilung | **Kostenlos**: Google Play und GitHub Releases (später ggf. F-Droid). Unterstützung freiwillig per **Patreon-Link** |
| Sicherheit | **Zero Trust**: Gerätezertifikate (Schlüssel im Android Keystore) und **mTLS** statt Tokens, **Passkeys** für Eltern und Kinder (für Kinder durch die Eltern abschaltbar), Server-Pinning, Security Gate bei jeder Änderung; Details in [SICHERHEIT.md](SICHERHEIT.md) |
| Aktualität | Play-Variante über Play-Updates, GitHub-Variante mit eigenem Selbst-Updater (signiertes Manifest). Server-Images werden signiert, wöchentlich neu gebaut und bei Selbst-Hostern mit Prüfung ausgerollt; Dependabot + GitHub-Prüfroutinen |
| Deployment | `docker-compose` mit **Caddy** (Let's Encrypt + Prüfung der Gerätezertifikate). Öffentlich erreichbar **ohne VPN**; nur Geräte mit gültigem Zertifikat erreichen den Server. VPN bleibt optional |
| MVP | **Kalender + Erinnerungen**, **Todos + Einkaufsliste** |

---

## 2. Funktionsumfang

### 2.1 Kalender (MVP)
- **Ansichten:** Monat, Woche, 3 Tage, Tag, Agenda (Liste). Zwischen den Ansichten wechselt man per Wischgeste bzw. Umschalter.
- **Mehrere Kalender** pro Familie, jeweils mit Farbe und Quelle (Server oder CalDAV-Collection). Kalender lassen sich ein- und ausblenden.
- **Kategorien** (zum Beispiel Schule, Sport, Arzt, Feier) mit Icon und Farbe. Kategorien sind familienweit und unabhängig vom Kalender.
- **Personenzuordnung:** Ein Termin hat 0..n teilnehmende Personen. Jede Person hat eine Farbe, und es gibt einen Filter nach Person.
- **Fahrdienst:** optional ein Feld „bringt“ und eines „holt ab“ (jeweils eine Person, auch externe aus dem People-Graph).
- **Wiederholungen** nach RFC 5545 (RRULE, EXDATE, Ausnahmen per RECURRENCE-ID), damit es verlustfrei zu CalDAV passt.
- **Erinnerungen:** 0..n Erinnerungen pro Termin (Versatz in Minuten, oder absolut). Auch persönliche Erinnerungen, die nur für einen Nutzer gelten.
- **Feiertage** pro Bundesland werden lokal berechnet (eigene Implementierung mit Osterformel, keine Lizenzabhängigkeit). **Schulferien** kommen über ein abonniertes ICS (nur lesend).
- **Geburtstage** stammen aus Personen (ab Phase 2 aus dem People-Graph). Sie werden als virtueller Kalender eingeblendet, mit Vorlauf-Erinnerung (z. B. 7 Tage vorher: „Geschenk besorgen“).
- **Anhänge** (Fotos von Einladungen, PDFs von Elternbriefen) sind für Phase 3 vorgesehen.

### 2.2 Todos & Einkauf (MVP)
- **Listen** haben einen **Typ**: `General`, `Shopping`, `Packing` (Urlaub) und `Project`. Der Typ steuert Darstellung und Funktionen.
- **Gruppen** sind die Listen selbst (Einkauf, Urlaub Sommer 2027 usw.). Listen können archiviert werden.
- **Kategorien** für Einträge (Obst, Gemüse, Winter, Hygiene …). Sie sind familienweit, haben Icon/Farbe und eine **Sortierreihenfolge**, die im Supermarkt der Ladenreihenfolge entspricht.
- **Eintrag:** Titel, Menge + Einheit, Kategorie, Notiz, erledigt (von wem und wann), zugewiesene Person, Fälligkeit, Erinnerung.
- **Einkaufsliste:**
  - gruppiert nach Kategorie in Ladenreihenfolge, erledigte Einträge wandern nach unten;
  - „Zuletzt gekauft“-Vorschläge mit Autovervollständigung aus der Historie;
  - Duplikate werden zusammengeführt („Milch“ ist schon auf der Liste, also wird die Menge erhöht).
- **Vorlagen:** Jede Liste kann als Vorlage gespeichert werden. Aus einer Vorlage entsteht eine neue Liste (Packliste „Skiurlaub“, „Kindergeburtstag“).
- **Verknüpfung mit dem Kalender:**
  - Ein Eintrag oder eine ganze Liste kann **als Termin übernommen** werden. Dabei entsteht ein neuer Termin mit Verweis.
  - Ein Eintrag oder eine Liste kann mit einem **bestehenden Termin verknüpft** werden (Packliste ↔ Urlaubstermin).
  - Im Termin werden verknüpfte Listen/Einträge mit Fortschritt angezeigt. Im Eintrag wird der Termin angezeigt.
  - Ein Eintrag mit Fälligkeit erscheint optional als Aufgabe in der Agenda-Ansicht.

### 2.3 People-Graph (Phase 2)
- **Person:** Name, Spitzname, Foto, Geburtstag, Telefon, E-Mail, Adresse, **Allergien/Unverträglichkeiten**, Notizen, Kleidergrößen (nur für Familienmitglieder).
- **Beziehungen** sind typisiert und gerichtet bzw. symmetrisch: `Elternteil von`, `Geschwister`, `befreundet mit`, `Partner`, `Großelternteil von`, `Betreuer/Trainer von`, frei definierbare Typen.
- **Gruppen/Cluster:** Kita, Schule/Klasse, Verein, Nachbarn. Eine Person kann mehreren Gruppen angehören.
- **Darstellung:** Graph-Ansicht mit Force-Directed-Layout, selbst gezeichnet mit Skia. Zoomen, Verschieben, Fokus auf eine Person (Nachbarschaft 1–2 Ebenen), Filter nach Gruppe. Zusätzlich gibt es eine Listenansicht.
- **Aktionen:** Anrufen, SMS, E-Mail und Navigation öffnen jeweils die System-Apps. Es gibt keinen eigenen Messenger.
- **Kontaktquelle:** Server oder CardDAV-Adressbuch. Wird ein CardDAV-Adressbuch genutzt, werden graph-spezifische Daten (Beziehungen, Gruppen, Allergien) im eigenen Server gehalten und per vCard-UID verknüpft.
- **Geschenkideen** pro Person (Phase 3).

### 2.4 Linkliste (Phase 2)
- Links zu Webseiten (`https://…`) und **Apps** (Android-Paketname → Launch-Intent, oder App-Deep-Link/Intent-URI).
- Ordner/Gruppen, Icon (Favicon, App-Icon oder eigenes), Sortierung per Drag & Drop.
- Sichtbarkeit pro Rolle (z. B. Banking nur für Eltern).
- Homescreen-Widget „Schnellzugriff“.

### 2.5 Dashboard & Widgets (Phase 2)
- Startseite: heutige und morgige Termine, offene Todos (mir zugewiesen bzw. fällig), nächste Geburtstage, Essen heute.
- Android-Widgets (RemoteViews): „Heute“ (Termine) und „Einkaufsliste“ (abhaken direkt im Widget).

### 2.6 Weitere Module (Phase 3)
- **Essensplan:** Woche mit Mahlzeiten pro Tag. Gerichte mit Zutaten, und ein Klick überträgt die Zutaten in die Einkaufsliste.
- **Notfall- und Infokarte:** Kinderarzt, Notfallnummern, Versicherungsnummern, Impfungen, Blutgruppe, Größen. Diese Daten sind besonders geschützt (siehe 5.3).
- **Geschenkideen** pro Person (verknüpft mit dem Graph und dem Geburtstag).
- **Anhänge** an Terminen und Todos (Dateispeicher auf dem Server).

### 2.7 „Wo ist was?“ – Haushalts-Memorizer (Phase 3)
Ein durchsuchbares Verzeichnis, in dem steht, wo sich was im Haus befindet (z. B. „Staubsauger → Schrank im Flur, EG“).
- **Orte als Hierarchie:** Gebäude/Etage → Raum → Möbel/Behälter → Fach (z. B. *EG → Flur → Garderobenschrank → unten links*). Orte haben selbst ein Foto, damit man „die blaue Kiste im Keller“ wiedererkennt.
- **Gegenstand:** Name, Ort, ein oder mehrere **Fotos**, Notiz, Menge, optional „verliehen an“ (Person aus dem Graph) und „zuletzt gesehen am“.
- **Keywords:**
  - manuell vergebene Schlagwörter;
  - **automatisch aus dem Text:** Normalisierung, Wortstamm, zusammengesetzte Wörter zerlegt (*Winterreifen → Winter, Reifen*), Synonym-Liste (*Staubsauger ↔ Sauger*);
  - **automatisch aus dem Foto:** Bilderkennung schlägt Labels vor (*vacuum cleaner → Staubsauger*), die der Nutzer bestätigt oder verwirft. Die Variante ist noch offen: on-device (ML Kit Image Labeling mit gebündeltem Modell, ohne Google-Konto) oder serverseitig (selbst gehostetes ONNX-Modell, z. B. CLIP).
- **Suche:** Volltextsuche über Name, Ort, Keywords und Notiz. Sie ist fehlertolerant (Tippfehler) und funktioniert offline (SQLite FTS5 lokal, PostgreSQL-Volltext/`pg_trgm` auf dem Server). Treffer zeigen Foto und den kompletten Ortspfad.
- **Schnellerfassung:** Foto aufnehmen, Ort aus den zuletzt genutzten wählen, Keywords werden vorgeschlagen, speichern.
- **Umzug/Aufräumen:** Ganze Behälter verschieben, und alle Gegenstände darin wandern mit.
- Optional: QR-Etiketten für Kisten. Scannen zeigt den Inhalt.

---

## 3. Architektur

```
┌──────────────────────────── Android-Gerät ────────────────────────────┐
│  FamilyHome.Client (Avalonia, MVVM)                                   │
│   Views ─ ViewModels ─ Services ─┬─ LocalStore (SQLite/EF Core)       │
│                                  ├─ SyncEngine (Outbox, Pull, SignalR)│
│                                  └─ IPlatformServices (Interface)     │
│  FamilyHome.Client.Android: Alarme, Notifications, Widgets, WorkManager,│
│                         Dynamic Color, Intents, Boot-Receiver         │
└───────────────────────────────┬───────────────────────────────────────┘
                                │ mTLS mit Gerätezertifikat (REST + SignalR)
                          ┌─────▼─────┐
                          │   Caddy   │ Let's Encrypt, prüft Gerätezertifikat
                          └─────┬─────┘
┌───────────────────────────────▼───────────────────────────────────────┐
│  FamilyHome.Server (ASP.NET Core)                                     │
│   Geräte-Auth (Zertifikat) · Familien-CA · Sync-API · SyncHub         │
│   Domain-Services · EF Core ─► PostgreSQL                             │
│   DavSyncWorker (BackgroundService) ◄──► CalDAV/CardDAV-Server        │
│   IcsSubscriptionWorker (Schulferien etc., nur lesend)                │
└───────────────────────────────────────────────────────────────────────┘
```

### 3.1 Projekte

| Projekt | Ziel-Framework | Inhalt |
|---|---|---|
| `FamilyHome.Shared` | `net10.0` | Entitäts-Records, DTOs, Enums, Sync-Verträge (`SyncChange`, `PushRequest` …), Wiederholungsexpansion, Feiertagsberechnung, Berechtigungsregeln |
| `FamilyHome.Server` | `net10.0` | Web-API, SignalR-Hub, EF Core (Npgsql), DAV-Sync, Auth |
| `FamilyHome.Client` | `net10.0` | Avalonia-UI (Views, ViewModels, Controls wie Kalender und Graph), lokale DB, Sync-Engine, Lokalisierung |
| `FamilyHome.Client.Android` | `net10.0-android` | Android-Kopfprojekt, `IPlatformServices`-Implementierung |
| `FamilyHome.Client.Desktop` | `net10.0` | Desktop-Kopfprojekt für schnelle UI-Iteration (kein Produktziel) |
| `tests/*` | `net10.0` | xUnit v3: Shared-Logik, Server (WebApplicationFactory + Testcontainers/PostgreSQL) |

### 3.2 Plattform-Abstraktion

`FamilyHome.Client` kennt nur das Interface `IPlatformServices`:

```csharp
public interface IPlatformServices
{
    IReminderScheduler Reminders { get; }   // AlarmManager / Desktop: Timer
    INotificationService Notifications { get; }
    IAppLauncher Launcher { get; }          // URLs, App-Intents, Telefon, Mail, Maps
    IThemeSource Theme { get; }             // Dynamic-Color-Palette
    IBackgroundSync BackgroundSync { get; } // WorkManager
    ISecureStorage SecureStorage { get; }   // Schlüssel/Geheimnisse (Android Keystore)
    IWidgetUpdater Widgets { get; }
}
```

---

## 4. Synchronisation

### 4.1 Grundprinzip
- **Der Server ist die Wahrheit**, die Geräte halten eine vollständige lokale Kopie der Familiendaten, die sie sehen dürfen.
- **IDs werden auf dem Client erzeugt** (UUIDv7, `Guid.CreateVersion7()`). Neue Objekte lassen sich so offline anlegen und sofort verknüpfen.
- **Änderungen sind Patches** (nur geänderte Felder) und keine ganzen Objekte. Zwei Personen, die verschiedene Felder desselben Eintrags ändern, erzeugen dadurch keinen Konflikt.
- **Reihenfolge:** Der Server vergibt eine streng monotone **Sequenznummer** (`Seq`, `bigint`). Jede angenommene Änderung bekommt eine `Seq`.

### 4.2 Ablauf
1. **Lokale Änderung:** Sie wird in die lokale DB geschrieben (die UI aktualisiert sofort) und als `PendingChange` in die **Outbox** gelegt.
2. **Push:** `POST /api/v1/sync/push` mit den Outbox-Einträgen (`ChangeId`, `EntityType`, `EntityId`, `Operation` = Upsert/Delete, `Patch` als JSON, `BaseSeq`). Der Server prüft die Rechte, wendet die Änderungen in einer Transaktion an, vergibt `Seq` und antwortet pro `ChangeId` mit „angenommen“, „abgelehnt + Grund“ oder „zusammengeführt“. Pushes sind **idempotent** über `ChangeId`.
3. **Broadcast:** Nach dem Commit sendet der Server per SignalR an alle verbundenen Geräte das Signal `ChangesAvailable(latestSeq)`.
4. **Pull:** `GET /api/v1/sync/pull?since={seq}&limit=500` liefert alle Änderungen > `since`, gefiltert nach Rechten, inklusive Tombstones für Löschungen. Der Client wendet sie an und speichert den Cursor.
5. **Erster Start:** Pull ab `since=0` (bzw. ein Snapshot-Endpunkt, falls die Datenmenge wächst).

### 4.3 Konflikte
- **Feldweise gilt: Wer zuletzt beim Server ankommt, gewinnt.** Die Server-Reihenfolge entscheidet.
- **Löschen gegen Bearbeiten:** Löschen gewinnt, die bearbeitende Seite bekommt einen Hinweis. Ausnahme: Ein abgehakter Einkaufseintrag, der gleichzeitig gelöscht wurde, bleibt gelöscht.
- **Wiederholte Termine:** Die Bearbeitung einer einzelnen Instanz erzeugt eine Ausnahme-Entität und keinen Patch an der Serie.
- **Zähler/Mengen:** Bei der Zusammenführung von Duplikaten sendet der Client eine „+n“-Operation statt eines absoluten Werts (`Operation = Increment`).
- **Löschungen** werden als Tombstones (`DeletedAt`) 90 Tage gehalten, danach bereinigt. Ein Client mit einem älteren Cursor macht einen vollständigen Resync.

### 4.4 Verbindung
- **App im Vordergrund:** Die SignalR-Verbindung ist dauerhaft offen (mit automatischem Reconnect). Bei `ChangesAvailable` folgt ein Pull, bei lokalen Änderungen sofort ein Push.
- **App im Hintergrund:** WorkManager synchronisiert periodisch (alle 15 Minuten, Android-Minimum) und bei Netzwechsel. Da Erinnerungen lokal geplant werden, ist die Verzögerung im Hintergrund akzeptabel.
- **Nach jedem Sync** werden die Alarme der nächsten 14 Tage neu geplant und die Widgets aktualisiert.

### 4.5 CalDAV/CardDAV (serverseitig)
- **Quelle** (`ExternalSource`): Typ (`CalDav` bzw. `CardDav`), Basis-URL, Benutzername, App-Passwort (verschlüsselt mit ASP.NET Data Protection), Sync-Intervall.
- **Zuordnung:** Je DAV-Collection wird ein App-Kalender bzw. -Adressbuch angelegt, mit Richtung `Bidirectional` oder `ReadOnly`.
- **Die Konfiguration ist getrennt:** Kalender können z. B. aus Nextcloud kommen, während Kontakte nur im eigenen Server liegen, oder umgekehrt.
- **Abruf:** WebDAV-Sync (RFC 6578, `sync-collection`) mit `sync-token`. Fallback ist ein ETag-Vergleich via `calendar-query`. Das Intervall beträgt standardmäßig 5 Minuten.
- **Schreiben:** Änderungen aus der App an DAV-gebundenen Objekten schreibt der Server per `PUT` mit `If-Match: <etag>` zurück. Bei `412` holt er neu, führt feldweise zusammen und versucht es erneut.
- **App-spezifische Felder** (Kategorie-ID, Personen, Fahrdienst, verknüpfte Todos) werden **zusätzlich** als `X-FAMILYHOME-*`-Properties ins iCal geschrieben und immer auch in der eigenen DB gehalten. Fremde Clients ignorieren sie.
- **Bibliotheken:** `Ical.Net` (iCalendar); vCard per eigenem schlanken Parser oder `FolkerKinzel.VCards`. Das DAV-Protokoll wird selbst mit `HttpClient` umgesetzt, weil es keine reife .NET-CalDAV-Bibliothek gibt.

---

## 5. Benutzer, Rollen & Sicherheit

### 5.1 Accounts
- **Familie:** **Ein Server gehört genau einer Familie.** Es gibt keine Mandantentrennung im Datenmodell (kein `FamilyId`); die Familieneinstellungen sind ein einzelner Datensatz.
- **Benutzer** (`Account`): Anzeigename, Rolle, verknüpfte `Person`, `PasskeyRequired` (bei Kindern durch Eltern abschaltbar), registrierte Passkeys. **Es gibt kein Passwort.**
- **Ersteinrichtung:**
  - Der erste Start des Servers erzeugt die Familien-CA und einen Einrichtungscode im Log.
  - Mit dem Code legt man den ersten Eltern-Account samt Passkey an und erhält einmalig einen **Wiederherstellungscode** zum Ausdrucken.
- **Einladung:** Ein Elternteil erzeugt in der App eine Einladung (Rolle, Person, Gültigkeit 48 h). Sie wird als QR-Code mit `familyhome://join?server=…&code=…` oder als Code zum Abtippen angezeigt.
- **Geräte-Identität:**
  - Jedes Gerät erhält beim Beitritt ein **kurzlebiges Zertifikat** der Familien-CA (7 Tage, automatisch erneuert). Der private Schlüssel liegt im Android Keystore.
  - Jede Verbindung ist **mutual TLS**; es gibt keine Access- oder Refresh-Tokens. Details in [SICHERHEIT.md](SICHERHEIT.md), Kap. 2.1.
- **Anmeldung:**
  - **Eltern und Kinder** melden sich mit **Passkey** an. Für Kinder können die Eltern den Passkey **deaktivieren**; dann bindet allein die von den Eltern bestätigte Einladung das Gerät.
  - Kritische Aktionen verlangen eine erneute Passkey-Bestätigung (Step-up). Jeder Beitritt muss von einem Elternteil bestätigt werden.
- **Geräteverwaltung:** Eltern sehen alle Geräte und können sie abmelden.

### 5.2 Rechte

| Aktion | Eltern | Kind |
|---|---|---|
| Termine sehen | alle | alle außer als „privat“ markierte fremde |
| Termine anlegen/ändern | alle | eigene + solche, an denen es teilnimmt (konfigurierbar) |
| Termine löschen | alle | nur selbst angelegte |
| Todo-Listen sehen | alle | alle außer als „nur Eltern“ markierte |
| Todo-Einträge abhaken | alle | ja |
| Todo-Einträge anlegen | alle | Einkauf + eigene Listen |
| Listen/Kategorien/Vorlagen verwalten | ja | nein |
| Personen/Graph bearbeiten | ja | nur lesen (Phase 2) |
| Notfallkarte | lesen/schreiben | eigene Karte lesen |
| Quellen (DAV), Accounts, Geräte | ja | nein |

Die Rechte werden **auf dem Server** durchgesetzt; die UI blendet nur aus. Die Regeln liegen in `FamilyHome.Shared` (`PermissionRules`), damit Client und Server dieselbe Logik nutzen.

### 5.3 Sicherheit & Datenschutz
Das vollständige Sicherheitskonzept steht in **[SICHERHEIT.md](SICHERHEIT.md)**: Bedrohungsmodell, Zero Trust, Transport, Datenverschlüsselung, Self-Updates, GitHub-Prüfroutinen und DevContainer. Die Kurzfassung:
- Öffentlich erreichbar ohne VPN, aber nur Geräte mit gültigem Zertifikat der Familien-CA erreichen den Server (mTLS am Proxy); Pinning des Server-Schlüssels beim Beitritt
- Rechte pro Objekt auf dem Server (deny by default, automatisch per Endpunkt-Inventar getestet), getrennte DB-Rollen, Audit-Log
- Jede Änderung durchläuft das **Security Gate** (Analyzer, Banned APIs, CodeQL, Semgrep, gitleaks, Linter für Container und Pipelines, Sicherheitstests)
- Lokale DB mit SQLCipher verschlüsselt (ab Phase 1), sensible Felder serverseitig zusätzlich verschlüsselt, verschlüsselte Backups mit Restore-Test
- Keine Telemetrie, keine Drittanbieter-SDKs

---

## 6. Datenmodell (MVP)

Gemeinsame Basisfelder jeder synchronisierten Entität:
`Id (Guid v7)`, `Seq (long)`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `DeletedAt?`

```
FamilySettings  Name, TimeZone, Bundesland (für Feiertage)          (genau ein Datensatz)
Person          DisplayName, Color, Birthday?, IsFamilyMember, AccountId?
Account         DisplayName, Role(Parent|Child), PersonId, PasskeyRequired, IsActive
Passkey         AccountId, CredentialId, PublicKey, SignCount, CreatedAt
Device          AccountId, Name, CertThumbprint, CertNotAfter, AttestationLevel, IsActive, LastSeenAt

Calendar        Name, Color, SourceId?, ExternalHref?, Direction, IsVisibleByDefault
EventCategory   Name, Color, Icon, SortOrder
Event           CalendarId, Title, Description, Location, Start, End, IsAllDay,
                TimeZone, RRule?, ExDates[], CategoryId?, IsPrivate,
                DropOffPersonId?, PickUpPersonId?, ExternalUid?, ExternalEtag?
EventOverride   EventId, RecurrenceId, (gleiche Felder wie Event, nullable) , IsCancelled
EventAttendee   EventId, PersonId
Reminder        OwnerType(Event|TodoItem), OwnerId, OffsetMinutes?, AbsoluteAt?, AccountId? (persönlich)

TodoList        Name, Kind(General|Shopping|Packing|Project), Icon, Color, IsTemplate,
                IsArchived, ParentsOnly, SortOrder, LinkedEventId?
TodoCategory    Name, Icon, Color, StoreSortOrder
TodoItem        ListId, Title, Quantity?, Unit?, CategoryId?, Note, IsDone, DoneAt?, DoneBy?,
                AssigneePersonId?, DueAt?, LinkedEventId?, SortOrder
PurchaseHistory Title, CategoryId?, Unit?, LastUsedAt, UseCount   (für Vorschläge)

ExternalSource  Kind(CalDav|CardDav|IcsSubscription), Url, Username, SecretProtected,
                SyncToken?, LastSyncAt, IntervalMinutes        (nur Server, nie an Clients)

ChangeLog       Seq, EntityType, EntityId, ChangedFields, Visibility  (nur Server)
AuditLog        At, AccountId?, DeviceId?, Action, Details  (nur Server, nur anhängen)
```

Zeiten: Termine werden mit Zeitzone (IANA) gespeichert, intern zusätzlich als UTC (`StartUtc`/`EndUtc`) für Abfragen. Ganztägige Termine sind `DateOnly`.

---

## 7. UI-Struktur

Untere Navigationsleiste (Material 3 Navigation Bar):

1. **Heute**: im MVP eine einfache Agenda für heute/morgen plus fällige Todos, später das Dashboard
2. **Kalender**: Ansichtsumschalter oben, FAB „Neuer Termin“, Filter-Sheet (Kalender, Personen, Kategorien)
3. **Listen**: Übersicht der Listen nach Typ, FAB „Neue Liste / aus Vorlage“
4. **Leute**: ab Phase 2
5. **Mehr**: Links (Phase 2), Notfallkarte und „Wo ist was?“ (Phase 3), Einstellungen, Quellen, Familie & Geräte

Prinzipien: einhändig bedienbar, schnelles Erfassen (Einkaufseintrag in einer Zeile mit Autovervollständigung, Termin-Schnellerfassung „Morgen 15 Uhr Fußball Max“ in Phase 2), große Touch-Ziele, Dark Mode.

**Theme:** Avalonia FluentTheme als Basis mit eigenen Material-3-Styles (Ressourcen-Tokens `md.sys.color.*`). Unter Android liefert `IThemeSource` die Systempalette (`android.R.color.system_accent1_*` usw.), auf dem Desktop gibt es eine feste Fallback-Palette.

---

## 8. Android-Spezifika

| Thema | Umsetzung |
|---|---|
| Erinnerungen | `AlarmManager.setExactAndAllowWhileIdle`, Berechtigung `USE_EXACT_ALARM` (zulässig für Kalender-Apps), Neuplanung bei `BOOT_COMPLETED`, `TIMEZONE_CHANGED` und nach jedem Sync |
| Benachrichtigungen | Kanäle „Termine“, „Aufgaben“, „Geburtstage“, „Sync-Hinweise“. Laufzeitberechtigung `POST_NOTIFICATIONS` (API 33). Aktionen: „Erledigt“, „Schlummern“ |
| Hintergrund-Sync | WorkManager (`Xamarin.AndroidX.Work.Runtime`): periodisch und auf Netz-Constraint |
| Widgets | `AppWidgetProvider` + `RemoteViews`/`RemoteViewsService` (native Android-Layouts im Kopfprojekt) |
| Deep-Links | Intent-Filter `familyhome://` (Einladung, Termin öffnen) |
| Externe Apps | `PackageManager.GetLaunchIntentForPackage`; `<queries>` im Manifest für Paket-Sichtbarkeit |
| Varianten | Ein Code, zwei Builds über `-p:Distribution=Play\|Direct`. **Play:** Updates über Play (In-App-Updates-API für Pflicht-Updates), kein `REQUEST_INSTALL_PACKAGES`. **Direct** (GitHub Releases): eigener Selbst-Updater mit Ed25519-signiertem Manifest, Installation per `PackageInstaller`-Session, nach dem ersten Update still (SICHERHEIT.md, Kap. 5.1) |
| Passkeys | AndroidX Credential Manager. Die Relying-Party-ID ist die Domain des Familien-Servers, der `/.well-known/assetlinks.json` automatisch ausliefert |
| Gerätezertifikat | Schlüssel im Android Keystore (StrongBox, falls vorhanden), CSR beim Beitritt, mTLS über `AndroidMessageHandler` mit eigenem `KeyManager`; SignalR über Server-Sent Events (Spike in Phase 0) |
| Play-Richtlinien | `USE_EXACT_ALARM` als Kalender-Kernfunktion deklarieren. Zielgruppe „Eltern“ (Familien-Richtlinie prüfen, da Kinder die App mitnutzen). Datensicherheits-Formular: keine Datenweitergabe, kein Tracking |
| Härtung | `network_security_config` ohne Klartext, `allowBackup=false`, `FLAG_SECURE` auf sensiblen Screens, nur nötige Komponenten exportiert, Join-Deep-Links zeigen den Server und verlangen eine Bestätigung |
| Signierung | APK Signature Scheme v3. Play: Play App Signing, der Upload-Schlüssel liegt im GitHub-Environment `release`. Direct: eigener Release-Schlüssel ebenfalls nur dort, nie im Repo oder im DevContainer |

---

## 9. Server-API (MVP, Überblick)

Alle Pfade außer den als **öffentlich** markierten verlangen ein gültiges Gerätezertifikat (mTLS am Proxy plus Prüfung im Server).

```
öffentlich  GET  /.well-known/assetlinks.json   für Passkeys (statisch)
öffentlich  POST /api/v1/setup                  Ersteinrichtung (Setup-Code; danach dauerhaft deaktiviert)
öffentlich  POST /api/v1/join                   Einladung einlösen: Code + Passkey-Registrierung + CSR → Gerätezertifikat
                                                (nur erreichbar, solange eine Einladung offen ist)

GET  /api/v1/info                     Server-Version, API-Version, Capabilities, minAppVersion
POST /api/v1/device/renew             neues Zertifikat per CSR (vor Ablauf, automatisch)
POST /api/v1/invitations              (Eltern, Step-up)
GET  /api/v1/devices · DELETE /api/v1/devices/{id}   (Eltern, Step-up)
POST /api/v1/stepup/begin · /finish   Passkey-Assertion für kritische Aktionen

GET  /api/v1/sync/pull?since=&limit=
POST /api/v1/sync/push
GET  /api/v1/sync/snapshot            (Erst-Sync, optional)

GET/POST/PUT/DELETE /api/v1/sources   (Eltern; DAV-/ICS-Quellen konfigurieren, Collections entdecken)
GET  /api/v1/app/latest               (nur Direct-Variante) signiertes Update-Manifest

SignalR: /hubs/sync   Server→Client: ChangesAvailable(long seq), SourceSyncStatus(...), DeviceRevoked
                      Transport: Server-Sent Events (mTLS-fähig), Client→Server: keine Daten
intern   GET  /health                 nur im Container-Netz (Docker-Healthcheck), nicht über den Proxy
```

---

## 10. Phasenplan

| Phase | Inhalt | Ergebnis |
|---|---|---|
| **0 – Fundament** | DevContainer (mit Egress-Firewall), Solution, **Security Gate** und GitHub-Prüfroutinen (SICHERHEIT.md Kap. 6–7), Familien-CA, Gerätezertifikate + mTLS über Caddy (inkl. Spike unter .NET Android), Passkeys, Audit-Log, Ersteinrichtung, Sync-Kern (Push/Pull/SignalR) mit Tests, App-Shell mit Navigation, Login/Join, lokale DB, Theme, DE/EN | Gerät kann sich verbinden, Daten fließen |
| **1 – MVP** | Kalender (alle Ansichten, Kategorien, Personen, Wiederholungen, Erinnerungen, Feiertage), Todos & Einkauf (Listen, Kategorien, Vorlagen, Vorschläge, Kalender-Verknüpfung), CalDAV bidirektional, Schulferien-ICS, Notifications, Hintergrund-Sync, SQLCipher, Step-up, Updates (Play bzw. Selbst-Updater), Versions-Aushandlung App↔Server, signiertes Server-Deployment mit Rollback, Backups | Familie nutzt die App täglich |
| **2** | People-Graph inkl. CardDAV, Geburtstage aus dem Graph, Fahrdienst, Linkliste, Dashboard, Widgets, Schnellerfassung | |
| **3** | Essensplan, Notfallkarte, Geschenkideen, Anhänge, „Wo ist was?“ (Memorizer mit Fotos und Auto-Keywords) | |

---

## 11. Offene Punkte
- GitHub-Organisation und Repo-Name (öffentlich), Projektwebsite, Patreon-Seite
- Ist ein Patreon-Link **in der Play-Variante** mit der Play-Zahlungsrichtlinie vereinbar? Bis zur Klärung steht er nur in der Direct-Variante, im README und auf der Website
- F-Droid: Unterstützt deren Build-Infrastruktur .NET Android? Sind reproduzierbare Builds machbar?
- Eigener Testserver für die Entwicklung (Homeserver/NAS/VPS mit Domain und Portfreigabe 443)
- Bundesland für die Feiertage und die Quelle für das Schulferien-ICS
- Gibt es bereits einen CalDAV-Server (Nextcloud o. ä.) für erste Integrationstests? Im DevContainer läuft zum Testen ein **Radicale**.
- Welche Familienmitglieder bekommen Geräte bzw. Accounts (für Seed-/Testdaten)
- „Wo ist was?“: Bilderkennung on-device (ML Kit) oder serverseitig (ONNX)?
- Backup-Ziel außer Haus für restic (z. B. zweiter Standort, S3-kompatibler Speicher)
