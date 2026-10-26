# FamilyHome – Hinweise für Claude

Familien-App für Android (Kalender, Todos/Einkauf, People-Graph, Links, „Wo ist was?“) mit eigenem Server und Live-Sync.
Open Source (Apache-2.0). **Eine Familie pro Server**, selbst gehostet; die App ist kostenlos (Play + GitHub Releases).
**Maßgeblich sind [docs/SPEZIFIKATION.md](docs/SPEZIFIKATION.md) und [docs/SICHERHEIT.md](docs/SICHERHEIT.md).** Vor größeren Änderungen das passende Kapitel lesen. Wenn eine Entscheidung davon abweicht, die Spezifikation im selben Zug anpassen.

## Stack
- .NET 10, C# latest, Nullable an, Central Package Management (`Directory.Packages.props`, dort alle Versionen pflegen)
- App: Avalonia 12 + CommunityToolkit.Mvvm (`[ObservableProperty] public partial …`, `[RelayCommand]`), compiled Bindings
- Server: ASP.NET Core Minimal APIs + SignalR, EF Core + Npgsql
- Tests: xUnit v3 auf Microsoft.Testing.Platform (`dotnet test --project tests/…`)

## Projekte
| Pfad | Zweck |
|---|---|
| `src/FamilyHome.Shared` | Entitäten, Sync-Verträge, Rechte (`PermissionRules`), Feiertage – keine UI-/Server-Abhängigkeiten |
| `src/FamilyHome.Server` | API (`/api/v1`), Geräte-Auth per Zertifikat (`Auth/`), Familien-CA (`Security/`), `SyncService`, `SyncHub`, `DavSyncWorker` |
| `src/FamilyHome.Client` | Avalonia-UI, ViewModels, lokale SQLite-DB, `SyncEngine`, `IPlatformServices` |
| `src/FamilyHome.Client.Android` | Android-Kopf, Namespace `FamilyHome.Client.Droid` (nicht `.Android`, sonst Kollision mit dem `Android.*`-Namespace) |
| `src/FamilyHome.Client.Desktop` | Desktop-Kopf, nur für schnelle UI-Iteration |

## Befehle (im DevContainer)
```bash
dotnet build FamilyHome.slnx
dotnet test --project tests/FamilyHome.Shared.Tests
dotnet test --project tests/FamilyHome.Server.Tests       # inkl. Sicherheitstests
scripts/security-gate.sh                                   # Security Gate komplett (--quick: nur .NET)
dotnet run --project src/FamilyHome.Server                 # http://localhost:5080, DB = Service "db"
dotnet run --project src/FamilyHome.Client.Desktop         # Anzeige im noVNC-Desktop (Port 6080)
dotnet build src/FamilyHome.Client.Android -t:Install      # APK auf verbundenes Gerät (adb)
```

## Konventionen
- Code-Bezeichner Englisch, Kommentare und Doku Deutsch, UI-Texte nur über `Resources/Strings*.resx` (DE neutral, EN)
- Plattformcode nur hinter Interfaces aus `FamilyHome.Client/Platform`; das gemeinsame Projekt kennt kein `Android.*`
- Jede synchronisierte Entität erbt von `EntityBase`; IDs mit `Guid.CreateVersion7()` auf dem Client
- Änderungen gehen immer über die Outbox bzw. den `SyncService`, nie direkt an der DB vorbei (sonst fehlen ChangeLog und Seq)
- Rechte werden auf dem Server mit `PermissionRules` geprüft; der Client nutzt dieselben Regeln nur zum Ausblenden
- Offene Arbeit ist mit `TODO Phase N` markiert (`grep -rn "TODO Phase" src`)

## Sicherheit (Zero Trust) – verbindlich
Sicherheit und Aktualität haben Vorrang vor Komfort. Im Zweifel nachfragen statt abschwächen.
- **Nie** Geheimnisse ins Repo, in Logs oder in Testdaten (Keystore, Signaturschlüssel, Passwörter, Tokens). `.env` bleibt lokal.
- Jeder neue Endpunkt: `RequireAuthorization()` (Ausnahme nur Setup/Login/Join/Health, und die mit Rate Limit), Rechte pro Objekt über `PermissionRules`, deny by default.
- Es gibt **kein `FamilyId`** und keine Mandanten: ein Server = eine Familie. Nicht wieder einführen.
- Authentifizierung ist **ausschließlich das Gerätezertifikat** (mTLS über Caddy, erneute Prüfung im Server). Keine Bearer-Tokens, Passwörter oder Cookies einführen. Personen-Bestätigung (Login neues Gerät, Step-up) nur per Passkey.
- Middleware-Reihenfolge in `Program.cs` nicht ändern, ohne die Tests in `EndpointSecurityTests` zu erweitern (Header-Filter → Forwarded Headers → Zertifikat).
- Öffentliche Endpunkte nur über `DeviceEndpoints.PublicRoutes`; der Endpunkt-Inventar-Test erzwingt das.
- Server-Code: Zeit nur über `TimeProvider`, HTTP nur über `IHttpClientFactory` (BannedSymbols).
- Eingaben validieren (Länge, Format); Deep-Link- und Join-Parameter gelten als feindlich.
- Keine neuen Pakete ohne Grund. Version nur in `Directory.Packages.props`, Lizenz prüfen (keine GPL/AGPL), danach `dotnet restore` ausführen, damit `packages.lock.json` aktuell ist.
- GitHub Actions nur per Commit-SHA pinnen (`uses: owner/action@<sha> # vX.Y.Z`), `permissions` minimal, `persist-credentials: false`.
- Keine Telemetrie- oder Tracking-SDKs, kein Klartext-HTTP außer localhost.
- Sicherheitsrelevante Änderungen (Auth, Rechte, Krypto, Pipeline, Deployment) im PR ausdrücklich als solche kennzeichnen.
- Arbeit auf Feature-Branches mit PR; nie direkt auf `main` pushen. Commits mit `-s` (DCO).

## Arbeitspakete (`work/`)
Abgegrenzte Aufgaben liegen als Arbeitspakete in `work/wp-<work-id>-<kurzname>.md` (Work-ID: 8 Hex-Zeichen, `openssl rand -hex 4`), Vorlage: `work/workpackage.template.md`.
- Arbeitspakete sind auf Englisch geschrieben (Frontmatter-Werte: `open | in-progress | blocked | in-review | done`).
- Ein Paket nur anfangen, wenn `status: open` ist, alle Pakete aus `depends_on` in `work/archive/` liegen und es noch keinen Branch `wp/<work-id>-…` und keinen PR dazu gibt (`git fetch --prune`, `git branch -a --list "*wp/<work-id>*"`, `gh pr list --state all --search <work-id>`). Beanspruchen = Branch anlegen, `status: in-progress` setzen, pushen.
- Das Paket ist der Auftrag: nur die erlaubten Dateien ändern, Schritte und Prüfroutinen der Reihe nach abarbeiten. Bei einer Abbruchbedingung `status: blocked` setzen und nachfragen, nicht improvisieren.
- Ein Paket = ein PR. Der Worker setzt höchstens `in-review`. Abnahme (`done`, `git mv` nach `work/archive/`) und Merge macht der Inhaber.
- Neue Pakete enthalten keine offenen Entscheidungen; die werden vorher mit dem Inhaber geklärt.

## Security Gate – vor jedem PR Pflicht (docs/SICHERHEIT.md Kap. 7)
1. `scripts/security-gate.sh` muss grün sein. Befunde beheben, nicht unterdrücken. Eine Unterdrückung (`#pragma`, `.editorconfig`, Semgrep-/zizmor-Ignore) nur mit Begründung im Code und im PR.
2. Bei Architektur- oder Schnittstellenänderungen: `docs/THREAT-MODEL.md` fortschreiben, die ASVS/MASVS-Punkte der PR-Checkliste prüfen und `/security-review` ausführen; das Ergebnis kommt in den PR.
3. Neue Sicherheitslogik bekommt Tests in `tests/FamilyHome.Server.Tests` (positiv **und** negativ).
