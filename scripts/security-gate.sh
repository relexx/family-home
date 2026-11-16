#!/usr/bin/env bash
# Security Gate (docs/SICHERHEIT.md Kap. 7) – muss vor jedem PR grün sein. Die CI führt dieselben Prüfungen aus.
#
#   scripts/security-gate.sh          alle Prüfungen
#   scripts/security-gate.sh --quick  nur .NET (Build mit Analyzern, Format, Tests, Audit) – ohne Scanner
#
# The scanners are installed in the DevContainer image (.devcontainer/Dockerfile, versions pinned there and synced with
# .github/workflows/security-lint.yml). Outside the DevContainer, only --quick works; the full gate refuses to run.
set -euo pipefail
cd "$(dirname "$0")/.."

PROJECTS=(
  src/FamilyHome.Shared
  src/FamilyHome.Server
  src/FamilyHome.Client
  src/FamilyHome.Client.Desktop
  tests/FamilyHome.Shared.Tests
  tests/FamilyHome.Server.Tests
)
TESTS=(tests/FamilyHome.Shared.Tests tests/FamilyHome.Server.Tests)

failed=()
step() { printf '\n\033[1;34m▶ %s\033[0m\n' "$1"; }
run() {
  local name="$1"; shift
  step "$name"
  if "$@"; then printf '\033[32m✔ %s\033[0m\n' "$name"; else printf '\033[31m✘ %s\033[0m\n' "$name"; failed+=("$name"); fi
}

dotnet_build() {
  for p in "${PROJECTS[@]}"; do
    dotnet restore "$p" --locked-mode >/dev/null || return 1
    dotnet build "$p" -c Release --no-restore -warnaserror -nologo -clp:ErrorsOnly || return 1
  done
}
dotnet_format() { for p in "${PROJECTS[@]}"; do dotnet format "$p" --verify-no-changes --no-restore || return 1; done; }
dotnet_tests() { for t in "${TESTS[@]}"; do dotnet test --project "$t" -c Release --no-build || return 1; done; }
dotnet_vulnerable() {
  local out
  for p in "${PROJECTS[@]}"; do
    out=$(dotnet list "$p" package --vulnerable --include-transitive 2>&1)
    if grep -q "has the following vulnerable packages" <<<"$out"; then echo "$out"; return 1; fi
  done
}

# Fails loudly if a scanner is missing, so that a skipped check is never reported as green
need() {
  command -v "$1" >/dev/null && return 0
  echo "$1 is missing: run the full gate in the DevContainer (scripts/security-gate.sh --quick works everywhere)"
  return 1
}

gitleaks_scan() {
  need gitleaks || return 1
  gitleaks git --redact --no-banner --config .gitleaks.toml . &&
  gitleaks dir --redact --no-banner --config .gitleaks.toml .
}
hadolint_scan() { need hadolint && hadolint --failure-threshold warning src/FamilyHome.Server/Dockerfile .devcontainer/Dockerfile; }
actionlint_scan() { need actionlint && need shellcheck && actionlint -color; }
shellcheck_scan() {
  need shellcheck || return 1
  mapfile -t files < <(git ls-files --cached --others --exclude-standard '*.sh')
  shellcheck "${files[@]}"
}
semgrep_scan() {
  need semgrep || return 1
  SEMGREP_SEND_METRICS=off SEMGREP_ENABLE_VERSION_CHECK=0 semgrep scan --error --metrics=off --quiet \
    --config p/csharp --config p/secrets --config p/dockerfile --config p/github-actions --config .semgrep/
}
zizmor_scan() { need zizmor && zizmor --offline .github/workflows; }

run ".NET-Build mit Analyzern (Warnungen = Fehler)" dotnet_build
run "Formatierung"                                 dotnet_format
run "Tests inkl. Sicherheitstests"                 dotnet_tests
run "Verwundbare Pakete (inkl. transitiv)"         dotnet_vulnerable

if [[ "${1:-}" != "--quick" ]]; then
  run "Secrets (gitleaks: Verlauf + Arbeitskopie)"  gitleaks_scan
  run "Semgrep (C#, Secrets, Docker, Actions)"     semgrep_scan
  run "Workflows (actionlint)"                     actionlint_scan
  run "Workflows (zizmor)"                         zizmor_scan
  run "Dockerfiles (hadolint)"                     hadolint_scan
  run "Shell-Skripte (ShellCheck)"                 shellcheck_scan
fi

echo
if ((${#failed[@]})); then
  printf '\033[31mSecurity Gate NICHT bestanden:\033[0m\n'; printf '  - %s\n' "${failed[@]}"; exit 1
fi
printf '\033[32mSecurity Gate bestanden.\033[0m\n'
