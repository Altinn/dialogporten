# Tools

`.github/tools/containerAppJobVerifier.sh` is used to verify the status of a container application job. It checks if the job has completed successfully or not.

`.github/tools/pgOwnerPassword.sh` is sourced by a workflow step to export `PGPASSWORD` for the PostgreSQL server administrator login, taken from the connection string secret in the environment Key Vault. The value is masked before use.

`.github/tools/pwdGenerator.ps1` is used to generate passwords.

`.github/tools/resolvePostgresFqdn.sh` resolves the fully qualified domain name of the active Dialogporten PostgreSQL server in a resource group and writes it to `$GITHUB_OUTPUT` as `pg_fqdn`.

`.github/tools/revisionVerifier.sh` is used to verify the revision of the deployed container app. It ensures that the current revision has been deployed successfully.
