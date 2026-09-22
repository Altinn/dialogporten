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

# Network hardening: no ICMP redirects in either direction, strict reverse-path
# filtering and logging of packets with impossible source addresses. Each
# interface has its own value that the kernel also honours, so the glob lines set
# every interface, including ones udev adds later. Applied with systemd-sysctl as
# at boot; existing connections are unaffected.
cat > /etc/sysctl.d/60-jumper-network.conf <<'SYSCTL'
net.ipv4.conf.all.send_redirects = 0
net.ipv4.conf.default.send_redirects = 0
net.ipv4.conf.*.send_redirects = 0
net.ipv4.conf.all.accept_redirects = 0
net.ipv4.conf.default.accept_redirects = 0
net.ipv4.conf.*.accept_redirects = 0
net.ipv6.conf.all.accept_redirects = 0
net.ipv6.conf.default.accept_redirects = 0
net.ipv6.conf.*.accept_redirects = 0
net.ipv4.conf.all.secure_redirects = 0
net.ipv4.conf.default.secure_redirects = 0
net.ipv4.conf.*.secure_redirects = 0
net.ipv4.conf.all.log_martians = 1
net.ipv4.conf.default.log_martians = 1
net.ipv4.conf.all.rp_filter = 1
net.ipv4.conf.default.rp_filter = 1
net.ipv4.conf.*.rp_filter = 1
SYSCTL
chmod 0644 /etc/sysctl.d/60-jumper-network.conf
/lib/systemd/systemd-sysctl /etc/sysctl.d/60-jumper-network.conf
echo "sysctl: network hardening applied"

# Filesystems, network protocols and USB storage the jumper never uses: prevent
# the kernel modules from being loaded.
cat > /etc/modprobe.d/10-jumper-blacklist.conf <<'MODPROBE'
install cramfs /bin/true
install freevxfs /bin/true
install hfs /bin/true
install hfsplus /bin/true
install jffs2 /bin/true
install dccp /bin/true
install sctp /bin/true
install rds /bin/true
install tipc /bin/true
install usb-storage /bin/true
MODPROBE
chmod 0644 /etc/modprobe.d/10-jumper-blacklist.conf
# A module that is in use stays loaded until the next reboot; this is reported,
# not fatal.
still_loaded=""
for module in cramfs freevxfs hfs hfsplus jffs2 dccp sctp rds tipc usb_storage; do
  if [ -e "/sys/module/${module}/initstate" ]; then
    modprobe -r "$module" 2>/dev/null || true
    if [ -e "/sys/module/${module}/initstate" ]; then
      still_loaded="${still_loaded} ${module}"
    fi
  fi
done
if [ -n "$still_loaded" ]; then
  echo "warning: blocked from loading, but still loaded until the next reboot:${still_loaded}" >&2
else
  echo "modprobe: unused filesystem and protocol modules disabled"
fi
# Restrict access to credential stores, logger configuration and cron directories
# to root.
chmod 0400 /etc/shadow /etc/shadow- /etc/gshadow /etc/gshadow-
chmod 0640 /etc/rsyslog.conf
chmod 0700 /etc/cron.d /etc/cron.daily /etc/cron.hourly /etc/cron.weekly /etc/cron.monthly

# No core dumps: a dump of a client process can contain credentials. The
# wildcard entry does not apply to root, so root has its own.
cat > /etc/security/limits.d/10-jumper-core.conf <<'LIMITS'
* hard core 0
root hard core 0
LIMITS
chmod 0644 /etc/security/limits.d/10-jumper-core.conf
# Ubuntu's crash reporter sets fs.suid_dumpable = 2 when it starts at boot. The
# jumper has no use for crash reports, so keep it disabled; stopping it resets
# suid_dumpable and core_pattern.
if [ -f /etc/default/apport ]; then
  sed -i 's/^enabled=1$/enabled=0/' /etc/default/apport
  systemctl stop apport
fi
# The baseline scanner reads this setting from /etc/sysctl.d/99-sysctl.conf by name.
grep -qx 'fs.suid_dumpable = 0' /etc/sysctl.d/99-sysctl.conf || echo 'fs.suid_dumpable = 0' >> /etc/sysctl.d/99-sysctl.conf
sysctl -q -w fs.suid_dumpable=0

# Default umask for login sessions: files created by users are private to them.
sed -i -E 's/^(UMASK\s+)[0-7]+/\1077/' /etc/login.defs

# su only for members of the root group. Root itself passes through pam_rootok
# first, so sudo su keeps working.
if grep -qE '^auth\s+required\s+pam_wheel\.so' /etc/pam.d/su; then
  sed -i -E '/^auth\s+required\s+pam_wheel\.so/ { /group=root/! s/$/ group=root/ }' /etc/pam.d/su
else
  sed -i '/^auth\s\+sufficient\s\+pam_rootok\.so/a auth       required   pam_wheel.so use_uid group=root' /etc/pam.d/su
fi
echo "perms: credential store, cron, logger, core dump, umask and su restrictions applied"
