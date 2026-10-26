# FamilyHome – Sicherheit, Aktualität & Prüfroutinen

Stand: 2026-10-26 · Status: **Entwurf zur Review** · ergänzt [SPEZIFIKATION.md](SPEZIFIKATION.md) (Kap. 5)

Leitlinie: **Zero Trust.** Kein Netz, kein Gerät und keine Komponente gilt als vertrauenswürdig, nur weil sie „drinnen“ ist. Jede Anfrage wird mit Gerät, Person und Recht geprüft. Alles wird automatisch aktuell gehalten, und jede Änderung durchläuft das **Security Gate** (Kap. 7), bevor sie auf Server oder Geräte gelangt.

**Betriebsmodell:** FamilyHome ist **Open Source (Apache-2.0)**. Jede Familie betreibt **ihren eigenen Server** (eine Familie pro Server) aus dem offiziellen, signierten Docker-Image. Die App ist kostenlos (Play Store, GitHub Releases), freiwillig unterstützt per Patreon. Ein zentrales Hosting gibt es in Version 1 nicht.

Das hat zwei Folgen für die Sicherheit:
1. Die Server laufen **bei Fremden, in unbekannten Netzen und in unterschiedlichen Versionen**. Sichere Voreinstellungen, die sich nicht abschalten lassen, und Selbst-Updates sind deshalb wichtiger als Konfigurierbarkeit.
2. Der Code ist öffentlich. Sicherheit darf **nie** auf Geheimhaltung des Codes beruhen, sondern nur auf Schlüsseln.

---

## 1. Bedrohungsmodell (kurz)

| Was schützen wir? | Gegen wen/was? |
|---|---|
| Kinderdaten (Termine, Orte, Kontakte, Allergien, Notfallkarte), DAV-Zugangsdaten | Angreifer aus dem Internet (der Server ist öffentlich erreichbar), verlorene oder gestohlene Handys, kompromittierte Abhängigkeiten (Supply Chain), Fehlkonfiguration durch Selbst-Hoster, neugierige Kinder (Rechte), Fehler im eigenen Code |
| Integrität von App und Server-Image | Gefälschte APKs oder Images, gekaperter Build, manipulierte Update-Quelle |
| Verfügbarkeit / Daten | Ausfall, Ransomware, versehentliches Löschen |

**Bewusst nicht im Scope:** Ende-zu-Ende-Verschlüsselung gegenüber dem eigenen Server. Der Server muss die Daten lesen können, um mit CalDAV zu synchronisieren. Er gehört der Familie selbst. Besonders sensible Felder werden zusätzlich auf Feldebene verschlüsselt (Kap. 4).

Das Bedrohungsmodell wird bei **jeder Architekturänderung** fortgeschrieben (STRIDE-Tabelle in `docs/THREAT-MODEL.md`, Kap. 7.1).

---

## 2. Identität & Zugriff (Zero Trust)

### 2.1 Gerätezertifikate statt Tokens
- **Familien-CA:** Jeder Server erzeugt bei der Ersteinrichtung eine **eigene Zertifizierungsstelle** (ECDSA P-256, nur für Geräte-Zertifikate, `pathLen=0`). Der private CA-Schlüssel liegt verschlüsselt (Data Protection) auf dem Server und verlässt ihn nie.
- **Beitritt:**
  - Das Gerät erzeugt ein Schlüsselpaar im **Android Keystore** (hardwaregestützt, StrongBox wenn vorhanden, nicht exportierbar) und sendet nur eine Zertifikatsanfrage (CSR).
  - Der Server stellt ein **kurzlebiges Gerätezertifikat** aus (7 Tage). Die App erneuert es automatisch über die bestehende, bereits authentifizierte Verbindung.
- **Jede Verbindung ist mutual TLS:** Der Reverse Proxy (Caddy) prüft das Gerätezertifikat schon beim Verbindungsaufbau. Ohne gültiges Zertifikat werden alle Anfragen **vom Proxy selbst** abgewiesen und erreichen den Server-Code nie (Ausnahmen: Kap. 3.1).
- **Keine Bearer-Tokens:** Die Identität ist das Zertifikat. Es gibt kein Access- oder Refresh-Token, das man abgreifen oder wiederverwenden könnte. SignalR nutzt dieselbe mTLS-Verbindung.
- **Kontinuierliche Prüfung im Server:**
  - Bei jeder Anfrage prüft der Server das weitergereichte Zertifikat erneut gegen die Familien-CA.
  - Er prüft außerdem den Fingerabdruck gegen die Gerätetabelle: Gerät aktiv? Account aktiv? Rolle aktuell?
  - **Sperren wirkt sofort.** Die SignalR-Verbindung wird getrennt, und die nächste Anfrage scheitert.
- **Key Attestation:** Beim Beitritt prüft der Server die Android-Attestierungskette. Damit ist belegt, dass der Schlüssel in Hardware liegt. Ergebnis und Sicherheitsstufe werden pro Gerät angezeigt. Eltern können „nur hardwaregestützte Geräte“ erzwingen.
- **Fallback, falls ein Spike in Phase 0 zeigt, dass mTLS mit Keystore-Schlüsseln unter .NET Android nicht zuverlässig funktioniert:** signierte Anfragen nach dem DPoP-Prinzip (RFC 9449) mit demselben Keystore-Schlüssel. Technischer Hinweis: Der Weg führt über `AndroidMessageHandler` mit eigener `SSLSocketFactory`/`KeyManager`. SignalR läuft über Server-Sent Events statt WebSockets, weil der managed WebSocket-Stack keine Keystore-Schlüssel verwenden kann.

### 2.2 Personen-Authentifizierung: Passkeys
- **Alle Accounts, Eltern wie Kinder, melden sich mit einem Passkey an** (FIDO2 über Android Credential Manager, Fido2NetLib auf dem Server).
- **Ausnahme Kinder:** Eltern können den Passkey pro Kind **deaktivieren** (z. B. für jüngere Kinder ohne Displaysperre). Dann bindet allein die von Eltern bestätigte Einladung das Gerät. Ein neues Gerät braucht dann immer eine neue Einladung, optional mit einer App-PIN.
- **Wofür der Passkey gebraucht wird:**
  1. beim Beitritt bzw. bei der Anmeldung eines neuen Geräts;
  2. als **Step-up** vor kritischen Aktionen: Geräte hinzufügen oder entfernen, Rollen ändern, DAV-Zugangsdaten ansehen oder ändern, Notfallkarte öffnen, Export, Passkey-Pflicht eines Kindes abschalten;
  3. als **App-Sperre**, optional und konfigurierbar (z. B. nach 7 Tagen Inaktivität).
- **Passkeys bei Selbst-Hosting:** Die Relying-Party-ID ist die Domain des Familien-Servers. Der Server liefert dazu automatisch `/.well-known/assetlinks.json` mit den Signatur-Fingerabdrücken der offiziellen App-Builds (Play- und GitHub-Signatur).
- **Kein Passwort-Login.** Wiederherstellung, wenn alle Passkeys verloren sind: ein **Wiederherstellungscode**, der bei der Ersteinrichtung einmalig angezeigt wird und ausgedruckt werden soll. Alternativ legt ein zweites Elternteil einen neuen Passkey an.
- **Einladungen:**
  - einmalig nutzbar, 48 h gültig, als Hash gespeichert;
  - der Beitritt wird erst wirksam, wenn ein Elternteil ihn bestätigt (Vier-Augen-Prinzip);
  - der QR-Code enthält den **SPKI-Fingerabdruck des Server-TLS-Schlüssels** und den Fingerabdruck der Familien-CA, damit die App von Anfang an nur mit dem richtigen Server spricht.

### 2.3 Autorisierung
- **Prüfung pro Objekt auf dem Server** mit `PermissionRules`, deny by default. Was die UI ausblendet, ist keine Sicherheitsmaßnahme.
- **Ein automatischer Test über alle registrierten Endpunkte** stellt sicher, dass jeder Endpunkt Autorisierung verlangt. Ausnahmen stehen auf einer kleinen, explizit gepflegten Liste (Kap. 7.2).
- **Getrennte DB-Rollen:** `familyhome_migrator` (DDL, nur beim Update) und `familyhome_app` (nur DML auf die App-Tabellen, kein Owner). Es gibt eine Familie pro Server, deshalb ist keine Mandantentrennung nötig.
- **Audit-Log** (nur anhängen): Beitritte, Geräte, Rollen, Rechteverweigerungen, Step-ups, Quellen-Änderungen und Exporte. Eltern sehen es in der App und werden bei kritischen Ereignissen benachrichtigt.

---

## 3. Transport & Netzwerk (ohne VPN)

### 3.1 Öffentlich erreichbar, aber nur mit Gerätezertifikat
```
Internet ──443──► Caddy (Let's Encrypt, TLS 1.3, client_auth: Familien-CA)
                    ├─ /.well-known/assetlinks.json  → öffentlich (für Passkeys)
                    ├─ /api/v1/join                  → öffentlich, nur wenn eine Einladung offen ist
                    ├─ alles mit gültigem Gerätezertifikat → Server (Zertifikat im Header weitergereicht)
                    └─ alles andere                  → Verbindung wird vom Proxy beendet
                  Server ── nur im internen Docker-Netz, kein eigener Port
                  PostgreSQL ── internes Netz ohne Internetzugang
```
- **Für Nutzer unsichtbar:** kein VPN und keine App eines Drittanbieters nötig.
- **Angriffsfläche ohne Gerätezertifikat:**
  - eine statische Datei;
  - der Join-Endpunkt. Er antwortet nur, solange ein Elternteil eine Einladung geöffnet hat (sonst 404), ist stark rate-limitiert und verlangt einen Einmal-Code.
- **Header-Fälschung ausgeschlossen:** Der Server akzeptiert den Zertifikat-Header nur aus dem internen Proxy-Netz. Caddy überschreibt bzw. entfernt den Header bei jeder Anfrage.
- **VPN bleibt optional** für besonders Vorsichtige (Tailscale/Headscale oder WireGuard vor dem Proxy).
- **Selbst-Hosting-Anforderungen:** eine Domain bzw. DynDNS und Portfreigabe 443 (für ACME zusätzlich 80, oder DNS-Challenge). Wer keine Portfreigabe will, nutzt das VPN.

### 3.2 TLS
- TLS 1.3 am Proxy. Proxy → Server läuft im isolierten Docker-Netz. Server → PostgreSQL ist intern, optional mit `sslmode=verify-full`.
- **App:**
  - `network_security_config` ohne Klartext und nur mit System-CAs für den Server; vom Nutzer installierte CAs werden ignoriert. Ausnahme sind Debug-Builds für localhost.
  - **Pinning beim Beitritt (TOFU aus dem QR-Code):** auf den SPKI des Server-Schlüssels plus einen Ersatz-Pin. Caddy behält den Schlüssel bei Zertifikatsverlängerungen (`reuse_private_keys`). Einen geplanten Schlüsselwechsel kündigt der Server vorab an; diese Nachricht ist mit dem alten Schlüssel signiert.
- HSTS, sichere Header (CSP `default-src 'none'`, `nosniff`, `no-referrer`, `frame-ancestors 'none'`), kein `Server`-Header, keine Versionsangaben in Antworten ohne Authentifizierung.

---

## 4. Daten

| Ort | Schutz |
|---|---|
| Gerät | Lokale DB mit **SQLCipher** verschlüsselt (ab Phase 1), der Schlüssel liegt im Android Keystore. `allowBackup=false` und `dataExtractionRules` schließen Cloud-Backups aus. Optional eine App-Sperre per Passkey bzw. Biometrie. `FLAG_SECURE` auf sensiblen Screens (Notfallkarte, Einladungs-QR, Wiederherstellungscode) |
| Server-DB | Feldverschlüsselung für Allergien, Notfallkarte und DAV-Passwörter (Data Protection; der Schlüsselring liegt außerhalb der DB in einem eigenen Volume). Empfehlung an Selbst-Hoster: Datenträgerverschlüsselung (LUKS) |
| Familien-CA | Privater Schlüssel mit Data Protection verschlüsselt. Nur das öffentliche CA-Zertifikat wird für Caddy exportiert |
| Backups | Täglich `pg_dump` plus Data-Protection-Schlüssel, mit **restic** verschlüsselt, Ziel konfigurierbar. Wöchentlich wird automatisch ein Restore getestet |
| Logs | Keine personenbezogenen Inhalte (nur IDs), 30 Tage Aufbewahrung |
| Telemetrie | Keine. **Keine Werbe-, Tracking- oder Analyse-SDKs**, auch keine Absturz-Telemetrie von Drittanbietern |

---

## 5. Aktualität – alles aktualisiert sich selbst

### 5.1 App-Verteilung & Updates

| Kanal | Updates | Besonderheiten |
|---|---|---|
| **Google Play** | über Play; erzwungene Updates per Play In-App-Updates-API | Play verbietet Selbst-Updates außerhalb von Play. Diese Variante hat deshalb kein `REQUEST_INSTALL_PACKAGES` und keinen eigenen Updater. `USE_EXACT_ALARM` wird in der Play Console als Kalender-Kernfunktion deklariert |
| **GitHub Releases** (direkt) | **eigener Selbst-Updater** | Update-Manifest mit **Ed25519-Signatur** (öffentlicher Schlüssel in der App) plus SHA-256 der APK. Android prüft zusätzlich dasselbe Signaturzertifikat. Installation per `PackageInstaller`-Session; nach dem ersten bestätigten Update still (`USER_ACTION_NOT_REQUIRED`, Android 12+) |
| **F-Droid** (später) | über F-Droid | Setzt reproduzierbare Builds voraus. Ob F-Droid den .NET-Android-Build unterstützt, ist noch zu klären |

- Beide Varianten entstehen aus **einem Code** über die Build-Eigenschaft `-p:Distribution=Play|Direct`. Sie unterscheiden sich nur bei Manifest-Berechtigungen und Updater.
- **Mindestversionen in beide Richtungen:**
  - Der Server meldet `minAppVersion`. Ältere Apps können nur noch aktualisieren.
  - Die App meldet `minServerVersion`. Ist der Server zu alt, zeigt die App den Eltern einen deutlichen Hinweis „Server aktualisieren“ mit Anleitung.
  - Die API ist versioniert (`/api/v1/…`), und der Server meldet seine Fähigkeiten (Capabilities). Neue Apps müssen mit Servern der letzten **6 Monate** funktionieren.
- **Spenden:** In der GitHub-Variante gibt es einen Patreon-Link unter „Über“. Für die Play-Variante ist **noch zu prüfen**, ob ein externer Spendenlink mit der Play-Zahlungsrichtlinie vereinbar ist. Bis dahin steht der Link dort nur auf der Projektwebsite bzw. im README.

### 5.2 Server-Updates bei Selbst-Hostern
- **Offizielles Image:**
  - `ghcr.io/<org>/familyhome-server`, **Multi-Arch** (amd64 und arm64 für NAS/Raspberry Pi);
  - chiseled (ohne Shell und Paketmanager), Nicht-Root, Read-only-Root-Dateisystem;
  - **wöchentlich neu gebaut** (Basis-Image-Patches);
  - **cosign-signiert** (keyless, gebunden an den Release-Workflow dieses Repos), mit **SBOM + SLSA-Provenance**.
- **`deploy/update.sh`** (täglicher systemd-Timer, in der Installationsanleitung standardmäßig aktiviert):
  1. neuesten Digest holen;
  2. **cosign-Signatur gegen genau diesen Workflow prüfen**;
  3. Backup;
  4. Migration;
  5. Rollout und Healthcheck;
  6. bei Fehlschlag automatischer Rollback.
- **Sicherheitsfixes:** Releases mit dem Label `security` werden in der App den Eltern angezeigt („Dein Server hat eine bekannte Lücke, Update verfügbar“). Die Grundlage dafür ist ein öffentliches, signiertes Advisory-Manifest im Repo.
- **PostgreSQL:** Minor-Updates automatisch (Digest-PRs im Repo, `update.sh` zieht sie). Major-Updates nur mit Release-Notiz und Upgrade-Skript.
- **Host-OS:** Die Installationsanleitung richtet `unattended-upgrades` ein.

### 5.3 Versionspolitik
- **.NET LTS** (aktuell .NET 10, Support bis Nov. 2028). Der Wechsel auf die nächste LTS erfolgt spätestens 3 Monate nach Erscheinen.
- **Android `targetSdk`** wird jährlich auf die neueste API angehoben; das verlangt auch Google Play.
- **Abhängigkeiten:** Dependabot, Patch-Updates nach grüner CI automatisch; Minor- und Major-Updates mit Review.

---

## 6. GitHub-Prüfroutinen (CI/CD)

### 6.1 Repository-Einstellungen
- **Öffentliches Repo** (Apache-2.0). Beiträge mit **DCO** (`Signed-off-by`) statt CLA.
- **Rulesets auf `main`:**
  - PR-Pflicht, alle Pflicht-Checks aus 6.2, signierte Commits, linearer Verlauf;
  - kein Force-Push;
  - CODEOWNERS-Review für Pipeline, Deployment und Sicherheitscode.
- **Secret Scanning + Push Protection**, **Dependabot Alerts + Security Updates**, **Private Vulnerability Reporting** (`SECURITY.md`), **OpenSSF Scorecard** öffentlich.
- **Actions-Härtung:**
  - Actions auf Commit-SHA gepinnt;
  - `permissions: {}` als Standard;
  - `persist-credentials: false`;
  - Harden-Runner (Egress-Audit, ab Phase 2 Allow-List);
  - Prüfung der Workflows mit **zizmor** und **actionlint**.
- **Release-Secrets** (APK-Keystore, Manifest-Signaturschlüssel) liegen nur im GitHub-Environment `release`, mit Pflicht-Freigabe und beschränkt auf Tags `v*`. Für Play wird **Play App Signing** genutzt; der Upload-Schlüssel liegt im Environment.

### 6.2 Workflows

| Workflow | Auslöser | Inhalt |
|---|---|---|
| `ci.yml` | PR, Push | Restore im Locked Mode, Build mit **Analyzern als Fehler** (Kap. 7.2), Format, Tests inkl. Sicherheitstests, NuGet-Audit, Android-APK |
| `security-lint.yml` | PR, Push, wöchentlich | **gitleaks** (Secrets, gesamter Verlauf), **Semgrep** (C#-, Secrets-, Dockerfile- und Actions-Regeln), **zizmor** + **actionlint** (Workflows), **hadolint** (Dockerfiles), **ShellCheck** (Skripte); SARIF ins Security-Tab |
| `codeql.yml` | PR, Push, wöchentlich | CodeQL für C# und Actions (`security-extended`) |
| `dependency-review.yml` | PR | Blockiert Abhängigkeiten mit Schwachstellen ab „moderate“ sowie inkompatible Lizenzen (GPL/AGPL; Apache-2.0-kompatibel bleiben) |
| `container.yml` | Push `main`, wöchentlich | Multi-Arch-Image, **Grype-Scan** (bricht bei High/Critical ab), GHCR, cosign, SBOM, Provenance |
| `release-android.yml` | Tag `v*` | Signierte APKs (Play/Direct), signiertes Update-Manifest, Provenance, GitHub Release |
| `scorecard.yml` | wöchentlich | OpenSSF Scorecard |
| Dependabot | täglich/wöchentlich | NuGet, Actions, Docker, Compose, DevContainer |

**Ab Phase 1 zusätzlich:**
- **OWASP ZAP API-Scan** (DAST) gegen den Server im CI, auf Basis des automatisch erzeugten OpenAPI-Dokuments.
- **MobSF**-Statikanalyse der APK, gemessen an OWASP MASVS.
- **Fuzzing** der iCal-/vCard-Parser (SharpFuzz) im wöchentlichen Lauf.

### 6.3 Supply Chain im Build
- `nuget.config` mit Package Source Mapping (nur nuget.org), Lock-Dateien und Locked Mode in der CI, Central Package Management.
- NuGet-Audit (`all`, Fehler ab „moderate“).
- Images per Digest gepinnt.
- Neue Abhängigkeiten nur mit Begründung im PR. Analyzer werden als `PrivateAssets=all` eingebunden.

---

## 7. Security Gate – Pflicht bei jeder Änderung

Jede Code- oder Architekturänderung muss dieses Gate durchlaufen, **lokal vor dem PR** (`scripts/security-gate.sh`) und **identisch in der CI** (Pflicht-Checks). Erst wenn alles grün ist, darf gemergt werden.

### 7.1 Bei Architektur- oder Schnittstellenänderungen zusätzlich (manuell, im PR dokumentiert)
1. **Bedrohungsmodell fortschreiben:** In `docs/THREAT-MODEL.md` die neuen Datenflüsse, Vertrauensgrenzen und STRIDE-Risiken samt Gegenmaßnahmen eintragen.
2. **Abgleich mit Standards:** Server/API gegen **OWASP ASVS 5.0 Level 2**, App gegen **OWASP MASVS L2** (Checkliste im PR-Template).
3. **Datenschutz:** Neue personenbezogene Felder? Klassifizieren (normal/sensibel), Verschlüsselung und Sichtbarkeit (`PermissionRules`) festlegen, Löschkonzept beschreiben.
4. **Review durch CODEOWNER.** Wer mit Claude arbeitet, lässt zusätzlich `/security-review` auf den Branch laufen und fügt das Ergebnis dem PR hinzu.

### 7.2 Automatisch bei jeder Änderung

| Prüfung | Werkzeug | Bricht ab bei |
|---|---|---|
| Statische Codeanalyse | .NET-Analyzer (`AnalysisLevel latest`, **Kategorie Security = Fehler**: SQL-Injection, unsichere Deserialisierung, schwache Krypto, Zertifikatsprüfung, XML/XXE …), **Meziantou.Analyzer**, Nullable-Warnungen | jeder Warnung (CI baut mit `-warnaserror`) |
| Verbotene APIs | **BannedApiAnalyzers** mit `BannedSymbols.txt`: `BinaryFormatter`, MD5/SHA1, `System.Random` für Sicherheitszwecke, `FromSqlRaw`/`ExecuteSqlRaw`, unsichere `ServerCertificateCustomValidationCallback`, `new HttpClient()` im Server, `DateTime.Now` (stattdessen `TimeProvider`) … | Nutzung |
| Semantische Sicherheitsanalyse | CodeQL `security-extended`, Semgrep | neuem Befund ab „warning“ |
| Secrets | gitleaks (Commit + Verlauf), GitHub Push Protection | jedem Fund |
| Abhängigkeiten | NuGet-Audit, Dependency Review, Lizenzprüfung, Lock-Datei-Konsistenz | Schwachstelle ab „moderate“ bzw. Lizenzverstoß |
| Container & IaC | hadolint, Grype (Image), ShellCheck | Fehler bzw. High/Critical |
| Pipelines | zizmor, actionlint | jedem Befund |
| Sicherheitstests | `FamilyHome.Server.Tests`: **Endpunkt-Inventar** (jeder Endpunkt verlangt Autorisierung außer der Allowlist), Rechte-Matrix aus Kap. 5.2 der Spezifikation, Zertifikatsprüfung (fremde CA, abgelaufen, gesperrt), Header-Fälschung | Fehlschlag |
| Format/Stil | `dotnet format --verify-no-changes`, `.editorconfig` | Abweichung |

---

## 8. Entwicklungsumgebung & KI-Agent

- Der DevContainer läuft ohne Zugriff auf Host-Geheimnisse und ohne SSH-Agent-Weiterleitung.
- **Egress-Firewall** für den DevContainer (Phase 0), nach dem Vorbild der Referenz-DevContainer-Konfiguration von Anthropic. Ausgehend erlaubt sind nur nuget.org, `dl.google.com`/Google Maven, GitHub/GHCR, die Anthropic-API, npm/PyPI und die .NET-Download-Server.
- Claude arbeitet auf Feature-Branches, führt das Security Gate aus und öffnet PRs. Merges nach `main` laufen nur über die Pflicht-Checks und deine Freigabe.
- Release-Signaturschlüssel sind **nie** im DevContainer.

---

## 9. Umsetzung nach Phasen

| Phase | Sicherheit & Aktualität |
|---|---|
| **0** | Security Gate komplett (Analyzer, Banned APIs, Linter-Workflow, Endpunkt-Inventar-Test), Rulesets, Familien-CA + Gerätezertifikate + mTLS über Caddy (inkl. Spike mTLS unter .NET Android), Passkeys (Eltern und Kinder), Audit-Log, getrennte DB-Rollen, Multi-Arch-Image, DevContainer-Firewall, `THREAT-MODEL.md` |
| **1** | SQLCipher, Pinning aus dem QR-Code, Step-up, Selbst-Updater (Direct) bzw. Play In-App Updates, Versions-Aushandlung App↔Server, `update.sh` mit Backup/Migration/Rollback, restic-Backups, ZAP-, MobSF- und Fuzzing-Läufe |
| **2** | Key Attestation erzwingbar, Harden-Runner-Allow-List, Advisory-Manifest + Hinweis in der App, F-Droid-Prüfung |
| **laufend** | wöchentliche Rebuilds, Dependabot, jährlicher Wechsel von `targetSdk` und PostgreSQL-Major-Version, LTS-Wechsel, Bedrohungsmodell pflegen |
