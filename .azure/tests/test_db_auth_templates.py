"""Evaluate database authentication templates locally with Bicep CLI 0.41.2+.

Run with: python3 -m unittest discover -s .azure/tests
Set BICEP_CLI if the executable is not on PATH or installed by Azure CLI.
Snapshots are generated in a temporary directory; no Azure connection is used.
"""

import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
APPLICATIONS = (
    "custom-metrics-job",
    "graphql",
    "reindex-dialogsearch-job",
    "service",
    "sync-resource-policy-information-job",
    "sync-subject-resource-mappings-job",
    "web-api-eu",
    "web-api-so",
)
SECRET_READER_ROLE = "4633458b-17de-408a-b874-0445c86b69e6"
DB_HOST = "validation.postgres.database.azure.com"
RUNTIME_SECRETS = ["dialogportenRedisConnectionString", "runtimeSigningKey"]


class DatabaseAuthenticationTemplates(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.bicep = os.environ.get("BICEP_CLI") or shutil.which("bicep")
        if not cls.bicep:
            azure_cli_bicep = Path.home() / ".azure/bin/bicep"
            if azure_cli_bicep.is_file():
                cls.bicep = str(azure_cli_bicep)
        if not cls.bicep:
            raise unittest.SkipTest("Bicep CLI 0.41.2+ is required for offline snapshots")

    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="dialogporten-bicep-tests-")
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name)

    def snapshot(self, template, parameter_text, expected_error=None):
        parameters = self.path / "test.bicepparam"
        template_path = os.path.relpath(template, self.path)
        parameters.write_text(f"using '{template_path}'\n{parameter_text}\n")
        result = subprocess.run(
            [
                self.bicep, "snapshot", str(parameters), "--mode", "overwrite",
                "--subscription-id", "00000000-0000-0000-0000-000000000000",
                "--tenant-id", "00000000-0000-0000-0000-000000000000",
                "--resource-group", "dp-be-test-rg", "--location", "norwayeast",
            ],
            text=True,
            capture_output=True,
            check=False,
        )
        if expected_error:
            self.assertNotEqual(result.returncode, 0, result.stdout)
            self.assertIn(expected_error, result.stderr)
            return None
        self.assertEqual(result.returncode, 0, result.stderr)
        snapshot = json.loads(parameters.with_suffix(".snapshot.json").read_text())
        self.assertEqual(snapshot["diagnostics"], [])
        return snapshot["predictedResources"]

    def application_snapshot(self, application, mode, host=DB_HOST, secrets=None, expected_error=None):
        directory = ROOT / ".azure/applications" / application
        parameters = (directory / "test.bicepparam").read_text()
        parameters = re.sub(r"^using .+\n", "", parameters)
        parameters = re.sub(r"readEnvironmentVariable\('[^']+'\)", "'validation-only'", parameters)
        parameters += f"\nparam dbAuthMode = '{mode}'\nparam dbHost = '{host}'\n"
        if not application.endswith("-job"):
            secret_names = RUNTIME_SECRETS if secrets is None else secrets
            parameters += "param runtimeSecretNames = [" + ", ".join(f"'{name}'" for name in secret_names) + "]\n"
        return self.snapshot(directory / "main.bicep", parameters, expected_error)

    def test_entra_workloads_have_no_database_secret_or_vault_wide_grant(self):
        for application in APPLICATIONS:
            with self.subTest(application=application):
                resources = self.application_snapshot(application, "EntraToken")
                workload = next(resource for resource in resources if resource["type"] in (
                    "Microsoft.App/jobs", "Microsoft.App/containerApps"))
                identity = next(resource for resource in resources if resource["type"] ==
                                "Microsoft.ManagedIdentity/userAssignedIdentities")
                container = workload["properties"]["template"]["containers"][0]
                environment = {entry["name"]: entry for entry in container["env"]}
                connection = environment["Infrastructure__DialogDbConnectionString"]
                self.assertEqual(connection, {
                    "name": "Infrastructure__DialogDbConnectionString",
                    "value": f"Host={DB_HOST};Database=dialogporten;SSL Mode=VerifyFull",
                })
                self.assertEqual(environment["Infrastructure__DialogDbAuth__Mode"]["value"], "EntraToken")
                self.assertEqual(environment["Infrastructure__DialogDbAuth__Username"]["value"], identity["name"])
                secrets = workload["properties"]["configuration"].get("secrets", [])
                self.assertEqual([secret["name"] for secret in secrets],
                                 ["redisconnectionstring"] if application.endswith("-job") else [])
                grants = [resource for resource in resources if resource["type"] ==
                          "Microsoft.Authorization/roleAssignments" and
                          resource["properties"]["roleDefinitionId"].endswith(SECRET_READER_ROLE)]
                self.assertEqual(len(grants), 1 if application.endswith("-job") else len(RUNTIME_SECRETS))
                for grant in grants:
                    # Unknown managed-identity IDs remain expressions in offline snapshots;
                    # their resource scopes still distinguish vault-wide and secret grants.
                    self.assertRegex(grant["id"], r"Microsoft\.KeyVault/vaults/(?:secrets'|[^/']+/secrets/)")

    def test_password_workloads_keep_existing_secret_access(self):
        for application in APPLICATIONS:
            with self.subTest(application=application):
                resources = self.application_snapshot(application, "Password", host="", secrets=[])
                workload = next(resource for resource in resources if resource["type"] in (
                    "Microsoft.App/jobs", "Microsoft.App/containerApps"))
                container = workload["properties"]["template"]["containers"][0]
                environment = {entry["name"]: entry for entry in container["env"]}
                self.assertNotIn("Infrastructure__DialogDbAuth__Mode", environment)
                self.assertNotIn("Infrastructure__DialogDbAuth__Username", environment)
                secrets = workload["properties"]["configuration"].get("secrets", [])
                self.assertEqual([secret["name"] for secret in secrets],
                                 ["dbconnectionstring", "redisconnectionstring"]
                                 if application.endswith("-job") else [])
                if application.endswith("-job"):
                    self.assertEqual(environment["Infrastructure__DialogDbConnectionString"]["secretRef"],
                                     "dbconnectionstring")
                else:
                    self.assertNotIn("Infrastructure__DialogDbConnectionString", environment)
                grants = [resource for resource in resources if resource["type"] ==
                          "Microsoft.Authorization/roleAssignments" and
                          resource["properties"]["roleDefinitionId"].endswith(SECRET_READER_ROLE)]
                self.assertEqual(len(grants), 1)
                self.assertIn("Microsoft.KeyVault/vaults", grants[0]["id"])
                self.assertNotIn("Microsoft.KeyVault/vaults/secrets", grants[0]["id"])
                self.assertNotIn("/secrets/", grants[0]["id"])

    def test_entra_mode_rejects_missing_host_and_connection_string_options(self):
        for host in ("", "validation.postgres.database.azure.com;Password=unexpected"):
            with self.subTest(host=host):
                self.application_snapshot("custom-metrics-job", "EntraToken", host=host,
                                          expected_error="EntraToken requires dbHost")

    def test_entra_api_requires_explicit_runtime_secrets(self):
        self.application_snapshot("web-api-so", "EntraToken", secrets=[],
                                  expected_error="EntraToken requires an explicit runtimeSecretNames allowlist.")

    def test_secret_reader_rejects_database_administrator_credentials(self):
        for secret in ("dialogportenAdoConnectionString", "dialogportenPsqlConnectionString",
                       "DIALOGPORTENPGADMINPASSWORD"):
            with self.subTest(secret=secret):
                self.snapshot(ROOT / ".azure/modules/keyvault/addSecretReaderRoles.bicep", f"""
param keyvaultName = 'validation-vault'
param principalId = '00000000-0000-0000-0000-000000000001'
param secretNames = ['{secret}']
""", expected_error="Token-authenticated workloads must not receive access to database administrator credentials.")


if __name__ == "__main__":
    unittest.main()
