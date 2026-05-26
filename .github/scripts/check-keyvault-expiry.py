"""Aggregate Key Vault secret expiry findings across envs, emit Slack payloads and step summary.

Reads:
  - $MONITORED_SECRETS_FILE: YAML with `required_expires:` list of secret names that MUST have expires set.
  - $SECRETS_DIR: directory containing one `secrets-<env>.json` per env, each a list of
      {"name": "...", "expires": "ISO8601 or null"} for every ENABLED secret in that env's KV.
  - $GITHUB_RUN_URL: link to current workflow run, embedded in Slack messages.

Writes:
  - slack-nonprod.json / slack-prod.json / slack-prod-critical.json: payloads for the
    Slack chat.postMessage step (only files for channels that have something to say).
  - $GITHUB_STEP_SUMMARY: public-facing markdown summary (env + secret name + expires + days,
    NO vault names).
  - $GITHUB_OUTPUT: sets `any_rule_tripped=true|false` so the workflow can red-flag the run.

Routing tiers (per finding):
  - Tier 1: 8 <= days_left <= 30, no mention
  - Tier 2: 2 <= days_left <= 7, @here
  - Tier 3: days_left <= 1 (incl. expired), @channel; for prod, also cross-posts to the
    prod-critical channel
Configuration-error findings (a monitored secret missing or without expires) post at
the @here level with no tier escalation.
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
STEP_SUMMARY = pathlib.Path(os.environ["GITHUB_STEP_SUMMARY"])
STEP_OUTPUT = pathlib.Path(os.environ["GITHUB_OUTPUT"])

CHANNEL_PLACEHOLDER = "${{ env.CHANNEL_ID }}"  # filled in by slack-github-action's payload-templated

PROD_ENV = "prod"


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


def load_monitored() -> set[str]:
    if not MONITORED_FILE.exists():
        return set()
    data = yaml.safe_load(MONITORED_FILE.read_text()) or {}
    return set(data.get("required_expires", []) or [])


def collect_findings() -> tuple[list[dict], list[dict], list[str]]:
    """Returns (expiry_findings, missing_findings, env_list)."""
    monitored = load_monitored()
    today = datetime.now(timezone.utc).date()

    expiry_findings: list[dict] = []
    missing_findings: list[dict] = []
    envs: list[str] = []

    for f in sorted(SECRETS_DIR.glob("secrets-*.json")):
        env_name = f.stem[len("secrets-"):]
        envs.append(env_name)
        secrets = json.loads(f.read_text())
        by_name = {s["name"]: s for s in secrets}

        for s in secrets:
            if not s.get("expires"):
                continue
            days = (parse_iso(s["expires"]).date() - today).days
            tier = classify_tier(days)
            expiry_findings.append({
                "env": env_name,
                "name": s["name"],
                "expires": s["expires"],
                "days": days,
                "tier": tier,
            })

        for required_name in sorted(monitored):
            if required_name not in by_name:
                missing_findings.append({
                    "env": env_name,
                    "name": required_name,
                    "state": "absent",
                })
            elif not by_name[required_name].get("expires"):
                missing_findings.append({
                    "env": env_name,
                    "name": required_name,
                    "state": "no-expires",
                })

    return expiry_findings, missing_findings, envs


def build_blocks(alerted: list[dict], missing: list[dict], header_suffix: str = "") -> list[dict]:
    blocks: list[dict] = [{
        "type": "header",
        "text": {"type": "plain_text", "text": f"Key Vault secret expiry{header_suffix}"},
    }]

    if alerted:
        rows = []
        for f in sorted(alerted, key=lambda x: (x["days"], x["env"], x["name"])):
            rows.append(
                f"{tier_icon(f['tier'])} *{f['env']}* `{f['name']}` "
                f"— {days_phrase(f['days'])} (expires {f['expires'][:10]})"
            )
        blocks.append({
            "type": "section",
            "text": {"type": "mrkdwn", "text": "*Expiring soon:*\n" + "\n".join(rows)},
        })

    if missing:
        rows = []
        for m in sorted(missing, key=lambda x: (x["env"], x["name"])):
            detail = (
                "no `attributes.expires` set"
                if m["state"] == "no-expires"
                else "secret not found in Key Vault"
            )
            rows.append(f":warning: *{m['env']}* `{m['name']}` — {detail}")
        blocks.append({
            "type": "section",
            "text": {
                "type": "mrkdwn",
                "text": "*Monitored secrets with configuration issues:*\n" + "\n".join(rows),
            },
        })

    if RUN_URL:
        blocks.append({
            "type": "context",
            "elements": [{"type": "mrkdwn", "text": f"<{RUN_URL}|View workflow run>"}],
        })

    return blocks


def mention_for(alerted: list[dict], missing: list[dict]) -> str:
    tiers = {f["tier"] for f in alerted}
    if 3 in tiers:
        return "<!channel>"
    if 2 in tiers or missing:
        return "<!here>"
    return ""


def build_payload(alerted: list[dict], missing: list[dict], header_suffix: str = "") -> dict:
    blocks = build_blocks(alerted, missing, header_suffix=header_suffix)
    mention = mention_for(alerted, missing)
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


def render_step_summary(expiry_findings: list[dict], missing: list[dict], envs: list[str]) -> str:
    lines: list[str] = []
    lines.append("# Key Vault secret expiry — daily check\n")
    lines.append(f"Checked envs: {', '.join(envs) if envs else '(none)'}\n")

    with_expires = [f for f in expiry_findings if f["expires"]]
    lines.append("## Secrets with `attributes.expires` set\n")
    if not with_expires:
        lines.append("_None found._\n")
    else:
        lines.append("| Env | Secret | Expires (UTC) | Days left | Status |")
        lines.append("|---|---|---|---|---|")
        for f in sorted(with_expires, key=lambda x: (x["days"], x["env"], x["name"])):
            status = ""
            if f["tier"] == 3:
                status = ":red_circle: critical"
            elif f["tier"] == 2:
                status = ":large_orange_diamond: warning"
            elif f["tier"] == 1:
                status = ":large_yellow_circle: heads-up"
            lines.append(
                f"| {f['env']} | `{f['name']}` | {f['expires'][:10]} | {f['days']} | {status} |"
            )
        lines.append("")

    lines.append("## Monitored secrets with configuration issues\n")
    if not missing:
        lines.append("_None._\n")
    else:
        lines.append("| Env | Secret | Issue |")
        lines.append("|---|---|---|")
        for m in sorted(missing, key=lambda x: (x["env"], x["name"])):
            issue = (
                "Missing `attributes.expires`"
                if m["state"] == "no-expires"
                else "Secret not present in Key Vault"
            )
            lines.append(f"| {m['env']} | `{m['name']}` | {issue} |")
        lines.append("")

    return "\n".join(lines)


def main() -> int:
    expiry_findings, missing, envs = collect_findings()

    alerted = [f for f in expiry_findings if f["tier"] is not None]

    def is_prod(env: str) -> bool:
        return env == PROD_ENV

    nonprod_alerted = [f for f in alerted if not is_prod(f["env"])]
    prod_alerted = [f for f in alerted if is_prod(f["env"])]
    prod_tier3 = [f for f in prod_alerted if f["tier"] == 3]
    nonprod_missing = [m for m in missing if not is_prod(m["env"])]
    prod_missing = [m for m in missing if is_prod(m["env"])]

    if nonprod_alerted or nonprod_missing:
        write_payload("slack-nonprod.json", build_payload(nonprod_alerted, nonprod_missing, header_suffix=" alert (non-prod)"))
    if prod_alerted or prod_missing:
        write_payload("slack-prod.json", build_payload(prod_alerted, prod_missing, header_suffix=" alert (prod)"))
    if prod_tier3:
        write_payload("slack-prod-critical.json", build_payload(prod_tier3, [], header_suffix=" — PROD CRITICAL"))

    STEP_SUMMARY.write_text(render_step_summary(expiry_findings, missing, envs))

    any_rule_tripped = bool(alerted or missing)
    with STEP_OUTPUT.open("a") as out:
        out.write(f"any_rule_tripped={'true' if any_rule_tripped else 'false'}\n")
        out.write(f"have_nonprod_payload={'true' if (nonprod_alerted or nonprod_missing) else 'false'}\n")
        out.write(f"have_prod_payload={'true' if (prod_alerted or prod_missing) else 'false'}\n")
        out.write(f"have_prod_critical_payload={'true' if prod_tier3 else 'false'}\n")

    print(f"Findings: {len(alerted)} expiring, {len(missing)} configuration issues across envs {envs}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
