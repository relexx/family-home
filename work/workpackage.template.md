---
status: open             # open | in-progress | blocked | in-review | done (only the owner sets done)
branch:                  # wp/<work-id>-<short-name>, filled in when claiming
assignee:                # name/model of the worker, filled in when claiming
depends_on: []           # work IDs that must be done first
security_relevant: false # true if auth, permissions, crypto, pipeline or deployment are touched
---

# <Title of the work package>

> File name: `work/wp-<work-id>-<short-name>.md`. The work ID is 8 hex characters (`openssl rand -hex 4`).
> Before creating it, check that the ID exists neither in `work/` nor in `work/archive/`.

## Language
Everything written into the repo is English: code, string literals, comments, documentation, commit messages, PR texts
and the log below. This also applies where surrounding files are still German. The app UI supports English and German;
user-facing texts go only into `Resources/Strings*.resx` (German neutral, English), never hard-coded.

## Goal
What is different once this is done, in 1–3 sentences, and why it is needed.

## Background and decisions
Only what the worker needs to know so that they don't have to decide anything themselves. Decisions that have been
made go here, with their reasons. Open decisions do not belong in a work package; they are settled beforehand.

## Out of scope
- …

## Allowed files
Only these files may be changed or created in the PR (plus this work package file itself):
- `…`

If anything else has to change → **stop** (see below).

## Preconditions
- [ ] All packages listed in `depends_on` are in `work/archive/`.
- [ ] Work happens in the DevContainer, `main` is up to date (`git switch main && git pull --ff-only`).
- [ ] …

## Claiming
1. `git fetch origin --prune`
2. Check whether the branch already exists: `git branch -a --list "*wp/<work-id>*"`, and also `gh pr list --state all --head wp/<work-id>-<short-name>`.
   (Do not search for the work ID in PR texts: the PR that created the package mentions it too.)
   If either finds something, or `status` is not `open` → **do not start**, tell the owner.
3. `git switch -c wp/<work-id>-<short-name>`
4. In the front matter, set `status: in-progress` and fill in `branch` and `assignee`.
5. `git commit -s -am "WP <work-id>: claimed"` and `git push -u origin HEAD`. Only the pushed branch counts as a claim.

## Steps
Each step: what to do, the exact command or change, and the expected result.
Code blocks are written exactly as they go into the file or terminal, including leading spaces; copy the raw text unchanged.
When writing a package: never indent code blocks under list items, and exclude `work/` from checks that grep the diff (the log mentions the check itself).

### Step 1: …
- Do: …
- Check: `…` → expected: …

## Checks (before the PR, all must pass)
- [ ] `scripts/security-gate.sh` is green.
- [ ] `git diff --name-only origin/main...HEAD` lists only allowed files.
- [ ] Every commit is signed: `for c in $(git rev-list origin/main..HEAD); do git cat-file -p "$c" | grep -q 'BEGIN SSH SIGNATURE' || echo "UNSIGNED $c"; done` → no output. After pushing, the PR shows every commit as "Verified".
- [ ] …

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All required checks on the PR are green.
- [ ] …

## Stop conditions
If any of the following happens, **stop immediately**: set `status: blocked`, write what you found into the log and ask the owner. Do not improvise, weaken or suppress anything:
- A commit cannot be signed. Never disable signing (`--no-gpg-sign`, `-c commit.gpgsign=false`).
- A check cannot really run in your environment (e.g. a Security Gate step fails for environmental reasons). Report it as "not run" with the reason; never report it as green.
- A change outside the allowed files would be needed.
- A command gives a different result than expected.
- The Security Gate or a CI check is red and the cause is not obviously a typo of your own.
- The task would deviate from the specification or the security concept in `docs/`.
- …

## Completion by the worker
1. Fill in the log, set `status: in-review`.
2. Commit with `git commit -s`, push, open a PR against `main` that references this file. If the package is security-relevant, mark the PR accordingly.
   Fill in the PR template (`.github/pull_request_template.md`) completely; a list of commits is not a description.
3. Do not merge it yourself.

## Acceptance by the owner
On the PR branch: set `status: done`, `git mv work/wp-<work-id>-<short-name>.md work/archive/`, then squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Deviations/observations:
