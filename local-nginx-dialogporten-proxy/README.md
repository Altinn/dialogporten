# nginx /dialogporten prefix proxy

Simulates the production APIM gateway locally: every request must be made
with a `/dialogporten` prefix, which is stripped before forwarding
unchanged to the backend running on the host machine (default:
`http://localhost:5123`, i.e. the WebApi `http` launch profile).

## Run with HTTPS

The `8443` listener terminates TLS using your existing ASP.NET Core HTTPS
development certificate — the exact same cert `dotnet run` already uses on
`https://localhost:7214` — so it's already trusted by your OS/browser.
`dotnet dev-certs` only generates a new certificate if none exists yet; if
you have one, it's reused as-is, just exported to files nginx can read.

1. Export it once (requires the `dotnet` CLI):
   ```
   ./export-dev-cert.sh
   ```
   This writes `certs/dev-cert.pem` and `certs/dev-cert.key` (gitignored).
2. Start the proxy:
   ```
   docker compose up
   ```
3. Hit it over HTTPS:
   ```
   curl https://localhost:8443/dialogporten/swagger/v1/swagger.json
   ```

## Pointing at a different backend

Edit the `server host.docker.internal:5123;` line in `nginx.conf` (e.g. to
target the GraphQL service or a different port), then restart:

```
docker compose up -d --force-recreate
```
