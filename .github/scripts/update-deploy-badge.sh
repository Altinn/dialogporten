#!/usr/bin/env bash
# Write the deploy badge for one environment to the badges branch:
#   <name>.svg    the badge image, which the README loads from raw.githubusercontent.com
#   <name>.json   the badge values, which a later run reads to keep the version
# The badge shows the deployed version, green when the latest run succeeded and red when it failed.
#
# GitHub loads README images from other hosts, such as img.shields.io, through its image proxy, which shows a
# broken image when the host answers slowly. Images on raw.githubusercontent.com are loaded directly, so the
# README shows the stored SVG, and shields.io is only used here to draw it.
#
# Inputs (env):
#   GH_TOKEN      token with contents:write on REPOSITORY
#   REPOSITORY    owner/repo
#   DRY_RUN       when set to 1, print the JSON and save the SVG to ./<name>.svg instead of writing to the branch
# Args:
#   $1            badge name, written to <name>.svg and <name>.json
#   $2            badge label
#   $3            run result: success or failure
#   $4            deployed version; when empty, the badge keeps the version it already shows

# Stop on a failed command, an unset variable or a failed step in a pipe.
set -euo pipefail

NAME="$1"
LABEL="$2"
STATUS="$3"
VERSION="${4:-}"
BRANCH="badges"
JSON_FILE="$NAME.json"
SVG_FILE="$NAME.svg"
# The dark gray behind the label on GitHub's own badges.
LABEL_COLOR="#24292e"

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
  # The Contents API returns the file base64-encoded.
  # A missing file or branch gives a 404, which skips the decode.
  if content=$(gh api "repos/$REPOSITORY/contents/$JSON_FILE?ref=$BRANCH" --jq .content 2>/dev/null); then
    base64 -d <<<"$content"
  fi
}

# Succeeds when $1 is well-formed XML with an <svg> root, which a browser can draw.
is_svg() {
  [[ "$1" == "<svg "* ]] \
    && python3 -I -c 'import sys, xml.etree.ElementTree as ET; ET.fromstring(sys.stdin.read())' <<<"$1" 2>/dev/null
}

# Prints the badge SVG, drawn by shields.io's static badge from the same values as the JSON.
# Fails if shields.io does not answer with a well-formed SVG.
render_svg() {
  local svg="" attempt image position logo inlined
  # Each attempt is captured on its own: curl's --retry would append the retried answer to the part of a
  # timed-out answer it has already printed.
  for attempt in {1..3}; do
    if svg=$(curl --fail --silent --show-error --max-time 30 --get "https://img.shields.io/static/v1" \
        --data-urlencode "label=$LABEL" --data-urlencode "message=$message" --data-urlencode "color=$color" \
        --data-urlencode "labelColor=$LABEL_COLOR" --data-urlencode "logo=github"); then
      break
    fi
    svg=""
    sleep $((attempt * 2))
  done
  is_svg "$svg" || return 1

  # shields.io embeds the logo as an <image> with a data: URI, but raw.githubusercontent.com serves files with
  # "Content-Security-Policy: default-src 'none'", which allows no data: images. The logo is itself an SVG, so
  # it replaces the <image> as a nested <svg> at the same position. If a step finds nothing, or the result is
  # not a well-formed SVG, the SVG from shields.io is used as it is.
  if image=$(grep -o '<image [^>]*href="data:image/svg+xml;base64,[^"]*"[^>]*/>' <<<"$svg") \
    && position=$(grep -o 'x="[^"]*" y="[^"]*" width="[^"]*" height="[^"]*"' <<<"$image") \
    && logo=$(grep -o 'base64,[^"]*' <<<"$image" | cut -d, -f2 | base64 -d) \
    && [[ "$logo" == "<svg "* ]]; then
    # "<svg fill=... viewBox=...>" becomes "<svg x=... y=... width=... height=... fill=... viewBox=...>".
    inlined=${svg/"$image"/"<svg $position ${logo#<svg }"}
    if is_svg "$inlined"; then
      svg=$inlined
    fi
  fi
  printf '%s\n' "$svg"
}

# Creates the badges branch if it is missing. It shares no history with main, so it is built through the
# Git Data API: a tree with one README file, a commit of that tree with no parent, then the branch ref.
ensure_branch() {
  if gh api "repos/$REPOSITORY/git/ref/heads/$BRANCH" --silent 2>/dev/null; then
    return
  fi

  local tree commit
  tree=$(jq -n '{tree: [{path: "README.md", mode: "100644", type: "blob",
      content: "Deploy badges for the README, written by .github/workflows/workflow-update-deploy-badge.yml.\n"}]}' \
    | gh api "repos/$REPOSITORY/git/trees" --input - --jq .sha)
  commit=$(jq -n --arg tree "$tree" '{message: "Create badges branch", tree: $tree, parents: []}' \
    | gh api "repos/$REPOSITORY/git/commits" --input - --jq .sha)
  # Creating the ref fails if another run created the branch first, which is fine if it exists now.
  jq -n --arg ref "refs/heads/$BRANCH" --arg sha "$commit" '{ref: $ref, sha: $sha}' \
    | gh api "repos/$REPOSITORY/git/refs" --input - --silent \
    || gh api "repos/$REPOSITORY/git/ref/heads/$BRANCH" --silent
}

# Writes one file to the badges branch. Badges for different environments can be written at the same time,
# which moves the branch head under a pending write and fails it with a conflict, so each write retries with
# a fresh blob sha.
write_file() {
  local path=$1 content=$2 message=$3 attempt sha
  for attempt in {1..5}; do
    # Updating a file requires its current blob sha. A new file has none.
    if ! sha=$(gh api "repos/$REPOSITORY/contents/$path?ref=$BRANCH" --jq .sha 2>/dev/null); then
      sha=""
    fi
    # The request body has the content base64-encoded on one line, and the sha only when the file exists.
    if jq -n --arg message "$message" --arg content "$(base64 <<<"$content" | tr -d '\n')" \
        --arg branch "$BRANCH" --arg sha "$sha" \
        '{message: $message, content: $content, branch: $branch} + (if $sha == "" then {} else {sha: $sha} end)' \
      | gh api -X PUT "repos/$REPOSITORY/contents/$path" --input - --silent; then
      return
    fi
    echo "Writing $path failed (attempt $attempt), retrying"
    sleep $((attempt * 2))
  done
  echo "::warning::Could not write $path to the $BRANCH branch" >&2
  return 1
}

# The badge text is the version with one leading "v" (${VERSION#v} drops a "v" the input already has).
# Without a version, the badge keeps the text it already shows and only changes color.
if [[ -n "$VERSION" ]]; then
  message="v${VERSION#v}"
else
  message=$(current_badge | jq -r '.message // empty')
  if [[ -z "$message" ]]; then
    echo "No version given and $JSON_FILE has none yet, leaving the badge unwritten"
    exit 0
  fi
fi

# Badge values in the shields.io endpoint format (https://shields.io/badges/endpoint-badge).
json=$(jq -n --arg label "$LABEL" --arg message "$message" --arg color "$color" --arg labelColor "$LABEL_COLOR" \
  '{schemaVersion: 1, label: $label, message: $message, color: $color, labelColor: $labelColor, namedLogo: "github"}')
echo "$JSON_FILE: $json"

# When shields.io cannot draw the badge, the JSON is still written, and the SVG keeps its previous image until
# the next run draws it.
if ! svg=$(render_svg); then
  echo "::warning::Could not draw $SVG_FILE with shields.io, the badge keeps its previous image" >&2
  svg=""
fi

if [[ "${DRY_RUN:-0}" == "1" ]]; then
  if [[ -n "$svg" ]]; then
    printf '%s\n' "$svg" >"$SVG_FILE"
    echo "Saved $SVG_FILE"
  fi
  exit 0
fi

ensure_branch
# The JSON goes first: if the SVG write then fails, the next run still finds the new version and draws it.
write_file "$JSON_FILE" "$json" "$LABEL: $message ($STATUS)"
if [[ -n "$svg" ]]; then
  write_file "$SVG_FILE" "$svg" "$LABEL: $message ($STATUS)"
fi
