---
status: offen
branch:
bearbeiter:
abhaengig_von: []
sicherheitsrelevant: ja  # Pipeline (Workflows) und Supply Chain (Lock-Dateien, SDK)
---

# Lock-Dateien committen und .NET SDK für Android exakt festlegen

## Ziel
Die zwei unversionierten Lock-Dateien kommen ins Repo:
- `.devcontainer/devcontainer-lock.json` pinnt die DevContainer-Features;
- `src/FamilyHome.Client.Android/packages.lock.json` ermöglicht Locked Restore auch für Android.

Damit der Android-Build in der CI dadurch nicht bei jedem SDK-Patch bricht, wird die SDK-Version an einer Stelle (`global.json`) exakt festgelegt. DevContainer und Android-CI nutzen genau diese Version und prüfen das.

## Hintergrund und Entscheidungen
- Die Android-Lock-Datei enthält `Microsoft.NET.ILLink.Tasks` als implizite Abhängigkeit, die das SDK hinzufügt. Ihre Version ist die Runtime-Version, die mit dem SDK ausgeliefert wird (SDK `10.0.403` → `10.0.14`). Bei jedem anderen SDK schlägt der Locked Restore fehl (NU1004). Die anderen Projekte sind nicht betroffen.
- Bisher holen CI (`setup-dotnet` mit `rollForward: latestFeature`) und DevContainer (`--channel 10.0`) jeweils das neueste SDK. Diese Versionen laufen auseinander.
- **Entscheidung:**
  - `global.json` nennt die exakte Version `10.0.403` und ist die einzige Quelle.
  - `rollForward: latestFeature` bleibt, damit das Server-Image (`sdk:10.0`) und andere Rechner weiter bauen; für sie ist die Version ein Minimum.
  - Exakt erzwungen wird die Version nur dort, wo die Android-Lock-Datei entsteht oder geprüft wird: im DevContainer und in den Android-Jobs der CI.
  - In der CI wird das SDK isoliert installiert (`DOTNET_INSTALL_DIR`), damit kein vorinstalliertes, neueres SDK des Runners per Roll-Forward greift. Ein Prüfschritt bricht bei Abweichung ab.
- **SDK-Updates laufen künftig automatisiert an:**
  - Dependabot (`dotnet-sdk`) erhöht `global.json`.
  - Im selben PR werden die DevContainer-Version und die Android-Lock-Datei nachgezogen (Ablauf in SICHERHEIT.md 6.3).
  - Bis dahin ist der Android-Check rot. Das ist gewollt: Auto-Merge kann so nichts Inkonsistentes mergen.
  - Das Nachziehen voll zu automatisieren, ist ein eigenes späteres Arbeitspaket.
- Signierte Commits braucht es lokal nicht: Squash-Merges signiert GitHub. DCO (`-s`) ist Pflicht.

## Abgrenzung (gehört NICHT dazu)
- Kein Update von Paketen, Actions, Images oder des SDK selbst; die Version bleibt `10.0.403`.
- Keine Änderung am Job `build-test` in `ci.yml`, am Server-`Dockerfile` oder an `Directory.Build.props`.
- Kein Pinnen der Workload-Version.
- Keine Automatisierung des Lock-Nachziehens in Dependabot-PRs.
- Keine Änderung an anderen `packages.lock.json`.

## Erlaubte Dateien
- `.devcontainer/devcontainer-lock.json` (neu, Inhalt unverändert)
- `src/FamilyHome.Client.Android/packages.lock.json` (neu, Inhalt unverändert)
- `global.json`
- `.devcontainer/Dockerfile`
- `.github/workflows/ci.yml` (nur Job `android`)
- `.github/workflows/release-android.yml`
- `.github/workflows/devcontainer.yml`
- `.github/dependabot.yml`
- `docs/SICHERHEIT.md` (nur Kap. 6.3 und die Workflow-Tabelle)
- diese Paketdatei

## Vorbedingungen
- [ ] Die Arbeit läuft im DevContainer.
- [ ] `dotnet --version` gibt `10.0.403` aus. Bei einer anderen Ausgabe: **Abbruch**.
- [ ] `git status --short` zeigt auf `main` genau diese zwei Zeilen:
  ```
  ?? .devcontainer/devcontainer-lock.json
  ?? src/FamilyHome.Client.Android/packages.lock.json
  ```
  (Diese Paketdatei liegt dann schon auf `main`.)

## Beanspruchen
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/3399485c*"` und `gh pr list --state all --search "3399485c"` müssen leer sein, und `status` muss `offen` sein. Sonst **nicht anfangen**.
3. `git switch -c wp/3399485c-lockdateien-sdk-pin`. Die zwei unversionierten Dateien wandern dabei mit.
4. Im Frontmatter `status: in-arbeit`, `branch: wp/3399485c-lockdateien-sdk-pin` und `bearbeiter` setzen.
5. `git commit -s -m "WP 3399485c: beansprucht" work/wp-3399485c-lockdateien-sdk-pin.md && git push -u origin HEAD`

## Einzelschritte

### Schritt 1: Lock-Dateien prüfen
- Prüfen: `jq -r '.features | keys[]' .devcontainer/devcontainer-lock.json`
  → genau die 5 Features aus `.devcontainer/devcontainer.json`: `claude-code:1`, `desktop-lite:1`, `docker-outside-of-docker:1`, `github-cli:1`, `node:2`.
- Prüfen: `jq -r '.features[] | .resolved' .devcontainer/devcontainer-lock.json`
  → jede Zeile endet auf `@sha256:` und 64 Hex-Zeichen.
- Prüfen: `CI=true dotnet restore src/FamilyHome.Client.Android --locked-mode`
  → endet ohne Fehler und meldet `Restored …FamilyHome.Client.Android.csproj`.
- Prüfen: `jq -r '.dependencies[]["Microsoft.NET.ILLink.Tasks"].resolved' src/FamilyHome.Client.Android/packages.lock.json`
  → muss dieselbe Version zeigen wie `dotnet --list-runtimes | grep NETCore.App` (aktuell `10.0.14`).
- Prüfen: `dotnet restore FamilyHome.slnx`, danach `git status --short`
  → außer den zwei neuen Dateien keine Änderung, vor allem an keiner anderen `packages.lock.json`.
- Tun: `git add .devcontainer/devcontainer-lock.json src/FamilyHome.Client.Android/packages.lock.json`

### Schritt 2: `global.json`
- Tun: `"version": "10.0.100"` → `"version": "10.0.403"`. `rollForward` und der Abschnitt `test` bleiben unverändert.
- Prüfen: `jq -r .sdk.version global.json` → `10.0.403`. `dotnet --version` → weiterhin `10.0.403`.

### Schritt 3: DevContainer installiert exakt diese Version
In `.devcontainer/Dockerfile`:
- `ARG DOTNET_CHANNEL=10.0` ersetzen durch:
  ```dockerfile
  # Muss global.json entsprechen (Android-Lock-Datei, docs/SICHERHEIT.md 6.3); geprüft in devcontainer.yml
  ARG DOTNET_SDK_VERSION=10.0.403
  ```
- Im Abschnitt `# .NET SDK` `--channel ${DOTNET_CHANNEL}` ersetzen durch `--version ${DOTNET_SDK_VERSION}`.
- Prüfen: `grep -n DOTNET_ .devcontainer/Dockerfile`
  → `DOTNET_CHANNEL` kommt nicht mehr vor; `DOTNET_SDK_VERSION` steht genau 2-mal da (ARG und Verwendung).
- Den Container lokal neu bauen ist nicht nötig, weil er schon 10.0.403 hat. Das Image prüft der Workflow in Schritt 4.

### Schritt 4: `devcontainer.yml` prüft die Version
- Tun: bei `pull_request.paths` und `push.paths` jeweils `"global.json"` ergänzen.
- Tun: nach dem Schritt `Werkzeuge im Image prüfen` einen neuen Schritt anhängen:
  ```yaml
      - name: SDK-Version wie in global.json
        run: |
          expected="$(jq -r .sdk.version global.json)"
          actual="$(docker run --rm familyhome-devcontainer:ci dotnet --version)"
          echo "global.json: $expected, Image: $actual"
          test "$expected" = "$actual"
  ```

### Schritt 5: Android-Jobs nutzen exakt diese Version
In `.github/workflows/ci.yml` (nur Job `android`) und in `.github/workflows/release-android.yml` jeweils diesen Block:
```yaml
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          global-json-file: global.json
```
ersetzen durch:
```yaml
      - name: SDK-Version aus global.json
        id: sdk
        run: echo "version=$(jq -r .sdk.version global.json)" >> "$GITHUB_OUTPUT"

      # Exakt diese Version, getrennt von den vorinstallierten SDKs des Runners: Die Android-Lock-Datei
      # enthält Microsoft.NET.ILLink.Tasks in der Runtime-Version des SDK (docs/SICHERHEIT.md 6.3)
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        env:
          DOTNET_INSTALL_DIR: ${{ runner.temp }}/dotnet
        with:
          dotnet-version: ${{ steps.sdk.outputs.version }}

      - name: SDK-Version prüfen
        env:
          EXPECTED: ${{ steps.sdk.outputs.version }}
        run: |
          dotnet --version
          test "$(dotnet --version)" = "$EXPECTED"
```
Nur in `ci.yml` (Job `android`) zusätzlich vor `Build APK` einfügen, damit eine Lock-Abweichung eine klare Fehlermeldung gibt:
```yaml
      - name: Restore (Locked Mode)
        run: dotnet restore src/FamilyHome.Client.Android --locked-mode
```
- Prüfen: `grep -n "global-json-file" .github/workflows/*.yml`
  → nur noch die eine Stelle im Job `build-test` von `ci.yml`.
- Prüfen: `git diff .github/workflows/ci.yml`
  → im Job `build-test` keine Änderung.
- Der SHA von `setup-dotnet` bleibt exakt gleich. Keine weiteren Actions hinzufügen.

### Schritt 6: Dependabot für das SDK
In `.github/dependabot.yml` nach dem Eintrag `nuget` einfügen:
```yaml
  # Erhöht global.json. Im selben PR DevContainer-Version und Android-Lock-Datei nachziehen (docs/SICHERHEIT.md 6.3)
  - package-ecosystem: dotnet-sdk
    directory: /
    schedule:
      interval: weekly
```
- Prüfen: In der GitHub-Doku „Dependabot options reference“ → `package-ecosystem` muss `dotnet-sdk` als gültigen Wert aufführen. Wenn nicht: **Abbruch**.

### Schritt 7: Doku
In `docs/SICHERHEIT.md`:
- Kap. 6.3 bekommt diesen neuen Punkt:
  ```markdown
  - **.NET SDK exakt festgelegt:** `global.json` nennt die Version (einzige Quelle). DevContainer und Android-CI installieren genau diese und prüfen das, weil die Android-Lock-Datei `Microsoft.NET.ILLink.Tasks` in der Runtime-Version des SDK enthält. Ablauf bei einem SDK-Update (Dependabot-PR für `global.json`): `DOTNET_SDK_VERSION` in `.devcontainer/Dockerfile` angleichen, Container neu bauen, `dotnet restore src/FamilyHome.Client.Android --force-evaluate`, Lock-Datei im selben PR committen. Bis dahin ist der Android-Check rot, Auto-Merge greift also nicht.
  ```
- In der Workflow-Tabelle die Zeile `devcontainer.yml` ändern:
  - Auslöser: `Änderung an .devcontainer/ oder global.json, wöchentlich`;
  - Beschreibung ergänzen um `, SDK-Version = global.json`.

## Prüfroutinen (vor dem PR, alle müssen bestehen)
- [ ] `scripts/security-gate.sh` ist grün (darin u. a. actionlint, zizmor, hadolint, Locked Restore).
- [ ] `CI=true dotnet restore src/FamilyHome.Client.Android --locked-mode` ist erfolgreich.
- [ ] `dotnet build src/FamilyHome.Client.Android -c Debug` läuft ohne Warnungen.
- [ ] `git diff --name-only origin/main...HEAD` enthält nur erlaubte Dateien.
- [ ] `git log origin/main..HEAD --format=%B | grep -c "Signed-off-by"` ist mindestens die Zahl der Commits.

## Abnahmekriterien („erfolgreich“)
- [ ] Alle Prüfroutinen sind bestanden.
- [ ] Alle 10 Pflicht-Checks im PR sind grün.
- [ ] Im Log des Checks „Android-APK“ zeigt der Schritt „SDK-Version prüfen“ `10.0.403`, der Schritt „Restore (Locked Mode)“ ist grün.
- [ ] Der Workflow „DevContainer“ lief im PR (wegen `.devcontainer/**` bzw. `global.json`) und ist grün, inklusive „SDK-Version wie in global.json“.
- [ ] Der PR ist als sicherheitsrelevant gekennzeichnet (Pipeline, Supply Chain).

## Abbruchbedingungen
**Sofort aufhören**, `status: blockiert` setzen, Befund ins Protokoll schreiben und den Inhaber fragen, wenn:
- eine Prüfung in Schritt 1 ein anderes Ergebnis liefert, besonders eine geänderte andere Lock-Datei oder eine ILLink-Version ≠ Runtime-Version;
- `dotnet --version` im DevContainer nicht `10.0.403` ist;
- „SDK-Version prüfen“ in der CI eine andere Version zeigt. Das hieße: `DOTNET_INSTALL_DIR` wirkt nicht wie erwartet. Nicht durch andere Tricks umgehen;
- zizmor, actionlint, Semgrep oder hadolint etwas melden. Nicht per Ignore unterdrücken;
- `dotnet-sdk` kein gültiges Dependabot-Ökosystem ist;
- eine Datei außerhalb der erlaubten Liste geändert werden müsste;
- eine andere Action-Version oder ein neuer SHA nötig erscheint.

## Abschluss durch den Worker
1. Ergebnisprotokoll ausfüllen und `status: zur-abnahme` setzen.
2. `git commit -s`, Push, PR gegen `main`:
   - Titel: `Lock-Dateien committen, .NET SDK für Android exakt festlegen (WP 3399485c)`;
   - Im PR-Template „sicherheitsrelevant“ ankreuzen (Pipeline, Supply Chain) und kurz die Entscheidung aus „Hintergrund“ zusammenfassen;
   - PR-Text endet mit `🤖 Generated with [Claude Code](https://claude.com/claude-code)`, wenn ein Claude-Modell der Worker ist.
3. Nicht selbst mergen.

## Abnahme durch den Inhaber
Im PR-Branch `status: erfolgreich` setzen, `git mv work/wp-3399485c-lockdateien-sdk-pin.md work/archive/`, danach Squash-Merge.

## Ergebnisprotokoll (füllt der Worker aus)
- Datum/Bearbeiter:
- PR:
- Ausgaben der Prüfroutinen (gekürzt):
- Abweichungen/Auffälligkeiten:
