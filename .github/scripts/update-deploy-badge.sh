#!/usr/bin/env bash
# Write the shields.io endpoint badge for one deploy environment to the badges branch.
# The README renders it through https://img.shields.io/endpoint?url=<raw url of <name>.json>.
# The badge shows the deployed version, green when the latest run succeeded and red when it failed.
#
# Inputs (env):
#   GH_TOKEN      token with contents:write on REPOSITORY
#   REPOSITORY    owner/repo
#   DRY_RUN       when set to 1, print the badge JSON instead of writing it
# Args:
#   $1            badge name, written to <name>.json
#   $2            badge label
#   $3            run result: success or failure
#   $4            deployed version; when empty, the badge keeps the version it already shows
set -euo pipefail

NAME="$1"
LABEL="$2"
STATUS="$3"
VERSION="${4:-}"
BRANCH="badges"
FILE="$NAME.json"

# GitHub's own workflow badge colors, so the badges look like the badge.svg they replace.
case "$STATUS" in
  success) color="#2ea44f" ;;
  failure) color="#cf222e" ;;
  *)
    echo "::warning::Unknown status '$STATUS', expected success or failure" >&2
    exit 1
    ;;
esac

# Prints the badge JSON currently on the branch, or nothing if the branch or file does not exist yet.
current_badge() {
  local content
  if content=$(gh api "repos/$REPOSITORY/contents/$FILE?ref=$BRANCH" --jq .content 2>/dev/null); then
    base64 -d <<<"$content"
  fi
}

ensure_branch() {
  if gh api "repos/$REPOSITORY/git/ref/heads/$BRANCH" --silent 2>/dev/null; then
    return
  fi

  local tree commit
  tree=$(jq -n '{tree: [{path: "README.md", mode: "100644", type: "blob",
      content: "Shields.io endpoint badges written by .github/workflows/workflow-update-deploy-badge.yml.\n"}]}' \
    | gh api "repos/$REPOSITORY/git/trees" --input - --jq .sha)
  commit=$(jq -n --arg tree "$tree" '{message: "Create badges branch", tree: $tree, parents: []}' \
    | gh api "repos/$REPOSITORY/git/commits" --input - --jq .sha)
  # Another run may have created the branch in the meantime.
  jq -n --arg ref "refs/heads/$BRANCH" --arg sha "$commit" '{ref: $ref, sha: $sha}' \
    | gh api "repos/$REPOSITORY/git/refs" --input - --silent \
    || gh api "repos/$REPOSITORY/git/ref/heads/$BRANCH" --silent
}

# Badges for different environments can be written at the same time, which moves the branch head
# under a pending write and fails it with a conflict, so each write retries with a fresh blob sha.
write_file() {
  local content=$1 message=$2 attempt sha
  for attempt in 1 2 3 4 5; do
    if ! sha=$(gh api "repos/$REPOSITORY/contents/$FILE?ref=$BRANCH" --jq .sha 2>/dev/null); then
      sha=""
    fi
    if jq -n --arg message "$message" --arg content "$(base64 <<<"$content" | tr -d '\n')" \
        --arg branch "$BRANCH" --arg sha "$sha" \
        '{message: $message, content: $content, branch: $branch} + (if $sha == "" then {} else {sha: $sha} end)' \
      | gh api -X PUT "repos/$REPOSITORY/contents/$FILE" --input - --silent; then
      return
    fi
    echo "Writing $FILE failed (attempt $attempt), retrying"
    sleep $((attempt * 2))
  done
  echo "::warning::Could not write $FILE to the $BRANCH branch" >&2
  return 1
}

if [[ -n "$VERSION" ]]; then
  message="v${VERSION#v}"
else
  message=$(current_badge | jq -r '.message // empty')
  if [[ -z "$message" ]]; then
    echo "No version given and $FILE has none yet, leaving it unwritten"
    exit 0
  fi
fi

json=$(jq -n --arg label "$LABEL" --arg message "$message" --arg color "$color" \
  '{schemaVersion: 1, label: $label, message: $message, color: $color, labelColor: "#24292e", namedLogo: "github"}')
echo "$FILE: $json"
if [[ "${DRY_RUN:-0}" == "1" ]]; then
  exit 0
fi

ensure_branch
write_file "$json" "$LABEL: $message ($STATUS)"
