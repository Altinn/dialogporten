# PostgreSQL identity provisioning

The image runs inside ACA or AKS with access to the private PostgreSQL server.
It authenticates with its own Entra administrator identity. Connections require
`verify-full` TLS and use the image's system CA store. Set `PGSSLROOTCERT` to a
mounted CA file when a custom trust store is needed; use the server FQDN as `PGHOST`.

## Bootstrap and rollout

1. Deploy infrastructure with `enableDbProvisioner: true` for the target environment.
   This creates the ACA provisioner identity and administrator registration. For
   AKS, register the operator-created identity separately through
   `additionalEntraAdministrators`, using its principal ID and managed identity name.
2. For a fresh environment, run **Dispatch Apps** using a release containing these
   workflows, with `runMigration: true` and `provisionDbPrincipals: false`.
   Keep every workload's `dbAuthMode` at `Password`. This creates the tables and
   workload identities before the provisioner attempts to resolve them. The default
   for subsequent deployments remains `provisionDbPrincipals: true`.
3. Prepare pgAudit before the first provisioning run:
   - Ensure `PGAUDIT` is in `azure.extensions` (provided by the PostgreSQL module).
   - Read the server's current `shared_preload_libraries` and add `pgaudit` while
     preserving all other required libraries. If managing this through
     `staticServerConfigurations`, supply that complete list and explicitly enable
     `applyStaticServerConfigurations` for the planned change.
   - Restart the server during an agreed maintenance window if the preload setting
     changed. Check the active value afterwards; merely setting it is insufficient.
   - Connect as an administrator to **dialogporten**, then run:

     ```sql
     CREATE EXTENSION IF NOT EXISTS pgaudit;
     SELECT extname FROM pg_extension WHERE extname = 'pgaudit';
     ```

   These are deliberate environment setup operations, not part of each app deploy.
   Production database setup and any restart must be performed manually.
4. Run provisioning with the switch enabled (ACA), or manually create a Job from
   the provisioner CronJob (AKS). Its image tag must reference a published image.
   The read-only pgAudit preflight runs before any principal or privilege changes.
5. Verify login and auditing with a fresh workload session, then enable Entra token
   authentication per workload using the credential-retirement steps below. Do not
   use the bootstrap bypass for workloads already depending on the provisioned roles.

No workflow in this change restarts PostgreSQL or installs pgAudit automatically.
The membership script also retains its prerequisite check for standalone use.

## Retire administrator credentials per workload

Deploy the companion application changes before switching authentication. Token
mode now requires a password-free bootstrap connection string, so API startup and
App Configuration refresh never resolve the administrator connection-string secret.
The remaining App Configuration secrets still resolve normally.

For each ACA workload, set `dbAuthMode = 'EntraToken'` and `dbHost` to the PostgreSQL
server FQDN. The template supplies `Infrastructure__DialogDbConnectionString` with
the host, database and TLS settings only. The four database Janitor jobs also omit
the `dbconnectionstring` secret reference and receive secret-scoped access to
`dialogportenRedisConnectionString` only.

For WebApi EU/SO, GraphQL and Service, additionally set `runtimeSecretNames` to the
explicit list of non-database secrets referenced by that workload's selected App
Configuration keys (unlabelled keys plus its environment label). Inventory those
references before enabling the mode; do not guess or remove secrets still required
by other features. Include Redis and any signing/client credentials that resolve at
startup. The templates require a nonempty list and reject the canonical database
administrator secret names. For example, a workload whose only remaining secret
reference is Redis uses:

```bicep
param dbAuthMode = 'EntraToken'
param dbHost = '<server>.postgres.database.azure.com'
param runtimeSecretNames = ['dialogportenRedisConnectionString']
```

Token-mode deployments create secret-scoped grants instead of vault-wide grants.
**Incremental Bicep deployment does not delete an existing vault-wide assignment.**
After the new revision is healthy and all old Password-mode replicas/executions of
that workload have drained, explicitly retire its former grant. Review the
assignment IDs and scopes first; remove only that workload's old vault-wide grant:

```sh
az role assignment list --assignee-object-id "$WORKLOAD_PRINCIPAL_ID" \
  --scope "$KEY_VAULT_ID" --include-inherited --all \
  --query '[].{id:id,scope:scope,role:roleDefinitionName}' --output table

# Set this to the reviewed vault-wide Key Vault Secrets User assignment above.
az role assignment delete --ids "$VAULT_WIDE_ASSIGNMENT_ID"
```

Check for inherited or group-based grants that still expose database credentials;
removing the direct assignment alone cannot override them. Start a fresh workload
session, exercise database and other secret-dependent operations, and verify that
the workload identity is denied access to the administrator connection-string
secret. Until that check succeeds, the restricted PostgreSQL roles are not a
security boundary for a compromised workload. Keep administrator credentials for
the migration job and other workloads still using Password mode.

For AKS, apply the same password-free environment settings and remove the database
secret from the workload's SecretStore/ExternalSecret mapping. Narrow its Key Vault
grants and verify denial before considering that workload migrated. The provisioning
CronJob itself has no secret and needs no change for this step.

Returning a migrated workload to Password mode requires deliberately restoring its
database secret reference and access before starting the old application revision.
Do not reintroduce vault-wide access as an automatic failure fallback. Production
grant removal and any database/bootstrap changes remain manual rollout operations.

## Local checks

Run `shellcheck .azure/modules/postgreSql/provisioner/entrypoint.sh` and
`python3 -m unittest discover -s .azure/modules/postgreSql/provisioner/tests`.
The tests stub token acquisition and psql; live Azure permissions, TLS certificate
validation, and pgAudit installation still require environment validation.

Run `python3 -m unittest discover -s .azure/tests` with Bicep CLI 0.41.2 or later
to evaluate all eight workload templates locally in Password and EntraToken modes.
These tests check the resulting secrets and grant scopes, including rejected
administrator-secret allowlists. Set `BICEP_CLI` if the executable is not on PATH
or installed under `~/.azure/bin`. The credential-free PR validation job runs both
Python suites and ShellCheck; snapshots require no Azure connection.

## CI trust boundary

PRs and dry runs compile the provisioner Bicep template and test parameters in a
job with only `contents: read`, no environment, and no Azure login. This does not
perform an Azure what-if or verify that referenced resources exist.

Provisioning runs only from push or manual workflows on `main`. A separate job
without Azure credentials resolves `main` or a release tag to a commit already
in `main`; the deployment job checks out that immutable SHA. Checkout credentials
are not persisted. Start **Dispatch Apps** from `main`, with the release version
as its input.

These checks prevent the normal PR call path from authenticating, but editable
workflow checks are not a security boundary against a malicious PR. The shared
`test` environment must also restrict deployment branches to `main` (with no PR
merge-ref allowance), and the Azure federated credential must require that
protected environment. Before enabling that policy, migrate the existing
infrastructure and app PR what-if jobs to credential-free validation or a
separately approved workflow. Otherwise those existing PR checks will fail.
This is a repository-wide follow-up; this provisioning change alone does not
secure the other credential-bearing PR workflows.
