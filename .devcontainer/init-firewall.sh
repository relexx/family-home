#!/usr/bin/env bash
# Egress firewall of the DevContainer (docs/SICHERHEIT.md ch. 8), modelled on Anthropic's reference DevContainer.
# Runs as root when the container starts (docker-compose.yml, `command`); the container user cannot run it (no sudo).
# Outbound traffic is denied by default. Allowed: loopback (incl. Docker's DNS on 127.0.0.11), the Compose network
# (db, radicale), the host only on HOST_PORTS, and the IPs of ALLOWED_DOMAINS plus GitHub's ranges.
# IPs are resolved once at start; if a CDN changes its addresses, restart the container.
# When the rules are active, the script writes READY_FILE; .devcontainer/post-start.sh waits for it.
set -euo pipefail
IFS=$'\n\t'

# adb server (README, "Auf dem Handy testen") and the local model server of the AI workers
HOST_PORTS="5037,47831"
ALLOWED_DOMAINS=(
  # Anthropic (Claude Code, login)
  api.anthropic.com claude.ai console.anthropic.com platform.claude.com
  # NuGet, .NET downloads
  api.nuget.org builds.dotnet.microsoft.com download.visualstudio.microsoft.com
  # Android SDK, Google Maven
  dl.google.com maven.google.com
  # npm, PyPI
  registry.npmjs.org pypi.org files.pythonhosted.org
  # GitHub content outside the ranges of api.github.com/meta
  objects.githubusercontent.com raw.githubusercontent.com release-assets.githubusercontent.com codeload.github.com
  # Semgrep rule registry (p/csharp etc.), used by scripts/security-gate.sh
  semgrep.dev
  # VS Code server and extensions (devcontainer.json)
  update.code.visualstudio.com vscode.download.prss.microsoft.com marketplace.visualstudio.com
  ms-dotnettools.gallerycdn.vsassets.io avaloniateam.gallerycdn.vsassets.io
  editorconfig.gallerycdn.vsassets.io anthropic.gallerycdn.vsassets.io saoudrizwan.gallerycdn.vsassets.io
  kilocode.gallerycdn.vsassets.io
)
READY_FILE=/run/familyhome-firewall.ready
LOG_FILE=/var/log/familyhome-firewall.log

exec > >(tee -a "$LOG_FILE") 2>&1
echo "[$(date -Is)] Egress firewall: start"
rm -f "$READY_FILE"

# Deny first, then open step by step, so that nothing leaks while the allow list is being built.
# Docker's nat rules for the embedded DNS are left untouched.
iptables -F
iptables -X
ipset destroy allowed 2>/dev/null || true
ipset create allowed hash:net
iptables -P INPUT DROP
iptables -P FORWARD DROP
iptables -P OUTPUT DROP
iptables -A INPUT -i lo -j ACCEPT
iptables -A OUTPUT -o lo -j ACCEPT
iptables -A INPUT -m conntrack --ctstate ESTABLISHED,RELATED -j ACCEPT
iptables -A OUTPUT -m conntrack --ctstate ESTABLISHED,RELATED -j ACCEPT

# No IPv6 at all
if ip6tables -L -n >/dev/null 2>&1; then
  ip6tables -F
  ip6tables -P INPUT DROP
  ip6tables -P FORWARD DROP
  ip6tables -P OUTPUT DROP
  ip6tables -A INPUT -i lo -j ACCEPT
  ip6tables -A OUTPUT -o lo -j ACCEPT
fi

# The host (Compose gateway and host.docker.internal) only on HOST_PORTS; this must come before the Compose network.
host_ips=("$(ip -4 route show default | awk '{print $3; exit}')")
mapfile -t -O 1 host_ips < <(getent ahostsv4 host.docker.internal | awk '{print $1}' | sort -u)
for ip in "${host_ips[@]}"; do
  [[ -n "$ip" ]] || continue
  iptables -A OUTPUT -d "$ip" -p tcp -m multiport --dports "$HOST_PORTS" -j ACCEPT
  iptables -A OUTPUT -d "$ip" -j REJECT --reject-with icmp-admin-prohibited
  echo "Host $ip: only tcp $HOST_PORTS"
done

compose_net="$(ip -4 -o addr show dev eth0 | awk '{print $4}')"
compose_net="$(python3 -c 'import ipaddress, sys; print(ipaddress.ip_interface(sys.argv[1]).network)' "$compose_net")"
iptables -A OUTPUT -d "$compose_net" -j ACCEPT
echo "Compose network $compose_net: allowed"

iptables -A OUTPUT -m set --match-set allowed dst -j ACCEPT
iptables -A OUTPUT -p tcp -j REJECT --reject-with tcp-reset
iptables -A OUTPUT -j REJECT --reject-with icmp-admin-prohibited

add_domain() {
  local ips
  ips="$(dig +short +time=3 +tries=2 A "$1" | grep -E '^[0-9]+(\.[0-9]+){3}$' || true)"
  # Fail open would be wrong, but a missing domain only means less access; the self-test below is the hard check
  if [[ -z "$ips" ]]; then echo "WARNING: $1 does not resolve, not allowed"; return 0; fi
  while read -r ip; do ipset add -exist allowed "$ip"; done <<<"$ips"
}

# GitHub first: its ranges come from api.github.com/meta
add_domain api.github.com
meta="$(curl -fsS --connect-timeout 10 https://api.github.com/meta)"
if ! jq -e '.web and .api and .git' >/dev/null <<<"$meta"; then echo "ERROR: unexpected answer from api.github.com/meta"; exit 1; fi
while read -r cidr; do
  ipset add -exist allowed "$cidr"
done < <(jq -r '(.web + .api + .git)[] | select(contains(":") | not)' <<<"$meta" | aggregate -q)

for domain in "${ALLOWED_DOMAINS[@]}"; do add_domain "$domain"; done

# Self-test: a domain outside the list must be blocked, an allowed one reachable
if curl -fsS --connect-timeout 5 -o /dev/null https://example.com 2>/dev/null; then
  echo "ERROR: example.com is reachable, the firewall does not work"; exit 1
fi
if ! curl -fsS --connect-timeout 10 -o /dev/null https://api.github.com/zen; then
  echo "ERROR: api.github.com is not reachable"; exit 1
fi

touch "$READY_FILE"
echo "[$(date -Is)] Egress firewall: active ($(ipset list allowed | grep -cE '^[0-9]') entries)"
