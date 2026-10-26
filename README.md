# FamilyHome

Familien-App für Android mit eigenem Server: Kalender, Todos und Einkaufslisten, People-Graph, Linkliste und „Wo ist was?“. Ein Messenger ist bewusst nicht enthalten.

- **Open Source** unter der [Apache License 2.0](LICENSE). Beiträge sind willkommen, siehe [CONTRIBUTING.md](CONTRIBUTING.md).
- **Selbst gehostet:** Jede Familie betreibt ihren eigenen Server aus dem offiziellen, signierten Docker-Image.
- **Kostenlos:** Die App gibt es im Play Store und über GitHub Releases. Wer das Projekt unterstützen möchte: Patreon (Link folgt).

- Spezifikation und Phasenplan: [docs/SPEZIFIKATION.md](docs/SPEZIFIKATION.md)
- Sicherheit, Updates und GitHub-Prüfroutinen: [docs/SICHERHEIT.md](docs/SICHERHEIT.md), Meldungen: [SECURITY.md](SECURITY.md)
- Hinweise für die Arbeit mit Claude Code: [CLAUDE.md](CLAUDE.md)

**Stand:** Phase 0, Grundgerüst. Server, Shared, Desktop-App und Tests bauen; das Security Gate ist eingerichtet. Das Android-Projekt baut nur im DevContainer, wo die Android-Toolchain installiert ist.

## Entwicklung im DevContainer

Voraussetzung: Docker und VS Code mit der Extension „Dev Containers“ (oder die `devcontainer`-CLI).

1. Ordner in VS Code öffnen und **„Reopen in Container“** wählen. Der erste Build dauert länger, weil das .NET-Android-Workload und das Android SDK installiert werden.
2. Der Container enthält:
   - .NET 10 SDK mit Android-Workload, JDK 17, Android SDK (API 36), `adb`, `dotnet-ef`
   - Claude Code (CLI und VS-Code-Extension); die Konfiguration liegt im Volume `claude-config` und bleibt erhalten
   - PostgreSQL 17 (Service `db`) und Radicale als CalDAV/CardDAV-Testserver (Service `radicale`, Port 5232)
   - einen leichten Desktop per noVNC auf <http://localhost:6080> (Passwort `vscode`) für die Avalonia-Desktop-Variante
3. Claude starten: im Terminal des Containers `claude` aufrufen.

### Auf dem Handy testen

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

## GitHub einrichten (einmalig)

1. Repository anlegen und in `.github/CODEOWNERS` und `deploy/.env` den Platzhalter `OWNER` ersetzen.
2. Repo **öffentlich** stellen. **Settings → Code security:** Secret Scanning + Push Protection, Dependabot Alerts/Security Updates und Private Vulnerability Reporting aktivieren.
3. **Rulesets für `main`:**
   - PR-Pflicht, Pflicht-Checks (CI, Security Lint, CodeQL, Dependency Review), signierte Commits, DCO;
   - kein Force-Push, CODEOWNERS-Review;
   - Auto-Merge erlauben (für Dependabot-Patch-Updates).
4. **Environment `release`:** dich als Pflicht-Reviewer eintragen, Deployment nur von Tags `v*`. Dort folgende Secrets anlegen:
   - `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASS`;
   - `UPDATE_MANIFEST_ED25519_PEM`, erzeugt mit `openssl genpkey -algorithm ed25519`. Der öffentliche Teil kommt in die App.

## Selbst hosten (Entwurf)

Voraussetzungen:
- ein Rechner mit Docker (amd64 oder arm64, also auch NAS oder Raspberry Pi);
- eine Domain bzw. DynDNS;
- Portfreigabe 443 und 80 auf diesen Rechner.

Ein VPN ist **nicht** nötig. Caddy holt automatisch ein Let's-Encrypt-Zertifikat und lässt nur Geräte mit gültigem Gerätezertifikat eurer Familie durch (mutual TLS). Ohne Zertifikat sind nur der Beitritt per Einladung und eine statische Datei für Passkeys erreichbar.

```bash
cd deploy && cp .env.example .env && ./update.sh
```

`update.sh` rollt das Server-Image nur aus, wenn die cosign-Signatur aus genau diesem Repository stammt. Nach dem Rollout prüft es den Healthcheck und rollt bei einem Fehlschlag automatisch zurück. Richtet es als täglichen systemd-Timer ein. Details stehen in [docs/SICHERHEIT.md](docs/SICHERHEIT.md), Kap. 3 und 5.2.
