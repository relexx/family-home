# FamilyHome

[![Lizenz: Apache 2.0](https://img.shields.io/badge/Lizenz-Apache_2.0-blue.svg)](LICENSE)
[![Status: in Entwicklung](https://img.shields.io/badge/Status-in_Entwicklung-orange.svg)](docs/SPEZIFIKATION.md#10-phasenplan)
[![Plattform: Android 13+](https://img.shields.io/badge/Android-13%2B-3DDC84.svg?logo=android&logoColor=white)](#2-die-app-installieren)
[![.NET 10](https://img.shields.io/badge/.NET-10_LTS-512BD4.svg?logo=dotnet&logoColor=white)](global.json)
[![Kein Tracking](https://img.shields.io/badge/Tracking-keins-success.svg)](docs/SICHERHEIT.md#4-daten)

[![CI](https://github.com/relexx/family-home/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/relexx/family-home/actions/workflows/ci.yml)
[![Security Lint](https://github.com/relexx/family-home/actions/workflows/security-lint.yml/badge.svg?branch=main)](https://github.com/relexx/family-home/actions/workflows/security-lint.yml)
[![CodeQL](https://github.com/relexx/family-home/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/relexx/family-home/actions/workflows/codeql.yml)
[![Server-Container](https://github.com/relexx/family-home/actions/workflows/container.yml/badge.svg?branch=main)](https://github.com/relexx/family-home/actions/workflows/container.yml)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/relexx/family-home/badge)](https://securityscorecards.dev/viewer/?uri=github.com/relexx/family-home)
[![Release](https://img.shields.io/github/v/release/relexx/family-home?include_prereleases&label=Release)](https://github.com/relexx/family-home/releases)

**Der gemeinsame Familienplaner für Android: Termine, Einkaufslisten, Freunde der Kinder und alles, was ihr im Alltag sucht, an einem Ort und nur bei euch.**

> 🚧 **FamilyHome ist noch in Entwicklung und noch nicht nutzbar.** Diese Seite beschreibt, wie es werden soll. Stellen mit *(folgt)* werden ergänzt, sobald es so weit ist.

<!-- TODO: Screenshot bzw. kurzes Vorschaubild der App einfügen -->
*(Screenshots folgen)*

---

## Was ist FamilyHome?

FamilyHome ist eine App, mit der eure ganze Familie den Alltag gemeinsam organisiert. Alle sehen dieselben Termine und Listen. Was einer einträgt, erscheint sofort auf den Handys der anderen.

Das Besondere: Eure Daten liegen **nicht bei einem fremden Unternehmen**, sondern auf einem kleinen Server **bei euch zu Hause**. Das kann eine NAS, ein Raspberry Pi oder ein alter PC sein. Man kann es sich vorstellen wie einen eigenen Briefkasten im Haus statt eines Postfachs bei einer fremden Firma.

- ✅ **Kostenlos** und ohne Abo
- ✅ **Keine Werbung, kein Tracking**: Niemand wertet aus, was eure Familie plant
- ✅ **Kein Chat**: FamilyHome ist ein Planer, kein weiterer Messenger
- ✅ **Funktioniert auch offline**, zum Beispiel die Einkaufsliste im Supermarkt-Funkloch
- ✅ **Offener Quellcode**: Jeder kann nachprüfen, was die App tut

## Was kann die App?

| | Funktion | Wofür? | Geplant ab |
|---|---|---|---|
| 📅 | **Familienkalender** | Termine aller Familienmitglieder mit eigener Farbe pro Person, Kategorien (Schule, Sport, Arzt …), Monats-, Wochen- und Tagesansicht, wiederkehrende Termine, Erinnerungen, Feiertage und Schulferien. Auf Wunsch mit Fahrdienst: Wer bringt, wer holt ab? | erste Version |
| 🛒 | **Listen & Einkauf** | Einkaufsliste sortiert nach Ladenreihenfolge, Packlisten für den Urlaub, Aufgaben. Wiederverwendbare Vorlagen („Skiurlaub“, „Kindergeburtstag“). Listen lassen sich mit Terminen verknüpfen | erste Version |
| 👫 | **Leute** | Wer ist mit wem befreundet? Die Freunde eurer Kinder und deren Eltern als übersichtliches Netz, mit Telefonnummer, Adresse und Allergien. Mit einem Fingerdruck anrufen oder schreiben | später |
| 🔗 | **Schnellzugriff** | Eure wichtigsten Apps und Webseiten an einem Ort (Schul-App, Kita-Portal, Wetter …) | später |
| 🏠 | **Startseite & Widgets** | „Was steht heute an?“ auf einen Blick, auch direkt auf dem Homescreen | später |
| 🔎 | **Wo ist was?** | Wo ist eigentlich der Staubsauger, die Weihnachtsdeko, das Zelt? Mit Foto und Ort ablegen und später einfach suchen | später |
| 🍝 | **Essensplan** | Wochenplan fürs Essen; die Zutaten landen mit einem Fingerdruck auf der Einkaufsliste | später |
| 🩹 | **Notfallkarte** | Kinderarzt, Notfallnummern, Impfungen und Kleidergrößen, besonders geschützt | später |
| 🎁 | **Geschenkideen** | Ideen pro Person sammeln, mit Erinnerung vor dem Geburtstag | später |

Eure vorhandenen Kalender und Kontakte, zum Beispiel aus Nextcloud oder einem anderen Dienst mit CalDAV/CardDAV, könnt ihr einbinden.

## So kommt FamilyHome zu euch

Es sind drei Schritte. Den ersten erledigt **einmalig eine Person**, meist die technikaffinste in der Familie.

### 1. Den Familien-Server einrichten (einmalig, ca. 30–60 Minuten)

**Ihr braucht:**
- ein Gerät, das dauerhaft läuft und auf dem Docker funktioniert, z. B. eine NAS (Synology, QNAP …), einen Raspberry Pi 4/5 oder einen alten PC;
- eine Internet-Adresse für euren Server, z. B. über einen kostenlosen DynDNS-Dienst oder eine eigene Domain;
- in eurem Router eine Freigabe für zwei Anschlüsse („Ports“ 443 und 80). Die Anleitung zeigt, wie das geht.

**Ihr braucht nicht:** ein VPN, einen Cloud-Vertrag oder Programmierkenntnisse.

Der Server holt sich danach **selbstständig Sicherheits-Updates** und prüft dabei, dass sie wirklich vom FamilyHome-Projekt stammen.

📖 Schritt-für-Schritt-Anleitung: *(folgt)*
<!-- TODO: docs/anleitung/server-einrichten.md mit Screenshots für Synology, Raspberry Pi und Linux-PC -->

### 2. Die App installieren

- **Google Play Store:** *(Link folgt)*
- **Direkt als Datei (APK)** von der [Release-Seite](../../releases) *(folgt)*. Diese Variante aktualisiert sich selbst.

Voraussetzung ist ein Android-Handy ab **Android 13**. Eine iPhone-Version ist derzeit nicht geplant.

### 3. Die Familie einladen

1. Die Person, die den Server eingerichtet hat, öffnet die App und meldet sich als **erstes Elternteil** an.
2. Unter **Mehr → Familie & Geräte** erzeugt ein Elternteil eine **Einladung** für ein Familienmitglied. Sie erscheint als QR-Code.
3. Das Familienmitglied installiert die App und **scannt den QR-Code**.
4. Zur Anmeldung wird ein **Passkey** angelegt. Das heißt: Entsperren mit Fingerabdruck, Gesicht oder Displaysperre wie beim Handy selbst. Es gibt **kein Passwort**, das man vergessen kann.
5. Ein Elternteil **bestätigt** das neue Gerät, und fertig.

**Kinder** bekommen eigene, eingeschränkte Zugänge: Sie sehen die Familientermine und können zum Beispiel Einkäufe abhaken, aber nichts Wichtiges löschen. Für jüngere Kinder ohne eigene Displaysperre können Eltern den Passkey abschalten.

<!-- TODO: Screenshots der Einladung (QR-Code) und der Bestätigung -->

## So benutzt ihr die App

<!-- TODO: Screenshots je Bereich -->

Unten in der App gibt es fünf Bereiche:

| Bereich | Was ihr dort macht |
|---|---|
| **Heute** | Alles für heute und morgen auf einen Blick: Termine, fällige Aufgaben, Geburtstage |
| **Kalender** | Termine ansehen und mit dem **+** neue anlegen. Oben wechselt ihr zwischen Monat, Woche und Tag. Über den Filter seht ihr z. B. nur die Termine eines Kindes |
| **Listen** | Einkaufsliste, Packlisten und Aufgaben. Einträge abhaken, neue Listen anlegen, aus einer Vorlage starten |
| **Leute** | Freunde, Eltern und wichtige Kontakte *(später)* |
| **Mehr** | Schnellzugriff, Notfallkarte, „Wo ist was?“, Familie & Geräte, Einstellungen |

**Ein paar Beispiele:**
- *„Fußball jeden Dienstag um 17 Uhr“*: Termin anlegen, „wiederholen: wöchentlich“ wählen, Kind und Kategorie „Sport“ zuordnen. Alle Eltern bekommen eine Erinnerung.
- *„Wir brauchen Milch“*: Eintrag in die Einkaufsliste. Wer gerade im Laden ist, sieht ihn sofort und hakt ihn ab.
- *„Sommerurlaub“*: Packliste aus der Vorlage „Urlaub“ erstellen und mit dem Urlaubstermin im Kalender verknüpfen.

📖 Ausführliche Bedienungsanleitung: *(folgt)*

## Häufige Fragen

**Kostet FamilyHome etwas?**
Nein. App und Server sind kostenlos. Ihr braucht nur ein Gerät für den Server; der Stromverbrauch einer NAS oder eines Raspberry Pi ist gering. Wer das Projekt unterstützen möchte, kann freiwillig spenden (siehe unten).

**Braucht man immer Internet?**
Nein. Die App funktioniert auch offline. Änderungen werden übertragen, sobald wieder eine Verbindung besteht.

**Wer kann unsere Daten sehen?**
Nur eure Familie. Die Daten liegen auf eurem eigenen Server. Nur Geräte, die ihr eingeladen und bestätigt habt, kommen überhaupt an den Server heran. Es gibt keine Werbung, kein Tracking und keine Weitergabe an Dritte.

**Was ist, wenn ein Handy verloren geht?**
Ein Elternteil sperrt das Gerät unter **Mehr → Familie & Geräte** mit einem Fingerdruck. Ab dann hat es keinen Zugriff mehr. Die Daten auf dem Handy selbst sind verschlüsselt.

**Was passiert, wenn der Server kaputtgeht?**
Der Server legt automatisch tägliche Sicherungen an. Wie ihr eine Kopie außer Haus ablegt und wiederherstellt, beschreibt die Anleitung *(folgt)*.

**Können wir unseren bisherigen Kalender weiter nutzen?**
Ja, wenn er CalDAV unterstützt (z. B. Nextcloud, mailbox.org, Posteo). Er wird in beide Richtungen abgeglichen. Für Google-Kalender ist das noch offen.

**Gibt es FamilyHome fürs iPhone?**
Derzeit nicht. Die App ist für Android ab Version 13 gemacht.

**Ich habe keinen Server und kenne mich damit nicht aus. Was nun?**
Fragt jemanden im Bekanntenkreis, der eine NAS oder einen Raspberry Pi betreibt: Die Einrichtung ist einmalig, danach läuft alles von selbst. Ein fertig gehostetes Angebot ist für die erste Version nicht geplant.

## Mitmachen & Unterstützen

- ❤️ **Spenden:** Patreon *(Link folgt)*
- 🐞 **Fehler melden oder Ideen einbringen:** über die [Issues](../../issues)
- 🔒 **Sicherheitslücke gefunden?** Bitte nicht öffentlich melden, sondern wie in [SECURITY.md](SECURITY.md) beschrieben
- 👩‍💻 **Mitentwickeln:** siehe [CONTRIBUTING.md](CONTRIBUTING.md)

---

## Für Entwickler & Technikinteressierte

- **Open Source** unter der [Apache License 2.0](LICENSE).
- **Selbst gehostet:** Jede Familie betreibt ihren eigenen Server aus dem offiziellen, signierten Docker-Image (eine Familie pro Server).
- **Verteilung:** Google Play und GitHub Releases.
- Spezifikation und Phasenplan: [docs/SPEZIFIKATION.md](docs/SPEZIFIKATION.md)
- Sicherheit, Updates und GitHub-Prüfroutinen: [docs/SICHERHEIT.md](docs/SICHERHEIT.md), Meldungen: [SECURITY.md](SECURITY.md)
- Hinweise für die Arbeit mit Claude Code: [CLAUDE.md](CLAUDE.md)

**Stand:** Phase 0, Grundgerüst. Server, Shared, Desktop-App und Tests bauen; das Security Gate ist eingerichtet. Das Android-Projekt baut nur im DevContainer, wo die Android-Toolchain installiert ist.

### Entwicklung im DevContainer

Voraussetzung: Docker und VS Code mit der Extension „Dev Containers“ (oder die `devcontainer`-CLI).

1. Ordner in VS Code öffnen und **„Reopen in Container“** wählen. Der erste Build dauert länger, weil das .NET-Android-Workload und das Android SDK installiert werden.
2. Der Container enthält:
   - .NET 10 SDK mit Android-Workload, JDK 17, Android SDK (API 36), `adb`, `dotnet-ef`
   - Claude Code (CLI und VS-Code-Extension); die Konfiguration liegt im Volume `claude-config` und bleibt erhalten
   - PostgreSQL 17 (Service `db`) und Radicale als CalDAV/CardDAV-Testserver (Service `radicale`, Port 5232)
   - einen leichten Desktop per noVNC auf <http://localhost:6080> (Passwort `vscode`) für die Avalonia-Desktop-Variante
3. Claude starten: im Terminal des Containers `claude` aufrufen.

#### Auf dem Handy testen

Im Container läuft kein Android-Emulator, weil dafür KVM nötig wäre. Es gibt zwei Wege:

**A) WLAN-Debugging, direkt aus dem Container** (Android 11+): *Entwickleroptionen → Kabelloses Debugging → Gerät mit Kopplungscode koppeln*. Danach im Container:

```bash
adb pair <ip>:<pair-port>
```

```bash
adb connect <ip>:<port>
```

```bash
dotnet build src/FamilyHome.Client.Android -t:Install
```

**B) Host-adb nutzen** (USB-Gerät oder Emulator auf dem Host): Auf dem Host läuft `adb -a nodaemon server start`. Im Container wird `ADB_SERVER_SOCKET=tcp:host.docker.internal:5037` gesetzt.

Der Server muss vom Handy aus erreichbar sein. In der Entwicklung bedeutet das: die IP des Rechners und Port 5080, die von VS Code weitergeleitet werden.

### GitHub einrichten (einmalig)

1. Repository: <https://github.com/relexx/family-home> (Owner `relexx` ist in `.github/CODEOWNERS`, `deploy/.env.example` und den Badges eingetragen).
2. Repo **öffentlich** stellen. **Settings → Code security:** Secret Scanning + Push Protection, Dependabot Alerts/Security Updates und Private Vulnerability Reporting aktivieren.
3. **Rulesets für `main`:**
   - PR-Pflicht, Pflicht-Checks (CI, Security Lint, CodeQL, Dependency Review), signierte Commits, DCO;
   - kein Force-Push, CODEOWNERS-Review;
   - Auto-Merge erlauben (für Dependabot-Patch-Updates).
4. **Environment `release`:** dich als Pflicht-Reviewer eintragen, Deployment nur von Tags `v*`. Dort folgende Secrets anlegen:
   - `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASS`;
   - `UPDATE_MANIFEST_ED25519_PEM`, erzeugt mit `openssl genpkey -algorithm ed25519`. Der öffentliche Teil kommt in die App.

### Selbst hosten (Entwurf)

Voraussetzungen:
- ein Rechner mit Docker (amd64 oder arm64, also auch NAS oder Raspberry Pi);
- eine Domain bzw. DynDNS;
- Portfreigabe 443 und 80 auf diesen Rechner.

Ein VPN ist **nicht** nötig. Caddy holt automatisch ein Let's-Encrypt-Zertifikat und lässt nur Geräte mit gültigem Gerätezertifikat eurer Familie durch (mutual TLS). Ohne Zertifikat sind nur der Beitritt per Einladung und eine statische Datei für Passkeys erreichbar.

```bash
cd deploy && cp .env.example .env && ./update.sh
```

`update.sh` rollt das Server-Image nur aus, wenn die cosign-Signatur aus genau diesem Repository stammt. Nach dem Rollout prüft es den Healthcheck und rollt bei einem Fehlschlag automatisch zurück. Richtet es als täglichen systemd-Timer ein. Details stehen in [docs/SICHERHEIT.md](docs/SICHERHEIT.md), Kap. 3 und 5.2.
