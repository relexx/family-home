---
status: open
branch:
assignee:
depends_on: [533c3095, 3399485c, d188095c]  # they touch files translated here; run this package last to avoid conflicts
security_relevant: true            # touches workflows, CODEOWNERS and the security concept (text only)
---

# Translate the repository to English

## Language
Everything written into the repo is English: code, string literals, comments, documentation, commit messages, PR texts
and the log below. The app UI supports English and German; user-facing texts go only into
`Resources/Strings*.resx` (German neutral, English), never hard-coded.

## Goal
The owner's language rules are applied to the whole repo and written down in `CLAUDE.md`:
- **Repo content is English:** documentation, all documents, code including string literals, code comments, the PR template, commit messages and PR texts.
- **Instructions for worker AIs are English:** `CLAUDE.md`, work packages, templates.
- **The app supports English and German** via resx. This does not change.
- **Chat with the owner is German.**

Today most of the repo is German. After this package, only the German resx file remains German (and the glossary in this file).

## Background and decisions
- **English only.** German documentation is replaced, not kept as a copy.
- **The meaning stays exactly the same.** This is a translation, not a rewrite. No new content, no "improvements", no shortened sections. Where the German is unclear, translate it as literally as possible and record it in the log.
- **US English**, plain style, short sentences.
- **Renamed documents:**
  - `docs/SPEZIFIKATION.md` → `docs/SPECIFICATION.md`
  - `docs/SICHERHEIT.md` → `docs/SECURITY-CONCEPT.md`
  - `docs/THREAT-MODEL.md` keeps its name.
  - All references in the repo are updated (code comments, CODEOWNERS, workflows, templates, `CLAUDE.md`, open work packages).
- **Chapter numbers stay unchanged.** "Kap. 6.3" becomes "ch. 6.3". Headings keep their numbers, only their text is translated.
- **String literals in code are translated** (log, exception and debug messages, test data, output of `scripts/security-gate.sh`). If a test asserts on such a text, the test is updated to the same English text.
- **Workflow step names** (`- name:` inside `steps:`) are translated. **Workflow and job names** (`name:` at workflow and job level) stay unchanged: the ruleset requires checks by their names, so renaming them would block every PR.
- **Glossary** (use these terms consistently):

  | German | English |
  |---|---|
  | Inhaber | owner |
  | Arbeitspaket | work package |
  | Familien-CA | family CA |
  | Gerätezertifikat | device certificate |
  | Beitritt / Einladungscode | join / invite code |
  | Wiederherstellungscode | recovery code |
  | Eltern / Kinder | parents / children |
  | Feiertage / Schulferien | public holidays / school holidays |
  | Bundesland | federal state |
  | Einkaufsliste | shopping list |
  | Fahrdienst | rides |
  | „Wo ist was?“ | "Where Is It?" |
  | Essensplan / Notfallkarte / Geschenkideen | meal plan / emergency card / gift ideas |
  | Security Gate, Zero Trust, Step-up, Passkey | unchanged |
  | Pflicht-Checks | required checks |

  UI names follow `Strings.en.resx` (tabs: Today, Calendar, Lists, People, More).
- **New convention lines in `CLAUDE.md`** (replace the line `Code-Bezeichner Englisch, Kommentare und Doku Deutsch, UI-Texte nur über …`; copy exactly):
```markdown
- Language: everything in the repo is English: code (including string literals such as log and exception messages), comments, documentation, work packages and other instructions for worker AIs, commit messages and PR texts.
- The app supports English and German: user-facing texts only via `Resources/Strings*.resx` (German neutral, English), never hard-coded.
- Chat with the owner is in German.
```
  `CONTRIBUTING.md` gets the same first two rules.

## Out of scope
- **User-facing texts are not translated here.** They belong in resx; work package d188095c already moved the hard-coded ones there (holiday names, view placeholders, sync status). If you still find a hard-coded user-facing German text: do not translate it, see "Stop conditions".
- **No identifiers change:** class, method, property, file and resource names stay as they are, except the two renamed docs. If you find a German identifier, list it in the log; do not rename it.
- Workflow and job names (see above).
- `Resources/Strings*.resx`, `LICENSE`, all `packages.lock.json`, `work/archive/`, the git history.
- No content changes in the specification or the security concept, even if something looks outdated. Record it in the log instead.

## Allowed files
All tracked text files **except** the ones listed under "Out of scope". In particular:
- `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CLAUDE.md`
- `docs/**` (including the two renames)
- `.github/**` (workflows and `dependabot.yml`: comments and step names; `CODEOWNERS`: comment and path; PR template: full text)
- `work/*.md` (open packages: only references to the renamed docs; template: references only)
- comments and string literals in `src/**`, `tests/**`, `scripts/**`, `deploy/**`, `.devcontainer/**`
- `.semgrep/**` (comments and `message:`), `BannedSymbols*.txt` (message text after `;`), `Directory.*.props`, `*.csproj`, `.editorconfig`, `.gitleaks.toml`, `.gitignore`, `.dockerignore`

## Preconditions
- [ ] Packages 533c3095, 3399485c and d188095c are in `work/archive/`.
- [ ] Work happens in the DevContainer, `main` is up to date, `scripts/security-gate.sh` is green on `main`.
- [ ] There is no other open PR that changes `docs/`. If there is one: ask the owner whether to wait.

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/bd2b179e*"` and `gh pr list --state all --head wp/bd2b179e-translate-repo-english` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/bd2b179e-translate-repo-english`
4. In the front matter, set `status: in-progress`, `branch: wp/bd2b179e-translate-repo-english` and `assignee`.
5. `git commit -s -m "WP bd2b179e: claimed" work/wp-bd2b179e-translate-repo-english.md && git push -u origin HEAD`

## Steps
Make **one commit per step** (`git commit -s`, English message), so that the PR stays reviewable.

### Step 1: Record the baseline and create the code check
- Do: save the heading counts of the docs and the number of TODO markers:
```bash
for f in README.md CONTRIBUTING.md SECURITY.md CLAUDE.md docs/SPEZIFIKATION.md docs/SICHERHEIT.md docs/THREAT-MODEL.md; do
  printf '%s %s\n' "$(grep -c '^#' "$f")" "$f"; done > "${TMPDIR:-/tmp}/wp-bd2b179e-headings.txt"
grep -rn "TODO Phase" src | wc -l > "${TMPDIR:-/tmp}/wp-bd2b179e-todos.txt"
```
- Do: create the check script (outside the repo). It fails if anything other than comments or string literals changed in a C#/AXAML file compared to a base commit:
```bash
cat > "${TMPDIR:-/tmp}/wp-bd2b179e-codecheck.py" <<'EOF'
import re, subprocess, sys
base = sys.argv[1] if len(sys.argv) > 1 else "HEAD~1"
def norm(src):
    src = re.sub(r'(?:\$@|@\$|\$|@)?"(?:""|\\.|[^"\\\n])*"', '""', src)
    src = re.sub(r'/\*.*?\*/', '', src, flags=re.S)
    src = re.sub(r'//[^\n]*', '', src)
    src = re.sub(r'<!--.*?-->', '', src, flags=re.S)
    return re.sub(r'\s+', ' ', src).strip()
names = subprocess.run(["git", "diff", "--name-only", "--diff-filter=M", base, "--", "*.cs", "*.axaml"],
                       capture_output=True, text=True, check=True).stdout.split()
old = lambda n: subprocess.run(["git", "show", f"{base}:{n}"], capture_output=True, text=True, check=True).stdout
bad = [n for n in names if norm(old(n)) != norm(open(n, encoding="utf-8").read())]
print("\n".join(f"CODE CHANGED: {n}" for n in bad) or f"OK ({len(names)} files, only comments/strings changed)")
sys.exit(1 if bad else 0)
EOF
```
  It is a heuristic. If it reports a file, compare that file by hand (`git diff HEAD~1 -- <file>`). If code outside comments and string literals really differs: **stop**.

### Step 2: Rename the docs and update references (no translation yet)
- Do: `git mv docs/SPEZIFIKATION.md docs/SPECIFICATION.md` and `git mv docs/SICHERHEIT.md docs/SECURITY-CONCEPT.md`.
- Do: replace every reference: `git grep -l -e SPEZIFIKATION.md -e SICHERHEIT.md -- ':!work/archive'`, then edit each file (`SPEZIFIKATION.md` → `SPECIFICATION.md`, `SICHERHEIT.md` → `SECURITY-CONCEPT.md`).
- Check: `git grep -n -e SPEZIFIKATION -e SICHERHEIT -- ':!work/archive'` → no output.
- Check: `python3 -I "${TMPDIR:-/tmp}/wp-bd2b179e-codecheck.py" HEAD` → `OK` (run before committing; only comments changed).
- Check: `dotnet build FamilyHome.slnx` succeeds.

### Step 3: Translate the Markdown documents
- Do: translate `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `docs/SPECIFICATION.md`, `docs/SECURITY-CONCEPT.md`, `docs/THREAT-MODEL.md` and `.github/pull_request_template.md` completely. Replace "Kap." with "ch.". Keep code blocks, commands, tables, links and anchors working.
- Check: compare the heading counts with step 1 (the renamed files under their new names) → identical.
- Check that relative links resolve:
```bash
for f in $(git ls-files '*.md' ':!work/archive'); do
  grep -oE '\]\([^)#: ]+' "$f" | sed 's/^](//' | while read -r l; do
    [ -e "$(dirname "$f")/$l" ] || echo "$f -> $l"; done; done
```
  → no output.

### Step 4: Translate `CLAUDE.md` and add the language rules
- Do: translate `CLAUDE.md` completely and put in the convention lines from "Background" exactly. Add the first two rules to `CONTRIBUTING.md`.
- Check: the heading count matches step 1.
- Check: every command in the "Commands" block is unchanged: `git diff origin/main -- CLAUDE.md | grep -E '^[+-](dotnet|scripts/)'` → no output.
- Check: `grep -c "Chat with the owner is in German" CLAUDE.md` → `1`.

### Step 5: Configuration, pipeline and deployment files
- Do: translate comments (`#`, `<!-- -->`, `//`) and string literals/messages in `.github/**`, `.devcontainer/**`, `deploy/**`, `scripts/**`, `Directory.*.props`, `*.csproj`, `.editorconfig`, `.gitleaks.toml`, `.gitignore`, `.dockerignore`, Dockerfiles and Android XML resources; `message:` in `.semgrep/**`; the message after `;` in `BannedSymbols*.txt`. In workflows, also translate step names.
- Check: no workflow or job name changed. This lists all `name:` lines of the diff:
```bash
git diff -U0 HEAD~1 -- .github/workflows | grep -E '^[+-]\s*(- )?name:'
```
  → only step names (lines starting with `- name:` that are indented under `steps:`). If a workflow-level or job-level `name:` appears: revert it.
- Check: in shell scripts and workflows, only comments, `echo`/`printf` texts and step names changed: `git diff HEAD~1 -- scripts deploy .devcontainer .github` and review every changed line.
- Check: `scripts/security-gate.sh` is green (actionlint, zizmor, hadolint, ShellCheck, Semgrep).

### Step 6: C# and AXAML
- Do: translate comments (`//`, `///`, `/* */`, `<!-- -->`) and string literals (log, exception and debug messages, test data) in `src/**` and `tests/**`. Keep `TODO Phase N` markers exactly (`TODO Phase 1: …`), translate only the text after them. Do **not** touch resx files or user-facing texts (see "Out of scope").
- Do: if a test asserts on a translated message, update the expected text in the test to match.
- Check: `python3 -I "${TMPDIR:-/tmp}/wp-bd2b179e-codecheck.py" HEAD` before committing → `OK`.
- Check: `grep -rn "TODO Phase" src | wc -l` → same number as in step 1.
- Check: `dotnet format FamilyHome.slnx --verify-no-changes` passes; `dotnet build src/FamilyHome.Client.Android -c Debug` runs without warnings; both test projects are green.

### Step 7: Look for leftover German
- Check: `git grep -nIP '[äöüÄÖÜß]' -- . ':!*.resx' ':!work/archive' ':!**/packages.lock.json'`
  → only the glossary entries in this file. Any other hit: translate it, or stop if it is a user-facing text.
- Check: `git grep -nIwE 'und|nicht|oder|wird|werden|für|mit|wenn|auch|ist' -- . ':!*.resx' ':!work/archive' ':!**/packages.lock.json' ':!work/wp-bd2b179e-*'`
  → no output. Review every hit.

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green.
- [ ] `dotnet test --project tests/FamilyHome.Shared.Tests` and `dotnet test --project tests/FamilyHome.Server.Tests` are green.
- [ ] `python3 -I "${TMPDIR:-/tmp}/wp-bd2b179e-codecheck.py" origin/main` → `OK`.
- [ ] All checks from steps 2–7 are fulfilled.
- [ ] `git diff --name-only origin/main...HEAD | grep -E '\.resx$|packages\.lock\.json$|^work/archive/|^LICENSE$'` → no output.
- [ ] No workflow or job `name:` changed (only step names, see step 5).
- [ ] Every commit is signed: `for c in $(git rev-list origin/main..HEAD); do git cat-file -p "$c" | grep -q 'BEGIN SSH SIGNATURE' || echo "UNSIGNED $c"; done` → no output. After pushing, the PR shows every commit as "Verified".

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All 10 required checks on the PR are green (identical names as before).
- [ ] Apart from `Strings.resx` and the glossary in this file, nothing German remains.
- [ ] Owner reviewed spot checks of the specification and the security concept for correct meaning.

## Stop conditions
**Stop immediately**, set `status: blocked`, write what you found into the log and ask the owner if:
- a commit cannot be signed. Never disable signing (`--no-gpg-sign`, `-c commit.gpgsign=false`);
- a check cannot really run in your environment (e.g. a Security Gate step fails for environmental reasons). Report it as "not run" with the reason; never report it as green;
- the code check reports a real change outside comments and string literals;
- a build, test, format check or the Security Gate fails and the cause is not a translated test expectation;
- a workflow or job name would change;
- you find a hard-coded user-facing German text (it should have been moved to resx by d188095c; ask before touching it);
- a document seems to contain a content error or contradiction. Do not fix it; translate it as is and ask;
- a file outside the allowed list would have to change;
- another open PR changes the same documents.

## Completion by the worker
1. Fill in the log and set `status: in-review`.
2. `git commit -s`, push, open a PR against `main`:
   - title: `Translate repository to English (WP bd2b179e)`;
   - tick "security-relevant" in the PR template (text-only changes in workflows, CODEOWNERS and the security concept) and list the commits per step;
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
   - fill in the PR template (`.github/pull_request_template.md`) completely; a list of commits is not a description;
3. Do not merge it yourself.

## Acceptance by the owner
On the PR branch: set `status: done`, `git mv work/wp-bd2b179e-translate-repo-english.md work/archive/`, then squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Hard-coded user-facing texts found (should be none):
- German identifiers found (not renamed):
- Unclear passages:
- Deviations/observations:
