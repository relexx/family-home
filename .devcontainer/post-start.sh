#!/usr/bin/env bash
# Runs on every container start (devcontainer.json, postStartCommand). Waits until the egress firewall is active
# (.devcontainer/init-firewall.sh), so that no project code runs without it, then sets up commit signing.
set -euo pipefail

ready=/run/familyhome-firewall.ready
for _ in $(seq 60); do
  [[ -f "$ready" ]] && break
  sleep 1
done
if [[ ! -f "$ready" ]]; then
  echo "Egress firewall is not active after 60 s; see /var/log/familyhome-firewall.log" >&2
  exit 1
fi

bash "$(dirname "${BASH_SOURCE[0]}")/setup-signing.sh"
