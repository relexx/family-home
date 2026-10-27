#!/usr/bin/env bash
# Commit signing inside the DevContainer (docs/SICHERHEIT.md ch. 8): a dedicated SSH key that can only sign.
# It lives on the named volume "git-signing" and therefore survives rebuilds. The owner registers it on GitHub
# by hand as a "Signing Key"; the container is deliberately not allowed to register keys itself.
# Runs on every container start and is idempotent: an existing key is never replaced.
set -euo pipefail

dir="${FAMILYHOME_SIGNING_DIR:-$HOME/.signing}"
key="$dir/familyhome_signing"

email="$(git config --global user.email || true)"
if [[ -z "$email" ]]; then
  echo "Commit signing: git user.email is not set yet; set it and run: bash .devcontainer/setup-signing.sh" >&2
  exit 0
fi

# Named volumes belong to root when they are created
[[ -O "$dir" ]] || sudo chown "$(id -u):$(id -g)" "$dir"
chmod 700 "$dir"

created=false
if [[ ! -f "$key" ]]; then
  ssh-keygen -q -t ed25519 -N "" -C "familyhome-devcontainer signing" -f "$key"
  created=true
fi

printf '%s %s\n' "$email" "$(cat "$key.pub")" > "$dir/allowed_signers"
git config --global gpg.format ssh
git config --global user.signingkey "$key.pub"
git config --global gpg.ssh.allowedSignersFile "$dir/allowed_signers"
git config --global commit.gpgsign true
git config --global tag.gpgsign true

if [[ "$created" == true ]]; then
  echo "New commit signing key. Add it on GitHub once: Settings > SSH and GPG keys > New SSH key > Key type: Signing Key"
  cat "$key.pub"
fi
