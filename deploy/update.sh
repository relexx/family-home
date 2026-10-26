#!/usr/bin/env bash
# Verifiziertes Server-Update mit Rollback (docs/SICHERHEIT.md Kap. 5.2).
# Gedacht für einen täglichen systemd-Timer auf dem Host. Benötigt: docker, cosign, curl.
#
#  1. Neuesten Digest von :latest ermitteln
#  2. cosign-Signatur prüfen: muss aus GENAU diesem Repo und dem Container-Workflow auf main stammen
#  3. Backup der DB, Rollout, Healthcheck
#  4. Bei Fehlschlag: zurück auf den vorherigen Digest
set -euo pipefail
cd "$(dirname "$0")"

# shellcheck disable=SC1091
source .env
: "${FAMILYHOME_REPO:?}" "${FAMILYHOME_IMAGE_NAME:?}"

IDENTITY="https://github.com/${FAMILYHOME_REPO}/.github/workflows/container.yml@refs/heads/main"
ISSUER="https://token.actions.githubusercontent.com"

previous="${FAMILYHOME_IMAGE:-}"
docker pull --quiet "${FAMILYHOME_IMAGE_NAME}:latest" > /dev/null
digest=$(docker image inspect --format '{{index .RepoDigests 0}}' "${FAMILYHOME_IMAGE_NAME}:latest")

if [[ "$digest" == "$previous" ]]; then
  echo "Bereits aktuell: $digest"
  exit 0
fi

echo "Prüfe Signatur von $digest …"
cosign verify --certificate-identity "$IDENTITY" --certificate-oidc-issuer "$ISSUER" "$digest" > /dev/null
echo "Signatur gültig."

echo "Backup …"
mkdir -p backups
docker compose exec -T db pg_dump -U familyhome_owner -Fc familyhome > "backups/familyhome-$(date +%Y%m%d-%H%M%S).dump"
# TODO Phase 1: Backup mit restic verschlüsselt außer Haus kopieren, alte lokale Dumps rotieren

set_image() { sed -i "s#^FAMILYHOME_IMAGE=.*#FAMILYHOME_IMAGE=$1#" .env; }

set_image "$digest"
docker compose up -d server

echo "Warte auf Healthcheck …"
for _ in $(seq 1 30); do
  status=$(docker inspect --format '{{.State.Health.Status}}' "$(docker compose ps -q server)" 2>/dev/null || echo starting)
  if [[ "$status" == "healthy" ]]; then
    echo "Update erfolgreich: $digest"
    exit 0
  fi
  sleep 5
done

echo "Healthcheck fehlgeschlagen – Rollback auf $previous" >&2
if [[ -n "$previous" ]]; then
  set_image "$previous"
  docker compose up -d server
fi
exit 1
