---
status: open
branch:
assignee:
depends_on: [533c3095, 3399485c]  # both touch files translated here; run this package last to avoid conflicts
security_relevant: true            # touches workflows, CODEOWNERS and the security concept (text only)
---

# Translate the repository to English

## Goal
English becomes the only language for documentation, code comments, the PR template and commit messages. Today these are German. The app UI stays bilingual (German neutral, English) via resx.

## Background and decisions
Decided by the owner:
- **English only.** German documentation is replaced, not kept as a copy.
- **Scope:** all Markdown documents, `CLAUDE.md`, all comments (C#, AXAML/XML, csproj/props, YAML, Dockerfiles, shell scripts, Caddyfile, `.env.example`, `.editorconfig` and similar), the PR template, the commit message convention.

Decided for this package:
- **The meaning stays exactly the same.** This is a translation, not a rewrite. No new content, no "improvements", no shortened sections. Where the German is unclear, translate it as literally as possible and record it in the log.
- **US English**, plain style, short sentences.
- **Renamed documents:**
  - `docs/SPEZIFIKATION.md` → `docs/SPECIFICATION.md`
  - `docs/SICHERHEIT.md` → `docs/SECURITY-CONCEPT.md`
  - `docs/THREAT-MODEL.md` keeps its name.
  - All references in the repo are updated (code comments, CODEOWNERS, workflows, templates, `CLAUDE.md`, open work packages).
- **Chapter numbers stay unchanged.** "Kap. 6.3" becomes "ch. 6.3". Headings keep their numbers, only their text is translated.
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
- **`CLAUDE.md` conventions:**
  - The line `Code-Bezeichner Englisch, Kommentare und Doku Deutsch, UI-Texte nur über …` becomes: `Code identifiers, comments and documentation in English; UI texts only via Resources/Strings*.resx (German neutral, English).`
  - Add the line: `Commit messages and PR texts in English.`
  - Add the same rule to `CONTRIBUTING.md`.

## Out of scope
- **String literals in code stay unchanged** (log and exception messages, test data, holiday names, output of `scripts/security-gate.sh`). Translating them changes behavior and possibly tests; that is a separate package.
- **No identifiers change:** class, method, file and resource names stay as they are, except the two renamed docs.
- **Workflow and job names stay unchanged** (`name:` at workflow and job level). The ruleset requires checks by their names; renaming them would block every PR. Step names (`- name:` inside `steps:`) stay unchanged too.
- `Resources/Strings*.resx`, `LICENSE`, all `packages.lock.json`, `work/archive/`, the git history.
- No content changes in the specification or the security concept, even if something looks outdated. Record it in the log instead.

## Allowed files
All tracked text files **except** the ones listed under "Out of scope". In particular:
- `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CLAUDE.md`
- `docs/**` (including the two renames)
- `.github/**` (comments only in workflows and `dependabot.yml`; `CODEOWNERS`: comment and path; PR template: full text)
- `work/*.md` (open packages: only references to the renamed docs; template: references only)
- comments in `src/**`, `tests/**`, `scripts/**`, `deploy/**`, `.devcontainer/**`, `.semgrep/**` (comments and `message:`), `BannedSymbols*.txt` (message text after `;`), `Directory.*.props`, `.editorconfig`, `.gitleaks.toml`, `.gitignore`, `.dockerignore`

## Preconditions
- [ ] Packages 533c3095 and 3399485c are in `work/archive/`.
- [ ] Work happens in the DevContainer, `main` is up to date, `scripts/security-gate.sh` is green on `main`.
- [ ] There is no other open PR that changes `docs/`. If there is one: ask the owner whether to wait.

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/bd2b179e*"` and `gh pr list --state all --search "bd2b179e"` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/bd2b179e-translate-repo-english`
4. In the front matter, set `status: in-progress`, `branch: wp/bd2b179e-translate-repo-english` and `assignee`.
5. `git commit -s -m "WP bd2b179e: claimed" work/wp-bd2b179e-translate-repo-english.md && git push -u origin HEAD`

## Steps
Make **one commit per step** (`git commit -s`, English message), so that the PR stays reviewable.

### Step 1: Record the baseline
- Do: save the heading counts of the docs to compare them later:
  ```bash
  for f in README.md CONTRIBUTING.md SECURITY.md CLAUDE.md docs/SPEZIFIKATION.md docs/SICHERHEIT.md docs/THREAT-MODEL.md; do
    printf '%s %s\n' "$(grep -c '^#' "$f")" "$f"; done > /tmp/wp-bd2b179e-headings.txt
  ```

### Step 2: Rename the docs and update references (no translation yet)
- Do: `git mv docs/SPEZIFIKATION.md docs/SPECIFICATION.md` and `git mv docs/SICHERHEIT.md docs/SECURITY-CONCEPT.md`.
- Do: replace every reference: `git grep -l -e SPEZIFIKATION.md -e SICHERHEIT.md -- ':!work/archive'`, then edit each file (`SPEZIFIKATION.md` → `SPECIFICATION.md`, `SICHERHEIT.md` → `SECURITY-CONCEPT.md`).
- Check: `git grep -n -e SPEZIFIKATION -e SICHERHEIT -- ':!work/archive'` → no output.
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

### Step 4: Translate `CLAUDE.md` and add the conventions
- Do: translate `CLAUDE.md` completely and change/add the convention lines as described in "Background". Add the commit message rule to `CONTRIBUTING.md`.
- Check: the heading count matches step 1; every command in the "Commands" block is unchanged (`git diff origin/main -- CLAUDE.md | grep -E '^[+-](dotnet|scripts/)'` → no output).

### Step 5: Translate comments in configuration, pipeline and deployment files
- Do: comments only (`#`, `<!-- -->`, `//`) in `.github/**`, `.devcontainer/**`, `deploy/**`, `scripts/**`, `Directory.*.props`, `*.csproj`, `.editorconfig`, `.gitleaks.toml`, `.gitignore`, `.dockerignore`, Dockerfiles, Android XML resources; `message:` in `.semgrep/**`; the message after `;` in `BannedSymbols*.txt`.
- Check: only comment lines changed in the workflows:
  ```bash
  git diff -U0 HEAD~1 -- .github/workflows | grep -E '^[+-]' | grep -vE '^(\+\+\+|---)' | grep -vE '^[+-]\s*#'
  ```
  → only lines whose code part (before `#`) is identical on both sides. No `name:` line may appear.
- Check: `scripts/security-gate.sh` is green (actionlint, zizmor, hadolint, ShellCheck, Semgrep).

### Step 6: Translate comments in C# and AXAML
- Do: comments only (`//`, `///`, `/* */`, `<!-- -->`) in `src/**` and `tests/**`. Keep `TODO Phase N` markers exactly (`TODO Phase 1: …`), translate only the text after them.
- Check: count of TODO markers unchanged: `grep -rn "TODO Phase" src | wc -l` before and after → same number.
- Check: only comment lines changed:
  ```bash
  git diff -U0 HEAD~1 -- '*.cs' '*.axaml' | grep -E '^[+-]' | grep -vE '^(\+\+\+|---)' | grep -vE '^[+-]\s*(//|/\*|\*|<!--|-->)'
  ```
  → only lines with a trailing comment whose code part is identical on both sides.
- Check: `dotnet format FamilyHome.slnx --verify-no-changes` passes; `dotnet build src/FamilyHome.Client.Android -c Debug` runs without warnings.

### Step 7: Look for leftover German
- Check: `git grep -nIP '[äöüÄÖÜß]' -- . ':!*.resx' ':!work/archive' ':!**/packages.lock.json'`
  → only string literals from "Out of scope" remain. List the files in the log.
- Check: `git grep -nIwE 'und|nicht|oder|wird|werden|für|mit|wenn|auch' -- '*.md' ':!work/archive'` → no output (or only intended German quotes, e.g. the German UI names, noted in the log).

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green.
- [ ] `dotnet test --project tests/FamilyHome.Shared.Tests` and `dotnet test --project tests/FamilyHome.Server.Tests` are green.
- [ ] All checks from steps 2–7 are fulfilled.
- [ ] `git diff --name-only origin/main...HEAD | grep -E '\.resx$|packages\.lock\.json$|^work/archive/|^LICENSE$'` → no output.
- [ ] No job or workflow `name:` changed: `git diff origin/main...HEAD -- .github/workflows | grep -E '^[+-]\s*(- )?name:'` → no output.

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All 10 required checks on the PR are green (identical names as before).
- [ ] Owner reviewed spot checks of the specification and the security concept for correct meaning.

## Stop conditions
**Stop immediately**, set `status: blocked`, write what you found into the log and ask the owner if:
- a non-comment change seems necessary in code, workflows or configuration;
- a build, test, format check or the Security Gate fails;
- a required check would get a different name;
- a document seems to contain a content error or contradiction. Do not fix it; translate it as is and ask;
- a file outside the allowed list would have to change;
- another open PR changes the same documents.

## Completion by the worker
1. Fill in the log and set `status: in-review`.
2. `git commit -s`, push, open a PR against `main`:
   - title: `Translate documentation and comments to English (WP bd2b179e)`;
   - tick "security-relevant" in the PR template (text-only changes in workflows, CODEOWNERS and the security concept) and list the commits per step;
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
3. Do not merge it yourself.

## Acceptance by the owner
On the PR branch: set `status: done`, `git mv work/wp-bd2b179e-translate-repo-english.md work/archive/`, then squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Unclear passages and remaining German string literals:
- Deviations/observations:
