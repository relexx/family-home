---
status: open
branch:
assignee:
depends_on: []
security_relevant: true  # Security Gate (pipeline tooling)
---

# Make the Security Gate work inside the DevContainer

## Goal
`scripts/security-gate.sh` runs green inside the DevContainer. If it cannot see the repository, it fails loudly instead of reporting a false green.

## Background and decisions
- The DevContainer uses the feature `docker-outside-of-docker`: the Docker daemon runs **on the host**. The gate starts gitleaks, Semgrep, actionlint, hadolint and ShellCheck in containers with `-v "$PWD:/src:ro"`. `$PWD` is the path *inside* the DevContainer (`/workspaces/FamilyHome`), which does not exist on the host. Docker then mounts an empty directory, the tools find no files, and five checks fail.
- Worse: with `-v`, Docker silently creates a missing source directory. A tool that accepts an empty directory would report green although it checked nothing.
- **Decision:**
  1. `devcontainer.json` sets `remoteEnv.LOCAL_WORKSPACE_FOLDER` to `${localWorkspaceFolder}`, the host path of the repo. This is the approach documented by the `docker-outside-of-docker` feature.
  2. The script mounts `LOCAL_WORKSPACE_FOLDER` if it is set. Inside a container (`/.dockerenv` exists) without that variable, the docker-based checks fail with a clear message. Otherwise (e.g. directly on a host or a CI runner) `$PWD` is used as before.
  3. The mount uses `--mount type=bind,…` instead of `-v`. `--mount` fails if the source path does not exist, so a wrong path can never produce an empty mount.
- The host path is personal (it contains the user name). It must never be written into the repo, the PR or the log; use `<host path>` instead.
- **Language:** everything written into the repo is English, including the snippets below, even where the surrounding file is still German (work package bd2b179e translates the rest later). Copy the snippets exactly as written.

## Out of scope
- No change to tool versions/images, to the list of checks or to `.github/workflows/security-lint.yml`.
- No change to the .NET part of the gate.
- No egress firewall work.

## Allowed files
- `scripts/security-gate.sh`
- `.devcontainer/devcontainer.json`
- this work package file

## Preconditions
- [ ] Work happens in the DevContainer, `main` is up to date.
- [ ] `test -f /.dockerenv && echo container` prints `container`.
- [ ] `docker info` works (docker-outside-of-docker is active).

## Claiming
1. `git fetch origin --prune`
2. `git branch -a --list "*wp/533c3095*"` and `gh pr list --state all --search "533c3095"` must both be empty, and `status` must be `open`. Otherwise **do not start**.
3. `git switch -c wp/533c3095-gate-devcontainer-mounts`
4. In the front matter, set `status: in-progress`, `branch: wp/533c3095-gate-devcontainer-mounts` and `assignee`.
5. `git commit -s -m "WP 533c3095: claimed" work/wp-533c3095-gate-devcontainer-mounts.md && git push -u origin HEAD`

## Steps

### Step 1: Reproduce the problem
- Check: `scripts/security-gate.sh 2>&1 | tail -8`
  → fails, the list of failed checks includes at least `Dockerfiles (hadolint)` and `Shell-Skripte (ShellCheck)` with messages like `does not exist`.
  If the gate is already green: **stop** (the problem is gone or different).

### Step 2: `devcontainer.json`
- Do: in `.devcontainer/devcontainer.json`, add after the line `"postCreateCommand": …,`:
  ```jsonc
    // Host path of the repo: docker-outside-of-docker needs it for bind mounts (scripts/security-gate.sh)
    "remoteEnv": { "LOCAL_WORKSPACE_FOLDER": "${localWorkspaceFolder}" },
  ```
- Check: no other key changes (`git diff .devcontainer/devcontainer.json` shows only these 2 added lines).

### Step 3: `scripts/security-gate.sh`
- Do: replace the line
  ```bash
  docker_run() { docker run --rm -v "$PWD:/src:ro" -w /src "$@"; }
  ```
  with:
  ```bash
  # Source for bind mounts: inside the DevContainer (docker-outside-of-docker) the Docker daemon runs on the host and
  # does not know the container path; devcontainer.json therefore sets LOCAL_WORKSPACE_FOLDER to the host path.
  mount_source() {
    if [[ -n "${LOCAL_WORKSPACE_FOLDER:-}" ]]; then printf '%s\n' "$LOCAL_WORKSPACE_FOLDER"
    elif [[ -f /.dockerenv ]]; then
      echo "LOCAL_WORKSPACE_FOLDER is not set: inside a container \$PWD is not a host path (rebuild the DevContainer)" >&2; return 1
    else printf '%s\n' "$PWD"; fi
  }
  # --mount instead of -v: fails if the path is missing on the host instead of mounting an empty directory (false greens)
  docker_run() {
    local src; src=$(mount_source) || return 1
    docker run --rm --mount "type=bind,source=$src,target=/src,readonly" -w /src "$@"
  }
  ```
- Check: `grep -n -- '-v "\$PWD' scripts/security-gate.sh` → no output.

### Step 4: Test (positive)
The new `remoteEnv` only takes effect after rebuilding the container, which the worker cannot do. So set the variable by hand for the test:
```bash
export LOCAL_WORKSPACE_FOLDER="$(docker inspect "$(hostname)" --format '{{range .Mounts}}{{if eq .Destination "/workspaces/FamilyHome"}}{{.Source}}{{end}}{{end}}')"
test -n "$LOCAL_WORKSPACE_FOLDER" && echo set
scripts/security-gate.sh
```
→ `set`, then `Security Gate bestanden.` If the variable stays empty: **stop**.

### Step 5: Tests (negative)
- `env -u LOCAL_WORKSPACE_FOLDER scripts/security-gate.sh 2>&1 | tail -10`
  → **not** passed; the output contains `LOCAL_WORKSPACE_FOLDER is not set` and all 5 docker-based checks are listed as failed (gitleaks, Semgrep, actionlint, hadolint, ShellCheck). zizmor stays green (it does not use Docker).
- `LOCAL_WORKSPACE_FOLDER=/does/not/exist scripts/security-gate.sh 2>&1 | tail -10`
  → **not** passed; the 5 docker-based checks fail (Docker reports that the bind source path does not exist).

## Checks (before the PR, all must pass)
- [ ] Step 4 is green, steps 5 fail as described.
- [ ] ShellCheck is part of the green run in step 4 (it checks the modified script).
- [ ] `git diff --name-only origin/main...HEAD` lists only allowed files.
- [ ] Neither the diff, the PR text nor the log contains the real host path (`git diff origin/main...HEAD | grep -c "/home/"` → `0`).

## Acceptance criteria ("done")
- [ ] All checks passed.
- [ ] All required checks on the PR are green.
- [ ] Owner: after rebuilding the DevContainer, `echo "$LOCAL_WORKSPACE_FOLDER"` prints the host path and `scripts/security-gate.sh` passes without setting anything by hand.

## Stop conditions
**Stop immediately**, set `status: blocked`, write what you found into the log and ask the owner if:
- step 1 does not reproduce the problem;
- the variable from step 4 stays empty or the gate is not green in step 4;
- a negative test in step 5 passes (false green);
- a check reports a real finding in the repo (not caused by the mount problem). Do not fix it here; report it;
- a file outside the allowed list would have to change.

## Completion by the worker
1. Fill in the log and set `status: in-review`.
2. `git commit -s`, push, open a PR against `main`:
   - title: `Security Gate: correct bind mounts inside the DevContainer (WP 533c3095)`;
   - tick "security-relevant" (pipeline tooling) in the PR template and mention the false-green risk that `--mount` removes;
   - if the worker is a Claude model, the PR text ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.
3. Do not merge it yourself.

## Acceptance by the owner
Rebuild the DevContainer and check the last acceptance criterion. Then, on the PR branch: set `status: done`, `git mv work/wp-533c3095-gate-devcontainer-mounts.md work/archive/`, squash-merge.

## Log (filled in by the worker)
- Date/assignee:
- PR:
- Output of the checks (shortened):
- Deviations/observations:
