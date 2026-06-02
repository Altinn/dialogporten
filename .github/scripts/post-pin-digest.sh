#!/usr/bin/env bash
# Post the monthly Key Vault expiry digest to Slack, pin it, and unpin the previous
# digest from this bot so only the latest stays pinned.
#
# Inputs (env):
#   SLACK_BOT_TOKEN   bot token with chat:write, pins:write, pins:read
#   CHANNEL_ID        target channel id
# Args:
#   $1                path to the digest payload JSON (its .channel is overwritten with CHANNEL_ID)
#
# Safety: only ever unpins messages that are BOTH authored by a bot AND whose text starts with
# the digest header, and never the message just pinned. Human-pinned content is left untouched.
set -euo pipefail

PAYLOAD_FILE="$1"
API="https://slack.com/api"
AUTH=(-H "Authorization: Bearer ${SLACK_BOT_TOKEN}")
JSON=(-H "Content-type: application/json; charset=utf-8")
DIGEST_PREFIX="Key Vault secret expiry digest"

# 1) Post the digest (inject the real channel id into the payload).
payload=$(jq --arg ch "$CHANNEL_ID" '.channel = $ch' "$PAYLOAD_FILE")
resp=$(curl -sS -X POST "$API/chat.postMessage" "${AUTH[@]}" "${JSON[@]}" --data "$payload")
if [ "$(jq -r '.ok' <<<"$resp")" != "true" ]; then
  echo "::error::chat.postMessage failed: $(jq -r '.error // .' <<<"$resp")"
  exit 1
fi
new_ts=$(jq -r '.ts' <<<"$resp")
echo "Posted digest ts=$new_ts to channel $CHANNEL_ID"

# 2) Pin the new digest.
resp=$(curl -sS -X POST "$API/pins.add" "${AUTH[@]}" "${JSON[@]}" \
  --data "$(jq -n --arg ch "$CHANNEL_ID" --arg ts "$new_ts" '{channel:$ch, timestamp:$ts}')")
if [ "$(jq -r '.ok' <<<"$resp")" != "true" ]; then
  # already_pinned is harmless; anything else is worth surfacing but not fatal.
  echo "::warning::pins.add: $(jq -r '.error // .' <<<"$resp")"
fi

# 3) Unpin previous digests from this bot (keep the one just pinned).
pins=$(curl -sS "$API/pins.list" "${AUTH[@]}" -G --data-urlencode "channel=$CHANNEL_ID")
if [ "$(jq -r '.ok' <<<"$pins")" != "true" ]; then
  echo "::warning::pins.list failed, skipping cleanup: $(jq -r '.error // .' <<<"$pins")"
  exit 0
fi

jq -r --arg keep "$new_ts" --arg prefix "$DIGEST_PREFIX" '
  .items[]? | select(.type == "message") | .message
  | select(.bot_id != null)
  | select((.text // "") | startswith($prefix))
  | select(.ts != $keep)
  | .ts' <<<"$pins" | while read -r ts; do
    [ -n "$ts" ] || continue
    r=$(curl -sS -X POST "$API/pins.remove" "${AUTH[@]}" "${JSON[@]}" \
      --data "$(jq -n --arg ch "$CHANNEL_ID" --arg ts "$ts" '{channel:$ch, timestamp:$ts}')")
    echo "Unpinned previous digest ts=$ts ok=$(jq -r '.ok' <<<"$r")"
  done
