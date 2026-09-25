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

# Package upgrades run unattended in the maintenance window. Keep configuration
# files that were changed locally and take new defaults only for unchanged ones,
# so an upgrade never stops at an interactive configuration file prompt.
cat > /etc/dpkg/dpkg.cfg.d/50-jumper-keep-local-conffiles <<'DPKG'
force-confdef
force-confold
DPKG
chmod 0644 /etc/dpkg/dpkg.cfg.d/50-jumper-keep-local-conffiles

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

# SSH server hardening as a drop-in. sshd_config.d is included at the top of
# sshd_config and the first value wins, so these settings take precedence.
# The config is validated before sshd is reloaded; a reload keeps existing
# sessions open, so a mistake here never cuts off the session fixing it.
# The pre-login banner is a file of our own: the distribution's /etc/issue.net
# names the OS release.
cat > /etc/ssh/jumper-banner <<'BANNER'
Authorised access only. Activity on this system is logged.
BANNER
chmod 0644 /etc/ssh/jumper-banner
cat > /etc/ssh/sshd_config.d/10-jumper-hardening.conf <<'SSHD'
Ciphers aes128-ctr,aes192-ctr,aes256-ctr
MACs hmac-sha2-512-etm@openssh.com,hmac-sha2-256-etm@openssh.com,hmac-sha2-512,hmac-sha2-256
LoginGraceTime 60
PermitRootLogin no
Banner /etc/ssh/jumper-banner
AllowUsers *@*
DenyUsers root
# Do not set global AllowGroups or DenyGroups: both block first-time Entra login.
# https://learn.microsoft.com/en-us/entra/identity/devices/howto-vm-sign-in-azure-ad-linux
SSHD
# The baseline scanner looks for a literal 'Protocol 2' line in sshd_config, or in
# this specific file when sshd_config includes it by name, directly below the
# '# Azure OSConfig Remediation' header line (the wildcard include is not
# recognised). The directive is a no-op on OpenSSH 7.4 and later.
cat > /etc/ssh/sshd_config.d/osconfig_remediation.conf <<'SSHD'
Protocol 2
SSHD
if ! grep -Pzq '\n# Azure OSConfig Remediation\nInclude /etc/ssh/sshd_config\.d/osconfig_remediation\.conf\n' /etc/ssh/sshd_config; then
  if grep -qx 'Include /etc/ssh/sshd_config.d/osconfig_remediation.conf' /etc/ssh/sshd_config; then
    sed -i '/^Include \/etc\/ssh\/sshd_config\.d\/osconfig_remediation\.conf$/i # Azure OSConfig Remediation' /etc/ssh/sshd_config
  else
    sed -i '/^Include \/etc\/ssh\/sshd_config\.d\/\*\.conf$/a # Azure OSConfig Remediation\nInclude /etc/ssh/sshd_config.d/osconfig_remediation.conf' /etc/ssh/sshd_config
  fi
fi
chmod 0600 /etc/ssh/sshd_config /etc/ssh/sshd_config.d/10-jumper-hardening.conf /etc/ssh/sshd_config.d/osconfig_remediation.conf
if sshd_check=$(sshd -t 2>&1); then
  systemctl reload ssh
  echo "sshd: hardening drop-in applied and sshd reloaded"
else
  rm -f /etc/ssh/sshd_config.d/10-jumper-hardening.conf /etc/ssh/sshd_config.d/osconfig_remediation.conf
  echo "warning: sshd rejected the hardening drop-in, removed it and left sshd unchanged: ${sshd_check}" >&2
fi

