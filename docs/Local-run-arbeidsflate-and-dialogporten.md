# Local run with arbeidsflate and dialogporten

## Download both repos:
- git@github.com:Altinn/dialogporten.git
- git@github.com:Altinn/dialogporten-frontend.git

## Local appsettings and secrets

Make sure to insert correct secrets for all products and environment.

`appsettings.local.json` must be changed depending on what to do. 
Example configurations for AT23

### Dialogporten

Arbeidsflate gets policies from AT23.
By default, getting these in Dialogporten is disabled.

Set both `UseLocalDevelopmentResourceRegister` and `DisablePolicyInformationSyncOnStartup`
to false in `src/Digdir.Domain.Dialogporten.WebApi/appsettings.local.json`

Example for GraphQL and WebApi 
```json
{
    "LocalDevelopment": {
        "UseLocalDevelopmentUser": false,
        "UseLocalDevelopmentResourceRegister": false,
        "UseLocalDevelopmentOrganizationRegister": false,
        "UseLocalDevelopmentNameRegister": false,
        "UseLocalDevelopmentPartyNameRegistry": false,
        "UseLocalDevelopmentAltinnAuthorization": false,
        "UseLocalDevelopmentCloudEventBus": true,
        "UseLocalDevelopmentCompactJwsGenerator": false,
        "DisableCache": false,
        "DisableAuth": false,
        "UseInMemoryServiceBusTransport": true
    }
}
```

### Dialogporten Adapter

The Adapter is not needed to run this setup.
However, app instance and FCE calls uses endpoints located in the adapter.  

By default, Adapter connect to TT02 and Arbeidsflate and Dialogporten connects to AT23.
Authorization can be a problem if there is a mismatch between environments. Thus, sticking to one environment is recommended.

Local settings for WebApi connecting to AT23
```json
{
  "DialogportenAdapter": {
    "Maskinporten": {
      "Environment": "test",
      "TokenExchangeEnvironment": "at23"
    },
    "Dialogporten": {
      "BaseUri": "https://localhost:7214"
    },
    "Altinn": {
      "BaseUri": "https://at23.altinn.cloud",
      "InternalStorageEndpoint": "https://platform.at23.altinn.cloud",
      "InternalRegisterEndpoint": "https://platform.at23.altinn.cloud",
      "SubscriptionKey": "PopulateFromEnvironmentVariable"
    }
  }
}
```
Make sure to insert correct secrets for AT23:
- "DialogportenAdapter:Maskinporten:EncodedJwk" 
- "DialogportenAdapter:Maskinporten:ClientId"
- "DialogportenAdapter:Altinn:SubscriptionKey"

## Setup Dialogporten

### Changing to non-default ports for Redis and Postgres
To avoid collisions with arbeidsflate, change the host ports for Postgres and Redis.
Both port numbers are increased by 10000 (inside the Docker containers, default ports are still used).
- Postgres: 5432 -> 15432
- Redis: 6379 -> 16379
- Change ports in user secrets
  - .Net user secrets: `"Infrastructure:DialogDbConnectionString": "Server=localhost;Port=15432;Database=Dialogporten;User ID=postgres;Password=supersecret;Include Error Detail=True;"`

### Start service
- Run `docker compose -f docker-compose-db-redis.yml up`
- Start at least GraphQL in Rider 

## Setup Arbeidsflate

### Make .env file
- In root folder for Arbeidsflate make a .env file.
- See Readme.md -> Running Docker locally for the fields needed
- Talk to Arbeidsflate team to get values for these environment variables
- **DIALOGPORTEN_URL** must be set to: http://host.docker.internal:5181

### Setting Up HTTPS Locally with mkcert
- See and follow: `packages/docs/docs/development/https+certs.md`

### Adding extra host URL for BFF (Backend for Frontend)
Update: `compose.yml` with this diff:
```diff
--- a/compose.yml
+++ b/compose.yml
@@ -120,6 +120,8 @@ services:
 
   bff:
     container_name: bff
+    extra_hosts:
+      - "host.docker.internal:host-gateway"
     restart: always
     build:
       context: .
```

### If using newer version of docker
This step might be removed, but currently Arbeidsflate runs with old version of podman/docker.
Thus update: `compose.yml` with the diff below:
```diff
--- a/compose.yml
+++ b/compose.yml
@@ -1,7 +1,7 @@
 services:
   reverse-proxy:
     container_name: reverse-proxy
-    image: traefik:v2.10
+    image: traefik:v3.6.2
     command:
       - "--api.dashboard=true"
       - "--providers.docker"
```

### Start services
- Run `make dev`
- When done, open a browser and go to `app.localhost`

## Problems 

### Browser caching for app.localhost
Use an incognito tab if needed
