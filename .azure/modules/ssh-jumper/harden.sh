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

# Microsoft Defender for Endpoint: real-time protection, behaviour monitoring and a
# daily low-priority quick scan at 04:00 local time. The daemon reloads this file
# on its own; no restart is needed.
install -d -m 0755 /etc/opt/microsoft/mdatp/managed
cat > /etc/opt/microsoft/mdatp/managed/mdatp_managed.json <<'MDATP'
{
  "antivirusEngine": {
    "enforcementLevel": "real_time",
    "behaviorMonitoring": "enabled",
    "scheduledScan": "enabled"
  },
  "scheduledScan": {
    "dailyConfiguration": { "timeOfDay": 240 },
    "lowPriorityScheduledScan": true
  }
}
MDATP
chmod 0644 /etc/opt/microsoft/mdatp/managed/mdatp_managed.json

# The agent is auto-provisioned by Defender for Servers and may not be installed
# yet on a new machine; it applies the file on install. When it is present, wait
# for the settings to take effect and report the outcome. This is reported, not
# fatal: a jumper hardening step must not block an infrastructure deployment.
if command -v mdatp >/dev/null 2>&1; then
  for _ in $(seq 1 18); do
    [ "$(mdatp health --field real_time_protection_enabled 2>/dev/null)" = "true" ] && break
    sleep 5
  done
  if [ "$(mdatp health --field real_time_protection_enabled 2>/dev/null)" = "true" ]; then
    echo "defender: managed configuration applied (version $(mdatp health --field app_version 2>/dev/null))"
  else
    echo "warning: defender is installed but real-time protection is not enabled after 90s (version $(mdatp health --field app_version 2>/dev/null))" >&2
  fi
else
  echo "defender: agent not installed yet; configuration will apply when Defender for Servers provisions it"
fi
