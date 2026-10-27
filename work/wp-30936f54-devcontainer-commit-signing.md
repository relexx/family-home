---
status: in-review
branch: wp/30936f54-devcontainer-commit-signing
assignee: relexx
depends_on: []
security_relevant: true  # key material and commit signing in the development environment
---

# Commit signing key that survives DevContainer rebuilds

## Language
Everything written into the repo is English: code, string literals, comments, documentation, commit messages, PR texts
and the log below. The app UI supports English and German; user-facing texts go only into
`Resources/Strings*.resx` (German neutral, English), never hard-coded.

## Goal
The ruleset on `main` requires verified signatures, so every commit made in the DevContainer has to be signed.
- **Today:** the owner creates an SSH signing key by hand in `~/.ssh`. It is lost on every rebuild and has to be registered on GitHub again.
- **After this package:** a script creates the key once on a named volume, keeps it across rebuilds and configures git on every container start. The owner registers it on GitHub **once**.

## Background and decisions
Decided with the owner:
- **Created at container start, never in the image build.** A key in an image layer would be a secret in a build artifact; the `devcontainer.yml` workflow builds that image in CI.
- **A dedicated key on its own named volume** `git-signing`, mounted at `/home/vscode/.signing`. The script creates it only if it is missing, so a rebuild reuses it. To rotate: delete the volume, rebuild, register the new key and remove the old one on GitHub.
- **Registering on GitHub stays manual** (Settings → SSH and GPG keys → Key type: *Signing Key*). The container's GitHub token must not get the right to add signing keys; otherwise any AI in the container could register its own "Verified" keys. This manual step is the owner's approval of the key.
- **No passphrase**, so that Claude and the workers can sign non-interactively. Consequence, documented in SICHERHEIT.md ch. 8: "Verified" means "created in the owner's DevContainer", not "written by the owner personally". Required checks and the owner's merge remain the actual control.
- The script runs as `postStartCommand`, not `postCreateCommand`: VS Code copies the host `.gitconfig` (with `user.email`) into the container, and on every start the script makes sure the signing configuration is in place. It is idempotent and never replaces an existing key.
- It also writes `allowed_signers`, so that `git log --format=%G?` shows `G` locally instead of `N`.
- The script was written and tested before this package (throwaway `HOME`). Results:
  - without `user.email`: clean skip with exit code 0;
  - first run: creates the key;
  - second run: same key;
  - directory mode `700`;
  - a test commit shows `%G?` = `G`;
  - ShellCheck green.

## Out of scope
- No change to `post-create.sh`, the `Dockerfile` or any workflow.
- No automatic registration on GitHub, no change to the `gh` token scopes.
- No signing of tags in CI or of releases (release keys never enter the DevContainer).

## Allowed files
- `.devcontainer/setup-signing.sh` (new)
- `.devcontainer/devcontainer.json`
- `.devcontainer/docker-compose.yml`
- `docs/SICHERHEIT.md` (only ch. 8)
- this work package file

## Preconditions
- [ ] Work happens in the DevContainer, `main` is up to date.
- [ ] `docker compose version` works.

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/30936f54*"` and `gh pr list --state all --head wp/30936f54-devcontainer-commit-signing` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/30936f54-devcontainer-commit-signing`
4. In the front matter, set `status: in-progress`, `branch: wp/30936f54-devcontainer-commit-signing` and `assignee`.
5. `git commit -s -m "WP 30936f54: claimed" work/wp-30936f54-devcontainer-commit-signing.md && git push -u origin HEAD`

## Steps

### Step 1: Create the script
- Do: extract the script from the section "Script" at the end of this file:
  ```bash
  awk '/^```bash setup-signing$/{f=1;next} /^```$/{f=0} f' work/wp-30936f54-devcontainer-commit-signing.md > .devcontainer/setup-signing.sh
  ```
- Check: `head -1 .devcontainer/setup-signing.sh` → `#!/usr/bin/env bash`; `tail -1 .devcontainer/setup-signing.sh` → `fi`.
- Check: `bash -n .devcontainer/setup-signing.sh && echo OK` → `OK`.

### Step 2: Test the script with a throwaway HOME (never with the real one)
First note the real value: `git config --global --get gpg.ssh.allowedSignersFile` (may be empty).
```bash
t="$(mktemp -d)"; mkdir "$t/.signing"
HOME="$t" FAMILYHOME_SIGNING_DIR="$t/.signing" bash .devcontainer/setup-signing.sh; echo "exit=$?"
HOME="$t" git config --global user.email test@example.org
HOME="$t" git config --global user.name Test
HOME="$t" FAMILYHOME_SIGNING_DIR="$t/.signing" bash .devcontainer/setup-signing.sh | head -1
k1="$(cat "$t/.signing/familyhome_signing.pub")"
HOME="$t" FAMILYHOME_SIGNING_DIR="$t/.signing" bash .devcontainer/setup-signing.sh
[ "$k1" = "$(cat "$t/.signing/familyhome_signing.pub")" ] && echo KEY_UNCHANGED
stat -c '%a' "$t/.signing"
(cd "$t" && git init -q r && cd r && HOME="$t" git commit -q --allow-empty -m t && HOME="$t" git log -1 --format='sig=%G?')
rm -rf "$t"
```
Expected, in this order:
1. the message `Commit signing: git user.email is not set yet; …` and `exit=0`;
2. `New commit signing key. Add it on GitHub once: …`;
3. no output from the second run;
4. `KEY_UNCHANGED`;
5. `700`;
6. `sig=G`.

Check afterwards that the real configuration was not touched: `git config --global --get gpg.ssh.allowedSignersFile` prints the same value as before the test.

### Step 3: Volume in `docker-compose.yml`
In `.devcontainer/docker-compose.yml`:
- Under `services.workspace.volumes`, after the line `- claude-config:/home/vscode/.claude`, add:
  ```yaml
      # Commit signing key (.devcontainer/setup-signing.sh), survives rebuilds
      - git-signing:/home/vscode/.signing
  ```
- Under the top-level `volumes:`, after `claude-config:`, add `  git-signing:`.
- Check: `docker compose -f .devcontainer/docker-compose.yml config -q && echo OK` → `OK`.
- Check: `docker compose -f .devcontainer/docker-compose.yml config | grep -c git-signing` → at least `2`.

### Step 4: `devcontainer.json`
- Do: after the line `"remoteUser": "vscode",` add:
  ```jsonc
    // Commit signing on every start; idempotent, never replaces an existing key
    "postStartCommand": "bash .devcontainer/setup-signing.sh",
  ```
- Check: `git diff .devcontainer/devcontainer.json` shows only these 2 added lines.

### Step 5: Security concept
In `docs/SICHERHEIT.md` ch. 8, after the bullet `Der DevContainer läuft ohne Zugriff auf Host-Geheimnisse …`, add (in English, although the chapter is still German):
```markdown
- **Commit signing:** `.devcontainer/setup-signing.sh` creates a dedicated SSH key without a passphrase on the volume `git-signing` (once, kept across rebuilds) and configures git to sign every commit. The owner registers it on GitHub by hand as a *Signing Key*; the container may not register keys itself. "Verified" therefore means "created in the owner's DevContainer", not "written by the owner"; required checks and the owner's merge remain the control. Rotation: delete the volume, rebuild, register the new key, remove the old one on GitHub.
```

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green (ShellCheck checks the new script).
- [ ] Steps 1–4 fulfilled.
- [ ] `git diff --name-only origin/main...HEAD` lists only allowed files.
- [ ] No private key in the diff: `git diff origin/main...HEAD | grep -c "PRIVATE KEY"` → `0`.

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All required checks on the PR are green; the "DevContainer" workflow ran and is green.
- [ ] Owner, with the PR branch checked out and the DevContainer rebuilt from it:
  - the start log shows `New commit signing key …` and the public key;
  - after registering it on GitHub, a test commit on a branch shows "Verified";
  - after a second rebuild, no new key message appears and `cat ~/.signing/familyhome_signing.pub` shows the same key;
  - the old key from `~/.ssh` was removed on GitHub.

## Stop conditions
**Stop immediately**, set `status: blocked`, write what you found into the log and ask the owner if:
- a result in step 2 differs from the expected one;
- the test in step 2 changed the real git configuration;
- `docker compose … config` fails;
- ShellCheck or another check reports anything. Do not suppress it;
- a file outside the allowed list would have to change.

## Completion by the worker
1. Fill in the log and set `status: in-review`.
2. `git commit -s`, push, open a PR against `main`:
   - title: `Persistent commit signing key in the DevContainer (WP 30936f54)`;
   - tick "security-relevant" (key material, development environment) and summarize the decisions;
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
3. Do not merge it yourself.

## Acceptance by the owner
Check out the PR branch, rebuild the DevContainer and check the last acceptance criterion. Then, on the PR branch: set `status: done`, `git mv work/wp-30936f54-devcontainer-commit-signing.md work/archive/`, squash-merge.

## Log (filled in by the worker)
- Date/assignee: 2026-10-27 / relexx (Claude)
- PR:
- Output of the checks (shortened):
  - Step 1 (script extraction): `head -1` = `#!/usr/bin/env bash`, `tail -1` = `fi`, `bash -n` = OK.
  - Step 2 (throwaway HOME): message "git user.email is not set yet…" + exit=0; "New commit signing key…"; second run silent; `KEY_UNCHANGED`; mode `700`; `sig=G`. Real `gpg.ssh.allowedSignersFile` unchanged (empty).
  - Step 3 (`docker-compose.yml`): `docker compose … config -q` = OK; `grep -c git-signing` = 3.
  - Step 4 (`devcontainer.json`): diff shows only the 2 added lines.
  - Checks: `scripts/security-gate.sh` green (ShellCheck included); diff contains only allowed files; the check against the private-key marker in `git diff origin/main…HEAD` reports zero hits.
- Deviations/observations: none.

## Script
Tested; do not edit. Extract it with the command from step 1.

```bash setup-signing
#!/usr/bin/env bash
# Commit signing inside the DevContainer (docs/SICHERHEIT.md ch. 8): a dedicated SSH key that can only sign.
# It lives on the named volume "git-signing" and therefore survives rebuilds. The owner registers it on GitHub
# by hand as a "Signing Key"; the container is deliberately not allowed to register keys itself.
# Runs on every container start and is idempotent: an existing key is never replaced.
set -euo pipefail

dir="${FAMILYHOME_SIGNING_DIR:-$HOME/.signing}"
key="$dir/familyhome_signing"

email="$(git config --global user.email || true)"
if [[ -z "$email" ]]; then
  echo "Commit signing: git user.email is not set yet; set it and run: bash .devcontainer/setup-signing.sh" >&2
  exit 0
fi

# Named volumes belong to root when they are created
[[ -O "$dir" ]] || sudo chown "$(id -u):$(id -g)" "$dir"
chmod 700 "$dir"

created=false
if [[ ! -f "$key" ]]; then
  ssh-keygen -q -t ed25519 -N "" -C "familyhome-devcontainer signing" -f "$key"
  created=true
fi

printf '%s %s\n' "$email" "$(cat "$key.pub")" > "$dir/allowed_signers"
git config --global gpg.format ssh
git config --global user.signingkey "$key.pub"
git config --global gpg.ssh.allowedSignersFile "$dir/allowed_signers"
git config --global commit.gpgsign true
git config --global tag.gpgsign true

if [[ "$created" == true ]]; then
  echo "New commit signing key. Add it on GitHub once: Settings > SSH and GPG keys > New SSH key > Key type: Signing Key"
  cat "$key.pub"
fi
```
