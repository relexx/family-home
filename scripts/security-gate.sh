#!/usr/bin/env bash
# Security Gate (docs/SICHERHEIT.md Kap. 7) – muss vor jedem PR grün sein. Die CI führt dieselben Prüfungen aus.
#
#   scripts/security-gate.sh          alle Prüfungen
#   scripts/security-gate.sh --quick  nur .NET (Build mit Analyzern, Format, Tests, Audit) – ohne Docker
#
# Externe Werkzeuge laufen in gepinnten Containern; Versionen hier und in .github/workflows/security-lint.yml synchron halten.
set -euo pipefail
cd "$(dirname "$0")/.."

GITLEAKS_IMAGE="zricethezav/gitleaks:v8.30.1"
HADOLINT_IMAGE="hadolint/hadolint:v2.15.1"
ACTIONLINT_IMAGE="rhysd/actionlint:1.7.12"
SHELLCHECK_IMAGE="koalaman/shellcheck:v0.11.0"
SEMGREP_IMAGE="semgrep/semgrep:1.182.1"
ZIZMOR_VERSION="1.31.1"

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
    CI=true dotnet restore "$p" >/dev/null || return 1
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

docker_run() { docker run --rm -v "$PWD:/src:ro" -w /src "$@"; }

gitleaks_scan() {
  docker_run "$GITLEAKS_IMAGE" git --redact --no-banner --config /src/.gitleaks.toml /src &&
  docker_run "$GITLEAKS_IMAGE" dir --redact --no-banner --config /src/.gitleaks.toml /src
}
hadolint_scan() { docker_run "$HADOLINT_IMAGE" hadolint --failure-threshold warning src/FamilyHome.Server/Dockerfile .devcontainer/Dockerfile; }
actionlint_scan() { docker_run "$ACTIONLINT_IMAGE" -color; }
shellcheck_scan() { mapfile -t files < <(git ls-files --cached --others --exclude-standard '*.sh'); docker_run "$SHELLCHECK_IMAGE" "${files[@]}"; }
semgrep_scan() {
  docker_run -e SEMGREP_SEND_METRICS=off "$SEMGREP_IMAGE" semgrep scan --error --metrics=off --quiet \
    --config p/csharp --config p/secrets --config p/dockerfile --config p/github-actions --config .semgrep/
}
zizmor_scan() {
  if command -v pipx >/dev/null; then pipx run "zizmor==$ZIZMOR_VERSION" --offline .github/workflows
  elif command -v uvx >/dev/null; then uvx "zizmor@$ZIZMOR_VERSION" --offline .github/workflows
  else echo "pipx oder uvx fehlt (im DevContainer vorhanden)"; return 1; fi
}

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
