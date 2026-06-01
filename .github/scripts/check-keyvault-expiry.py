"""Report soon-to-expire secrets in the source Key Vaults, to Slack and the step summary.

Secrets are rotated in two source vaults — one non-prod (serving test/yt01/staging) and
one prod — and their expiry dates live there. This script checks those vaults, grouped as
the `non-prod` and `prod` tiers.

Reads:
  - $MONITORED_SECRETS_FILE: YAML `required_expires:` list of secrets that must have an
    expiry set; a missing one is flagged as a config error.
  - $SECRETS_DIR: one `secrets-<tier>.json` per tier, each a list of
    {"name": ..., "expires": ISO8601 or null} for the enabled secrets in that tier's vault.
  - $EXPECTED_TIERS_JSON: tiers the workflow meant to check (used to detect failed legs).
  - $GITHUB_RUN_URL / $RUNBOOK_URL: links embedded in Slack messages.

Writes:
  - slack-{nonprod,prod,prod-critical}.json: chat.postMessage payloads, one per channel
    that has something to report.
  - slack-{nonprod,prod}-digest.json: a full overview of every tracked secret expiry (no
    @mention), written when $SEND_DIGEST is true or it's the 1st of the month. Informational
    only — does not affect the run's pass/fail.
  - $GITHUB_STEP_SUMMARY: markdown table (tier, secret, expires, days left).
  - $GITHUB_OUTPUT: `any_rule_tripped` + per-channel `have_*_payload` / `have_*_digest` flags.

Per-finding routing by days until expiry: 8-30 -> no mention; 2-7 -> @here;
<=1 or expired -> @channel (prod also cross-posts to the prod-critical channel).
Config-error findings post at @here with no escalation.
"""

from __future__ import annotations

import json
import os
import pathlib
import sys
from datetime import datetime, timezone

import yaml


MONITORED_FILE = pathlib.Path(os.environ["MONITORED_SECRETS_FILE"])
SECRETS_DIR = pathlib.Path(os.environ["SECRETS_DIR"])
RUN_URL = os.environ.get("GITHUB_RUN_URL", "")
RUNBOOK_URL = os.environ.get("RUNBOOK_URL", "")
SEND_DIGEST = os.environ.get("SEND_DIGEST", "").lower() == "true"
EXPECTED_TIERS = json.loads(os.environ.get("EXPECTED_TIERS_JSON", "[]"))
STEP_SUMMARY = pathlib.Path(os.environ["GITHUB_STEP_SUMMARY"])
STEP_OUTPUT = pathlib.Path(os.environ["GITHUB_OUTPUT"])

CHANNEL_PLACEHOLDER = "${{ env.CHANNEL_ID }}"  # filled in by slack-github-action's payload-templated

PROD_TIER = "prod"


def parse_iso(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def classify_tier(days_left: int) -> int | None:
    if days_left <= 1:
        return 3
    if days_left <= 7:
        return 2
    if days_left <= 30:
        return 1
    return None


def days_phrase(days: int) -> str:
    if days < 0:
        n = abs(days)
        return f"expired {n} day{'s' if n != 1 else ''} ago"
    if days == 0:
        return "expires today"
    return f"{days} day{'s' if days != 1 else ''} left"


def tier_icon(tier: int) -> str:
    return {1: ":large_yellow_circle:", 2: ":large_orange_diamond:", 3: ":red_circle:"}[tier]


def status_icon(days_left: int) -> str:
    tier = classify_tier(days_left)
    return tier_icon(tier) if tier is not None else ":large_green_circle:"


def footer_blocks() -> list[dict]:
    """Runbook button + workflow-run link, shared by alert and digest payloads."""
    blocks: list[dict] = []
    if RUNBOOK_URL:
        blocks.append({
            "type": "actions",
            "elements": [{
                "type": "button",
                "text": {"type": "plain_text", "text": "Open rotation runbook"},
                "url": RUNBOOK_URL,
                "style": "primary",
            }],
        })
    if RUN_URL:
        blocks.append({
            "type": "context",
            "elements": [{"type": "mrkdwn", "text": f"<{RUN_URL}|View workflow run>"}],
        })
    return blocks


def load_monitored() -> set[str]:
    data = yaml.safe_load(MONITORED_FILE.read_text()) or {}
    return set(data.get("required_expires", []) or [])


def matches_monitored(secret_name: str, canonical_key: str) -> bool:
    """True if a source secret is the monitored canonical key, optionally carrying a
    single documented source prefix: `dialogporten--<env>--` or `dialogporten--any--`
    (Configuration.md). Names with extra leading segments, or without the dialogporten
    prefix, are rejected so an unrelated secret that merely ends with the key cannot
    satisfy the rule."""
    if secret_name == canonical_key:
        return True
    suffix = f"--{canonical_key}"
    if not secret_name.endswith(suffix):
        return False
    prefix = secret_name[: -len(suffix)]
    if not prefix.startswith("dialogporten--"):
        return False
    env = prefix[len("dialogporten--"):]
    # the env segment must be exactly one `--`-delimited token (e.g. any, test, yt01)
    return env != "" and "--" not in env


def collect_findings() -> tuple[list[dict], list[dict], list[str]]:
    """Scan every `secrets-<tier>.json` in $SECRETS_DIR.

    Returns (expiry_findings, missing_findings, present_tiers), where present_tiers
    lists the tiers that actually produced a file.
    """
    monitored = load_monitored()
    today = datetime.now(timezone.utc).date()

    expiry_findings: list[dict] = []
    missing_findings: list[dict] = []
    tiers: list[str] = []

    for f in sorted(SECRETS_DIR.glob("secrets-*.json")):
        tier_name = f.stem[len("secrets-"):]
        tiers.append(tier_name)
        secrets = json.loads(f.read_text())

        for s in secrets:
            if not s.get("expires"):
                continue
            days = (parse_iso(s["expires"]).date() - today).days
            tier = classify_tier(days)
            expiry_findings.append({
                "tier": tier_name,
                "name": s["name"],
                "expires": s["expires"],
                "days": days,
                "alert_tier": tier,
            })

        for canonical_key in sorted(monitored):
            matches = [s for s in secrets if matches_monitored(s["name"], canonical_key)]
            if not matches:
                missing_findings.append({
                    "tier": tier_name,
                    "name": canonical_key,
                    "state": "absent",
                })
                continue
            for s in matches:
                if not s.get("expires"):
                    missing_findings.append({
                        "tier": tier_name,
                        "name": s["name"],
                        "state": "no-expires",
                    })

    return expiry_findings, missing_findings, tiers


def build_blocks(
    alerted: list[dict],
    missing: list[dict],
    missing_tiers: list[str],
    header_suffix: str = "",
) -> list[dict]:
    blocks: list[dict] = [{
        "type": "header",
        "text": {"type": "plain_text", "text": f"Key Vault secret expiry{header_suffix}"},
    }]

    if missing_tiers:
        blocks.append({
            "type": "section",
            "text": {
                "type": "mrkdwn",
                "text": (
                    ":rotating_light: *Data unavailable for: "
                    f"{', '.join(missing_tiers)}* — the matrix leg(s) for these tiers failed. "
                    "Findings below reflect only the tiers that were checked successfully."
                ),
            },
        })

    if alerted:
        rows = []
        for f in sorted(alerted, key=lambda x: (x["days"], x["tier"], x["name"])):
            rows.append(
                f"{tier_icon(f['alert_tier'])} *{f['tier']}* `{f['name']}` "
                f"— {days_phrase(f['days'])} (expires {f['expires'][:10]})"
            )
        blocks.append({
            "type": "section",
            "text": {"type": "mrkdwn", "text": "*Expiring soon:*\n" + "\n".join(rows)},
        })

    if missing:
        rows = []
        for m in sorted(missing, key=lambda x: (x["tier"], x["name"])):
            detail = (
                "no `attributes.expires` set"
                if m["state"] == "no-expires"
                else "monitored secret not found in source vault"
            )
            rows.append(f":warning: *{m['tier']}* `{m['name']}` — {detail}")
        blocks.append({
            "type": "section",
            "text": {
                "type": "mrkdwn",
                "text": "*Monitored secrets with configuration issues:*\n" + "\n".join(rows),
            },
        })

    blocks.extend(footer_blocks())
    return blocks


def build_digest_payload(findings: list[dict], header_suffix: str = "") -> dict:
    """Informational monthly overview: every tracked secret with an expiry, soonest first,
    colour-coded by runway. No @mention; does not affect the run's pass/fail."""
    rows = [
        f"{status_icon(f['days'])} *{f['tier']}* `{f['name']}` "
        f"— {days_phrase(f['days'])} (expires {f['expires'][:10]})"
        for f in sorted(findings, key=lambda x: (x["days"], x["tier"], x["name"]))
    ]
    body = "\n".join(rows) if rows else "_No secrets with an expiry set._"
    blocks = [
        {"type": "header",
         "text": {"type": "plain_text", "text": f"Key Vault secret expiry digest{header_suffix}"}},
        {"type": "section",
         "text": {"type": "mrkdwn", "text": f"Monthly overview of tracked secret expiries:\n{body}"}},
    ]
    blocks.extend(footer_blocks())
    return {
        "channel": CHANNEL_PLACEHOLDER,
        "text": f"Key Vault secret expiry digest{header_suffix}",
        "blocks": blocks,
    }


def mention_for(alerted: list[dict], missing: list[dict], missing_tiers: list[str]) -> str:
    tiers = {f["alert_tier"] for f in alerted}
    if 3 in tiers:
        return "<!channel>"
    if 2 in tiers or missing or missing_tiers:
        return "<!here>"
    return ""


def build_payload(
    alerted: list[dict],
    missing: list[dict],
    missing_tiers: list[str],
    header_suffix: str = "",
) -> dict:
    blocks = build_blocks(alerted, missing, missing_tiers, header_suffix=header_suffix)
    mention = mention_for(alerted, missing, missing_tiers)
    if mention:
        blocks.insert(1, {
            "type": "section",
            "text": {"type": "mrkdwn", "text": mention},
        })
    return {
        "channel": CHANNEL_PLACEHOLDER,
        "text": f"Key Vault secret expiry{header_suffix or ' alert'}",
        "blocks": blocks,
    }


def write_payload(path: str, payload: dict) -> None:
    pathlib.Path(path).write_text(json.dumps(payload, indent=2))


def render_step_summary(
    expiry_findings: list[dict],
    missing: list[dict],
    present_tiers: list[str],
    missing_tiers: list[str],
) -> str:
    lines: list[str] = []
    lines.append("# Key Vault secret expiry — daily check\n")
    lines.append(f"Checked source tiers: {', '.join(present_tiers) if present_tiers else '(none)'}\n")
    if missing_tiers:
        lines.append(
            f":rotating_light: **Data unavailable for: {', '.join(missing_tiers)}** "
            "— matrix leg(s) for these tiers failed; rerun or check workflow logs.\n"
        )
    if RUNBOOK_URL:
        lines.append(f"Rotation runbook: {RUNBOOK_URL}\n")

    with_expires = [f for f in expiry_findings if f["expires"]]
    lines.append("## Source secrets with `attributes.expires` set\n")
    if not with_expires:
        lines.append("_None found._\n")
    else:
        lines.append("| Tier | Secret | Expires (UTC) | Days left | Status |")
        lines.append("|---|---|---|---|---|")
        for f in sorted(with_expires, key=lambda x: (x["days"], x["tier"], x["name"])):
            status = ""
            if f["alert_tier"] == 3:
                status = ":red_circle: critical"
            elif f["alert_tier"] == 2:
                status = ":large_orange_diamond: warning"
            elif f["alert_tier"] == 1:
                status = ":large_yellow_circle: heads-up"
            lines.append(
                f"| {f['tier']} | `{f['name']}` | {f['expires'][:10]} | {f['days']} | {status} |"
            )
        lines.append("")

    lines.append("## Monitored secrets with configuration issues\n")
    if not missing:
        lines.append("_None._\n")
    else:
        lines.append("| Tier | Secret | Issue |")
        lines.append("|---|---|---|")
        for m in sorted(missing, key=lambda x: (x["tier"], x["name"])):
            issue = (
                "Missing `attributes.expires`"
                if m["state"] == "no-expires"
                else "Not present in source vault"
            )
            lines.append(f"| {m['tier']} | `{m['name']}` | {issue} |")
        lines.append("")

    return "\n".join(lines)


def main() -> int:
    if not EXPECTED_TIERS:
        raise RuntimeError(
            "EXPECTED_TIERS_JSON is empty. The workflow's prepare job is supposed to "
            "populate it with the tiers to check; an empty list would silently "
            "succeed with zero findings."
        )

    expiry_findings, missing, present_tiers = collect_findings()
    # A tier we were asked to check but got no file for means its matrix leg failed.
    missing_tiers = sorted(set(EXPECTED_TIERS) - set(present_tiers))

    alerted = [f for f in expiry_findings if f["alert_tier"] is not None]

    def is_prod(tier: str) -> bool:
        return tier == PROD_TIER

    nonprod_alerted = [f for f in alerted if not is_prod(f["tier"])]
    prod_alerted = [f for f in alerted if is_prod(f["tier"])]
    prod_tier3 = [f for f in prod_alerted if f["alert_tier"] == 3]
    nonprod_missing = [m for m in missing if not is_prod(m["tier"])]
    prod_missing = [m for m in missing if is_prod(m["tier"])]

    nonprod_missing_tiers = [t for t in missing_tiers if not is_prod(t)]
    prod_missing_tiers = [t for t in missing_tiers if is_prod(t)]

    if nonprod_alerted or nonprod_missing or nonprod_missing_tiers:
        write_payload(
            "slack-nonprod.json",
            build_payload(nonprod_alerted, nonprod_missing, nonprod_missing_tiers,
                          header_suffix=" alert (non-prod)"),
        )
    if prod_alerted or prod_missing or prod_missing_tiers:
        write_payload(
            "slack-prod.json",
            build_payload(prod_alerted, prod_missing, prod_missing_tiers,
                          header_suffix=" alert (prod)"),
        )
    if prod_tier3:
        write_payload(
            "slack-prod-critical.json",
            build_payload(prod_tier3, [], [], header_suffix=" — PROD CRITICAL"),
        )

    # Monthly digest: a full overview of every tracked expiry, sent on the 1st (or on demand
    # via SEND_DIGEST). Independent of the alert rules — never flips the run to failed.
    digest_mode = SEND_DIGEST or datetime.now(timezone.utc).day == 1
    nonprod_findings = [f for f in expiry_findings if not is_prod(f["tier"])]
    prod_findings = [f for f in expiry_findings if is_prod(f["tier"])]
    have_nonprod_digest = digest_mode and bool(nonprod_findings)
    have_prod_digest = digest_mode and bool(prod_findings)
    if have_nonprod_digest:
        write_payload("slack-nonprod-digest.json", build_digest_payload(nonprod_findings, " (non-prod)"))
    if have_prod_digest:
        write_payload("slack-prod-digest.json", build_digest_payload(prod_findings, " (prod)"))

    STEP_SUMMARY.write_text(render_step_summary(expiry_findings, missing, present_tiers, missing_tiers))

    any_rule_tripped = bool(alerted or missing or missing_tiers)
    with STEP_OUTPUT.open("a") as out:
        out.write(f"any_rule_tripped={'true' if any_rule_tripped else 'false'}\n")
        out.write(f"have_nonprod_payload={'true' if (nonprod_alerted or nonprod_missing or nonprod_missing_tiers) else 'false'}\n")
        out.write(f"have_prod_payload={'true' if (prod_alerted or prod_missing or prod_missing_tiers) else 'false'}\n")
        out.write(f"have_prod_critical_payload={'true' if prod_tier3 else 'false'}\n")
        out.write(f"have_nonprod_digest={'true' if have_nonprod_digest else 'false'}\n")
        out.write(f"have_prod_digest={'true' if have_prod_digest else 'false'}\n")

    print(
        f"Findings: {len(alerted)} expiring, {len(missing)} configuration issues "
        f"across present tiers {present_tiers}; missing tiers: {missing_tiers}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
