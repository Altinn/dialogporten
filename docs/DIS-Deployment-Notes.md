# DIS deployment notes

This repository now builds and publishes two OCI artifacts for DIS:
- App-config: `ghcr.io/altinn/dialogporten-config:<version>`
- Syncroot: `ghcr.io/altinn/dialogporten-syncroot:<version>`

## Required GitHub environment secrets
These are consumed by the app-config OCI build workflow (`workflow-build-app-config.yml`) and substituted into the Kustomize YAML via envsubst:
- `AZURE_APP_INSIGHTS_CONNECTION_STRING`
- `AZURE_APP_CONFIGURATION_NAME`
- `AZURE_ENVIRONMENT_KEY_VAULT_NAME`
- `AZURE_SERVICE_BUS_NAMESPACE_NAME`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_COST_METRICS_STORAGE_ACCOUNT_NAME` (staging/prod only)

## Env vars injected into manifests
- `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING`
- `DIALOGPORTEN_AZURE_APPCONFIG_URI`
- `DIALOGPORTEN_KEY_VAULT_URL`
- `DIALOGPORTEN_SERVICEBUS_HOST` (format: `sb://<namespace>.servicebus.windows.net/`)
- `DIALOGPORTEN_AZURE_SUBSCRIPTION_ID`
- `DIALOGPORTEN_COST_METRICS_STORAGE_ACCOUNT_NAME`

## Pending work
- RoleAssignment CRs for App Config, Service Bus, App Insights and storage are not yet implemented. The DIS ASO RoleAssignment schema must be confirmed before adding them.
