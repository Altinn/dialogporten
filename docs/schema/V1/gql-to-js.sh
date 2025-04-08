#!/usr/bin/env bash
set -euo pipefail

INPUT_FILE="schema.verified.graphql"
OUTPUT_FILE="src/index.js"
VAR_NAME="schema_verified_graphql"

if [[ ! -f "$INPUT_FILE" ]]; then
  echo "Error: $INPUT_FILE not found!"
  exit 1
fi

CLEANED_SCHEMA=$(sed '1s/^\xEF\xBB\xBF//' "$INPUT_FILE" | sed "s/\`/'/g")

{
  printf "export const %s = \`" "$VAR_NAME"
  printf "%s" "$CLEANED_SCHEMA"
  printf "\`"
} > "$OUTPUT_FILE"
