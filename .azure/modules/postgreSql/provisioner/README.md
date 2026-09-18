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
