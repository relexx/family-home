---
status: open
branch:
assignee:
depends_on: []
security_relevant: true  # git hooks run code on the developer machine
---

# Add the DCO sign-off automatically in the DevContainer

## Language
Everything written into the repo is English: code, string literals, comments, documentation, commit messages, PR texts
and the log below. The app UI supports English and German; user-facing texts go only into
`Resources/Strings*.resx` (German neutral, English), never hard-coded.

## Goal
A forgotten `-s` can no longer fail the required check DCO.
- A committed hook `.githooks/prepare-commit-msg` adds `Signed-off-by: <user.name> <user.email>` to every commit message.
- `.devcontainer/setup-signing.sh` enables the hook on every container start.

## Background and decisions
- **Why:** in PR #13 the DCO check failed because one commit was made without `-s`.
- **A committed hook directory instead of a hook in `.git/hooks`.** The hook is versioned and reviewed like any other code. `git config core.hooksPath .githooks` is set repo-locally (not `--global`), so it only affects this repository.
- **Security:** with `core.hooksPath`, every file in `.githooks/` runs on the developer machine. Therefore `.githooks/` gets an entry in `CODEOWNERS`, like `.devcontainer/`.
- **Enabled from `setup-signing.sh`.** That script already runs on every start and already exits early without `user.email`; without an e-mail a sign-off would be useless anyway.
- **Idempotent:** the hook uses `git interpret-trailers --if-exists addIfDifferent`. An identical `Signed-off-by` (e.g. from `-s`) is not added twice; other trailers such as `Co-Authored-By` stay.
- Merge commits get a sign-off as well. That is harmless; the DCO check ignores merges.
- `git commit -s` stays documented in `CONTRIBUTING.md` for contributors outside the DevContainer.
- The complete change was built and tested before this package was written (throwaway `HOME` and repository); it is included below as a patch. Results:
  - sign-off added for `-m`, together with other trailers, and for merges;
  - no duplicate with `-s`;
  - no action without `user.email`;
  - started from another directory, the script still sets `core.hooksPath` in the repository;
  - test commit signed (`G`) and signed off;
  - ShellCheck green for both scripts.

## Out of scope
- No other hooks (no pre-commit or pre-push checks).
- No change to the DCO workflow.
- No translation of the existing German text in `CONTRIBUTING.md` (work package bd2b179e).

## Allowed files
Exactly the 4 files in the patch, plus this work package file:
- `.githooks/prepare-commit-msg` (new, executable)
- `.devcontainer/setup-signing.sh`
- `.github/CODEOWNERS`
- `CONTRIBUTING.md`

## Preconditions
- [ ] Work happens in the DevContainer, `main` is up to date.
- [ ] `git config --global --get commit.gpgsign` prints `true` (signing from wp-30936f54 is active).

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/62b65119*"` and `gh pr list --state all --head wp/62b65119-auto-dco-signoff` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/62b65119-auto-dco-signoff`
4. In the front matter, set `status: in-progress`, `branch: wp/62b65119-auto-dco-signoff` and `assignee`.
5. `git commit -s -m "WP 62b65119: claimed" work/wp-62b65119-auto-dco-signoff.md && git push -u origin HEAD`

## Steps

### Step 1: Extract and check the patch
Write the patch from the section "Patch" at the end of this file into a temporary file:
```bash
awk '/^```diff$/{f=1;next} /^```$/{f=0} f' work/wp-62b65119-auto-dco-signoff.md > "${TMPDIR:-/tmp}/wp-62b65119.patch"
git apply --check "${TMPDIR:-/tmp}/wp-62b65119.patch" && echo OK
```
Expected: `OK`. If it fails (the files changed on `main` since this package was written): **stop**. Do not edit the patch by hand.

### Step 2: Apply the patch
```bash
git apply "${TMPDIR:-/tmp}/wp-62b65119.patch"
git status --short
test -x .githooks/prepare-commit-msg && echo EXECUTABLE
```
Expected: the 4 allowed files are changed or new (plus the untracked lock files, if they still exist), and `EXECUTABLE`.

### Step 3: Test in a throwaway environment (never with the real HOME)
```bash
real_home="$HOME"; t="$(mktemp -d)"; export HOME="$t/home"; mkdir -p "$HOME/.signing"
git config --global user.email test@example.org; git config --global user.name Test
git init -q "$t/r"; mkdir -p "$t/r/.devcontainer" "$t/r/.githooks"
cp .devcontainer/setup-signing.sh "$t/r/.devcontainer/"; cp .githooks/prepare-commit-msg "$t/r/.githooks/"
(cd "$t" && FAMILYHOME_SIGNING_DIR="$HOME/.signing" bash r/.devcontainer/setup-signing.sh > /dev/null)
git -C "$t/r" config core.hooksPath
(cd "$t/r" && git commit -q --allow-empty -m one && git log -1 --format=%B | grep -c "Signed-off-by: Test <test@example.org>")
(cd "$t/r" && git commit -q --allow-empty -s -m two && git log -1 --format=%B | grep -c "Signed-off-by")
(cd "$t/r" && git commit -q --allow-empty -m three -m "Co-Authored-By: X <x@example.org>" && git log -1 --format=%B | grep -c -e "Co-Authored-By" -e "Signed-off-by")
(cd "$t/r" && git log -1 --format='sig=%G?')
rm -rf "$t"; export HOME="$real_home"
```
Expected, in this order: `.githooks`, `1`, `1`, `2`, `sig=G`.
Afterwards: `git config --global user.email` prints the real address again (the test did not touch the real configuration).

### Step 4: ShellCheck for the hook
The Security Gate only checks `*.sh` files, so check the hook explicitly:
```bash
docker run --rm -i koalaman/shellcheck:v0.11.0 - < .githooks/prepare-commit-msg && echo OK
```
Expected: `OK`.

### Step 5: Enable it in this container and use it
```bash
bash .devcontainer/setup-signing.sh
git config core.hooksPath
```
Expected: `.githooks`. From now on, make the remaining commits of this package **without** `-s` and check that they still pass DCO.

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green (ShellCheck checks `setup-signing.sh`).
- [ ] Steps 3–5 fulfilled.
- [ ] `git diff --name-only origin/main...HEAD` lists only allowed files.
- [ ] Every commit is signed: `for c in $(git rev-list origin/main..HEAD); do git cat-file -p "$c" | grep -q 'BEGIN SSH SIGNATURE' || echo "UNSIGNED $c"; done` → no output. After pushing, the PR shows every commit as "Verified".
- [ ] Every commit is signed off: `for c in $(git rev-list --no-merges origin/main..HEAD); do git log -1 --format=%B "$c" | grep -q '^Signed-off-by: ' || echo "NO SIGN-OFF $c"; done` → no output.

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All required checks on the PR are green, including DCO.
- [ ] At least one commit of this PR was made without `-s` and still has a sign-off (noted in the log).

## Stop conditions
If any of the following happens, **stop immediately**: set `status: blocked`, write what you found into the log and ask the owner. Do not improvise, weaken or suppress anything:
- A commit cannot be signed. Never disable signing (`--no-gpg-sign`, `-c commit.gpgsign=false`).
- A check cannot really run in your environment. Report it as "not run" with the reason; never report it as green.
- `git apply --check` fails.
- A result in step 3 differs from the expected one, or the test changed the real git configuration.
- ShellCheck or another check reports anything. Do not suppress it.
- A file outside the allowed list would have to change.

## Completion by the worker
1. Fill in the log, set `status: in-review`.
2. Commit, push, open a PR against `main` that references this file:
   - title: `Add the DCO sign-off automatically in the DevContainer (WP 62b65119)`;
   - fill in the PR template (`.github/pull_request_template.md`) completely; a list of commits is not a description;
   - tick "security-relevant" (git hooks run code on the developer machine);
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
3. Do not merge it yourself.

## Acceptance by the owner
On the PR branch: set `status: done`, `git mv work/wp-62b65119-auto-dco-signoff.md work/archive/`, then squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Commit made without `-s`:
- Deviations/observations:

## Patch
Tested against `main` at 73ea64b. Do not edit; extract it with the command from step 1.

```diff
diff --git a/.devcontainer/setup-signing.sh b/.devcontainer/setup-signing.sh
index 137533d..4466fb8 100755
--- a/.devcontainer/setup-signing.sh
+++ b/.devcontainer/setup-signing.sh
@@ -3,6 +3,7 @@
 # It lives on the named volume "git-signing" and therefore survives rebuilds. The owner registers it on GitHub
 # by hand as a "Signing Key"; the container is deliberately not allowed to register keys itself.
 # Runs on every container start and is idempotent: an existing key is never replaced.
+# It also enables the repo's git hooks (.githooks/), which add the DCO sign-off to every commit.
 set -euo pipefail
 
 dir="${FAMILYHOME_SIGNING_DIR:-$HOME/.signing}"
@@ -31,6 +32,9 @@ git config --global gpg.ssh.allowedSignersFile "$dir/allowed_signers"
 git config --global commit.gpgsign true
 git config --global tag.gpgsign true
 
+# DCO sign-off for every commit in this repository (.githooks/prepare-commit-msg)
+git -C "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)" config core.hooksPath .githooks
+
 if [[ "$created" == true ]]; then
   echo "New commit signing key. Add it on GitHub once: Settings > SSH and GPG keys > New SSH key > Key type: Signing Key"
   cat "$key.pub"
diff --git a/.githooks/prepare-commit-msg b/.githooks/prepare-commit-msg
new file mode 100755
index 0000000..0f71ddd
--- /dev/null
+++ b/.githooks/prepare-commit-msg
@@ -0,0 +1,11 @@
+#!/usr/bin/env bash
+# Adds the DCO sign-off (CONTRIBUTING.md) to every commit message, so that a forgotten `-s` cannot fail the DCO check.
+# Enabled by .devcontainer/setup-signing.sh via `git config core.hooksPath .githooks`. Idempotent: an identical
+# Signed-off-by line is never added twice.
+set -euo pipefail
+
+name="$(git config user.name || true)"
+email="$(git config user.email || true)"
+[[ -n "$name" && -n "$email" ]] || exit 0
+
+git interpret-trailers --in-place --if-exists addIfDifferent --trailer "Signed-off-by: $name <$email>" "$1"
diff --git a/.github/CODEOWNERS b/.github/CODEOWNERS
index c6ddf03..23225c2 100644
--- a/.github/CODEOWNERS
+++ b/.github/CODEOWNERS
@@ -2,6 +2,7 @@
 /.github/                    @relexx
 /deploy/                     @relexx
 /.devcontainer/              @relexx
+/.githooks/                  @relexx
 /docs/SICHERHEIT.md          @relexx
 /src/**/Security/            @relexx
 /src/FamilyHome.Server/Auth/ @relexx
diff --git a/CONTRIBUTING.md b/CONTRIBUTING.md
index 219f553..d0823fd 100644
--- a/CONTRIBUTING.md
+++ b/CONTRIBUTING.md
@@ -10,6 +10,8 @@ Statt eines CLA nutzen wir das [DCO](https://developercertificate.org/). Jeder C
 git commit -s -m "Beschreibung"
 ```
 
+In the DevContainer, the hook `.githooks/prepare-commit-msg` adds the sign-off automatically (enabled by `.devcontainer/setup-signing.sh`).
+
 Mit dem Sign-off bestätigst du, dass du den Beitrag unter der Projektlizenz einreichen darfst.
 
 Der Pflicht-Check **DCO** prüft jeden Commit im PR. Die Sign-off-Adresse muss mit der Autor-E-Mail übereinstimmen. Fehlt der Sign-off, lässt er sich nachträglich ergänzen:
```
