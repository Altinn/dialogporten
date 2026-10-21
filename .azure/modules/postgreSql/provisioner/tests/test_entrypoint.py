"""Exercise provisioning control flow without Azure credentials or a database."""
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

ENTRYPOINT = Path(__file__).resolve().parents[1] / "entrypoint.sh"


class ProvisionerTests(unittest.TestCase):
    def run_provisioner(self, *, federation=False, fail_preflight=False,
                        workloads=None, token_source=True):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            log = root / "calls"
            for command, body in {
                "curl": 'printf \'{"access_token":"fixture-token"}\\n\'\n',
                "psql": '''[[ "$PGSSLMODE" == verify-full ]] || exit 91
[[ "$PGSSLROOTCERT" == system ]] || exit 92
[[ "$PGPASSWORD" == fixture-token ]] || exit 93
printf '%s\\n' "$*" >> "$CALL_LOG"
if [[ "$*" == *require-pgaudit.sql* && "$FAIL_PREFLIGHT" == 1 ]]; then exit 1; fi
''',
            }.items():
                path = root / command
                path.write_text("#!/usr/bin/env bash\nset -euo pipefail\n" + body)
                path.chmod(0o755)
            env = {
                "PATH": f"{root}:{os.environ['PATH']}",
                "PGHOST": "fixture.postgres.database.azure.com",
                "PG_ADMIN_ROLE": "fixture-admin",
                "AZURE_CLIENT_ID": "fixture-client",
                "PROVISION_WORKLOADS": json.dumps(workloads if workloads is not None else [
                    {"roleName": "fixture-role", "objectId": "fixture-id", "profile": "dp_api_dml"}
                ]),
                "CALL_LOG": str(log),
                "FAIL_PREFLIGHT": "1" if fail_preflight else "0",
            }
            if token_source:
                if federation:
                    token = root / "assertion"
                    token.write_text("fixture-assertion")
                    env.update(AZURE_FEDERATED_TOKEN_FILE=str(token), AZURE_TENANT_ID="fixture-tenant")
                else:
                    env.update(IDENTITY_ENDPOINT="https://fixture.invalid", IDENTITY_HEADER="fixture-header")
            result = subprocess.run(["bash", str(ENTRYPOINT)], env=env, capture_output=True, text=True)
            return result, log.read_text().splitlines() if log.exists() else []

    def test_both_token_sources_check_prerequisites_before_mutations(self):
        for federation in (False, True):
            with self.subTest(federation=federation):
                result, calls = self.run_provisioner(federation=federation)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(len(calls), 4)
                for call, script in zip(calls, ("require-pgaudit.sql", "provision-entra-principal.sql",
                                                "provision-profile-roles.sql", "provision-membership.sql")):
                    self.assertIn(script, call)
                self.assertIn("--dbname dialogporten", calls[0])
                self.assertNotIn("fixture-token", result.stdout + result.stderr)

    def test_failed_preflight_stops_before_any_mutation(self):
        result, calls = self.run_provisioner(fail_preflight=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(len(calls), 1)
        self.assertIn("require-pgaudit.sql", calls[0])

    def test_invalid_entry_never_reaches_database(self):
        result, calls = self.run_provisioner(workloads=[{"roleName": "missing-fields"}])
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(calls, [])

    def test_missing_token_source_never_reaches_database(self):
        result, calls = self.run_provisioner(token_source=False)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(calls, [])

    def test_empty_workloads_need_no_token_or_database(self):
        result, calls = self.run_provisioner(workloads=[], token_source=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(calls, [])
