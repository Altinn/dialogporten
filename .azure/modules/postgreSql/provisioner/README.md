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
   authentication per workload. Do not use the bootstrap bypass for workloads
   already depending on the provisioned roles.

No workflow in this change restarts PostgreSQL or installs pgAudit automatically.
The membership script also retains its prerequisite check for standalone use.

## Local checks

Run `shellcheck .azure/modules/postgreSql/provisioner/entrypoint.sh` and
`python3 -m unittest discover -s .azure/modules/postgreSql/provisioner/tests`.
The tests stub token acquisition and psql; live Azure permissions, TLS certificate
validation, and pgAudit installation still require environment validation.
