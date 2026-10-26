---
status: open
branch:
assignee:
depends_on: [533c3095]   # the Security Gate must work inside the DevContainer first
security_relevant: true  # pipeline (workflows) and supply chain (lock files, SDK)
---

# Commit the lock files and pin the .NET SDK exactly for Android

## Goal
The two untracked lock files go into the repo:
- `.devcontainer/devcontainer-lock.json` pins the DevContainer features;
- `src/FamilyHome.Client.Android/packages.lock.json` enables locked restore for Android as well.

To keep the Android build in CI from breaking on every SDK patch as a result, the SDK version is pinned exactly in one place (`global.json`). The DevContainer and the Android CI jobs use exactly that version and check it.

## Background and decisions
- The Android lock file contains `Microsoft.NET.ILLink.Tasks`, an implicit dependency that the SDK adds. Its version is the runtime version that ships with the SDK (SDK `10.0.403` → `10.0.14`). With any other SDK, locked restore fails (NU1004). The other projects are not affected.
- So far, CI (`setup-dotnet` with `rollForward: latestFeature`) and the DevContainer (`--channel 10.0`) each fetch the newest SDK. These versions drift apart.
- **Decision:**
  - `global.json` names the exact version `10.0.403` and is the single source.
  - `rollForward: latestFeature` stays, so that the server image (`sdk:10.0`) and other machines keep building; for them the version is a minimum.
  - The version is enforced exactly only where the Android lock file is created or checked: in the DevContainer and in the Android CI jobs.
  - In CI the SDK is installed in isolation (`DOTNET_INSTALL_DIR`), so that no newer SDK preinstalled on the runner wins through roll-forward. A check step fails on any mismatch.
- **SDK updates are triggered automatically from now on:**
  - Dependabot (`dotnet-sdk`) bumps `global.json`.
  - The DevContainer version and the Android lock file are updated in the same PR (procedure in SICHERHEIT.md 6.3).
  - Until then the Android check is red. That is intended: auto-merge cannot merge anything inconsistent.
  - Fully automating that follow-up is a separate, later work package.
- Signed commits are not needed locally: GitHub signs squash merges. DCO (`-s`) is mandatory.
- **Language:** the snippets below that end up in the repo (comments, SICHERHEIT.md) are in German on purpose, because German is still the documentation language. Copy them exactly as written.

## Out of scope
- No update of packages, actions, images or of the SDK itself; the version stays `10.0.403`.
- No change to the `build-test` job in `ci.yml`, to the server `Dockerfile` or to `Directory.Build.props`.
- No pinning of the workload version.
- No automation of the lock refresh in Dependabot PRs.
- No change to any other `packages.lock.json`.

## Allowed files
- `.devcontainer/devcontainer-lock.json` (new, content unchanged)
- `src/FamilyHome.Client.Android/packages.lock.json` (new, content unchanged)
- `global.json`
- `.devcontainer/Dockerfile`
- `.github/workflows/ci.yml` (`android` job only)
- `.github/workflows/release-android.yml`
- `.github/workflows/devcontainer.yml`
- `.github/dependabot.yml`
- `docs/SICHERHEIT.md` (only ch. 6.3 and the workflow table)
- this work package file

## Preconditions
- [ ] Work package 533c3095 is in `work/archive/`.
- [ ] Work happens in the DevContainer.
- [ ] `dotnet --version` prints `10.0.403`. If it prints anything else: **stop**.
- [ ] On `main`, `git status --short` shows exactly these two lines:
  ```
  ?? .devcontainer/devcontainer-lock.json
  ?? src/FamilyHome.Client.Android/packages.lock.json
  ```

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/3399485c*"` and `gh pr list --state all --search "3399485c"` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/3399485c-lockfiles-sdk-pin`. The two untracked files come along.
4. In the front matter, set `status: in-progress`, `branch: wp/3399485c-lockfiles-sdk-pin` and `assignee`.
5. `git commit -s -m "WP 3399485c: claimed" work/wp-3399485c-lockfiles-sdk-pin.md && git push -u origin HEAD`

## Steps

### Step 1: Check the lock files
- Check: `jq -r '.features | keys[]' .devcontainer/devcontainer-lock.json`
  → exactly the 5 features from `.devcontainer/devcontainer.json`: `claude-code:1`, `desktop-lite:1`, `docker-outside-of-docker:1`, `github-cli:1`, `node:2`.
- Check: `jq -r '.features[] | .resolved' .devcontainer/devcontainer-lock.json`
  → every line ends in `@sha256:` followed by 64 hex characters.
- Check: `CI=true dotnet restore src/FamilyHome.Client.Android --locked-mode`
  → finishes without errors and reports `Restored …FamilyHome.Client.Android.csproj`.
- Check: `jq -r '.dependencies[]["Microsoft.NET.ILLink.Tasks"].resolved' src/FamilyHome.Client.Android/packages.lock.json`
  → must show the same version as `dotnet --list-runtimes | grep NETCore.App` (currently `10.0.14`).
- Check: `dotnet restore FamilyHome.slnx`, then `git status --short`
  → no change apart from the two new files, in particular no other `packages.lock.json` changed.
- Do: `git add .devcontainer/devcontainer-lock.json src/FamilyHome.Client.Android/packages.lock.json`

### Step 2: `global.json`
- Do: `"version": "10.0.100"` → `"version": "10.0.403"`. `rollForward` and the `test` section stay unchanged.
- Check: `jq -r .sdk.version global.json` → `10.0.403`. `dotnet --version` → still `10.0.403`.

### Step 3: The DevContainer installs exactly this version
In `.devcontainer/Dockerfile`:
- Replace `ARG DOTNET_CHANNEL=10.0` with:
  ```dockerfile
  # Muss global.json entsprechen (Android-Lock-Datei, docs/SICHERHEIT.md 6.3); geprüft in devcontainer.yml
  ARG DOTNET_SDK_VERSION=10.0.403
  ```
- In the `# .NET SDK` section, replace `--channel ${DOTNET_CHANNEL}` with `--version ${DOTNET_SDK_VERSION}`.
- Check: `grep -n DOTNET_ .devcontainer/Dockerfile`
  → `DOTNET_CHANNEL` no longer appears; `DOTNET_SDK_VERSION` appears exactly twice (ARG and usage).
- Rebuilding the container locally is not needed, because it already has 10.0.403. The workflow from step 4 checks the image.

### Step 4: `devcontainer.yml` checks the version
- Do: add `"global.json"` to both `pull_request.paths` and `push.paths`.
- Do: after the step `Werkzeuge im Image prüfen`, append a new step:
  ```yaml
      - name: SDK-Version wie in global.json
        run: |
          expected="$(jq -r .sdk.version global.json)"
          actual="$(docker run --rm familyhome-devcontainer:ci dotnet --version)"
          echo "global.json: $expected, Image: $actual"
          test "$expected" = "$actual"
  ```

### Step 5: The Android jobs use exactly this version
In `.github/workflows/ci.yml` (`android` job only) and in `.github/workflows/release-android.yml`, replace this block:
```yaml
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          global-json-file: global.json
```
with:
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
In `ci.yml` only (`android` job), also insert this before `Build APK`, so that a lock mismatch gives a clear error message:
```yaml
      - name: Restore (Locked Mode)
        run: dotnet restore src/FamilyHome.Client.Android --locked-mode
```
- Check: `grep -n "global-json-file" .github/workflows/*.yml`
  → only the one occurrence in the `build-test` job of `ci.yml` remains.
- Check: `git diff .github/workflows/ci.yml`
  → no change in the `build-test` job.
- The `setup-dotnet` SHA stays exactly the same. Do not add any other actions.

### Step 6: Dependabot for the SDK
In `.github/dependabot.yml`, insert after the `nuget` entry:
```yaml
  # Erhöht global.json. Im selben PR DevContainer-Version und Android-Lock-Datei nachziehen (docs/SICHERHEIT.md 6.3)
  - package-ecosystem: dotnet-sdk
    directory: /
    schedule:
      interval: weekly
```
- Check: in the GitHub docs "Dependabot options reference" → `package-ecosystem` must list `dotnet-sdk` as a valid value. If it does not: **stop**.

### Step 7: Documentation
In `docs/SICHERHEIT.md`:
- Ch. 6.3 gets this new bullet:
  ```markdown
  - **.NET SDK exakt festgelegt:** `global.json` nennt die Version (einzige Quelle). DevContainer und Android-CI installieren genau diese und prüfen das, weil die Android-Lock-Datei `Microsoft.NET.ILLink.Tasks` in der Runtime-Version des SDK enthält. Ablauf bei einem SDK-Update (Dependabot-PR für `global.json`): `DOTNET_SDK_VERSION` in `.devcontainer/Dockerfile` angleichen, Container neu bauen, `dotnet restore src/FamilyHome.Client.Android --force-evaluate`, Lock-Datei im selben PR committen. Bis dahin ist der Android-Check rot, Auto-Merge greift also nicht.
  ```
- In the workflow table, change the `devcontainer.yml` row:
  - trigger: `Änderung an .devcontainer/ oder global.json, wöchentlich`;
  - append `, SDK-Version = global.json` to the description.

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green (it includes actionlint, zizmor, hadolint and locked restore, among others).
- [ ] `CI=true dotnet restore src/FamilyHome.Client.Android --locked-mode` succeeds.
- [ ] `dotnet build src/FamilyHome.Client.Android -c Debug` runs without warnings.
- [ ] `git diff --name-only origin/main...HEAD` lists only allowed files.
- [ ] `git log origin/main..HEAD --format=%B | grep -c "Signed-off-by"` is at least the number of commits.

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All 10 required checks on the PR are green.
- [ ] In the log of the "Android-APK" check, the step "SDK-Version prüfen" shows `10.0.403` and the step "Restore (Locked Mode)" is green.
- [ ] The "DevContainer" workflow ran on the PR (because of `.devcontainer/**` or `global.json`) and is green, including "SDK-Version wie in global.json".
- [ ] The PR is marked as security-relevant (pipeline, supply chain).

## Stop conditions
**Stop immediately**, set `status: blocked`, write what you found into the log and ask the owner if:
- a check in step 1 gives a different result, especially another lock file that changed or an ILLink version ≠ runtime version;
- `dotnet --version` in the DevContainer is not `10.0.403`;
- "SDK-Version prüfen" shows a different version in CI. That would mean `DOTNET_INSTALL_DIR` does not work as expected. Do not work around it with other tricks;
- zizmor, actionlint, Semgrep or hadolint report anything. Do not suppress it with an ignore;
- `dotnet-sdk` is not a valid Dependabot ecosystem;
- a file outside the allowed list would have to change;
- a different action version or a new SHA seems necessary.

## Completion by the worker
1. Fill in the log and set `status: in-review`.
2. `git commit -s`, push, open a PR against `main`:
   - title: `Commit lock files, pin .NET SDK exactly for Android (WP 3399485c)`;
   - in the PR template, tick "security-relevant" (pipeline, supply chain) and briefly summarize the decision from "Background";
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
3. Do not merge it yourself.

## Acceptance by the owner
On the PR branch: set `status: done`, `git mv work/wp-3399485c-lockfiles-sdk-pin.md work/archive/`, then squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Deviations/observations:
