# DIS deployment notes

Flux pulls app-config and syncroot directly from this repository.
The syncroot includes a `GitRepository` named `dialogporten` in `flux-system` that points at `https://github.com/Altinn/dialogporten` (branch `main`).

## Required Flux substitutions
Provide a ConfigMap or Secret in `flux-system` named `dialogporten-flux-substitutions` with the following keys
(use Secret for sensitive values):
- `DIALOGPORTEN_APPINSIGHTS_CONNECTION_STRING`
- `DIALOGPORTEN_AZURE_APPCONFIG_URI`
- `DIALOGPORTEN_KEY_VAULT_URL`
- `DIALOGPORTEN_SERVICEBUS_HOST` (format: `sb://<namespace>.servicebus.windows.net/`)
- `DIALOGPORTEN_AZURE_SUBSCRIPTION_ID`
- `DIALOGPORTEN_COST_METRICS_STORAGE_ACCOUNT_NAME` (staging/prod only)

## Pending work
- RoleAssignment CRs for App Config, Service Bus, App Insights and storage are not yet implemented. The DIS ASO RoleAssignment schema must be confirmed before adding them.
