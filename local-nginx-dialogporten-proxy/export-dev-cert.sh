#!/usr/bin/env bash
# Exports the *existing* ASP.NET Core HTTPS development certificate — the
# same one `dotnet run` already uses on https://localhost:7214 — to PEM
# files nginx can use for TLS termination.
#
# `dotnet dev-certs https` only ever generates a new certificate if none is
# already present in your user cert store; if one exists (it does, since
# you've run the app with HTTPS before), this reuses it as-is.
set -euo pipefail

CERT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/certs"
mkdir -p "$CERT_DIR"

dotnet dev-certs https \
  -ep "$CERT_DIR/dev-cert.pem" \
  --format PEM \
  --no-password \
  --trust

echo "Exported $CERT_DIR/dev-cert.pem and $CERT_DIR/dev-cert.key"
