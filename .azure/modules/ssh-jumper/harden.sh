#!/usr/bin/env bash
# Baseline hardening for the SSH jumper. Idempotent; applied by an Azure VM run command on deploy.
set -euo pipefail

# Keep credentials out of shell and database client history files.
cat > /etc/profile.d/99-jumper-history.sh <<'PROFILE'
export HISTCONTROL=ignoreboth
export HISTIGNORE='*PGPASSWORD*:*psql *:*password*:*Password=*:*redis-cli*:*REDISCLI_AUTH*'
export PSQL_HISTORY=/dev/null
export REDISCLI_HISTFILE=/dev/null
PROFILE
chmod 0644 /etc/profile.d/99-jumper-history.sh

# Patching is handled by the assigned maintenance configuration. Keep the
# distribution's unattended-upgrades off so two updaters never compete for apt.
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'APT'
APT::Periodic::Update-Package-Lists "0";
APT::Periodic::Unattended-Upgrade "0";
APT
