"""Run provision-entra-principal.sql with a real psql against a disposable PostgreSQL server.

The script refuses a role it cannot safely map, and the provisioner only stops when psql then
exits non-zero, which a stubbed psql cannot show. The test creates a database and roles, so it
runs only when PROVISIONER_TEST_DATABASE_URL is a postgresql:// URL for a disposable server, as
in the provisioning workflow's pull request job. Never point it at a shared server.

The Azure pgaadauth_* functions the script calls are replaced by stand-ins in the test database.
"""
import os
from pathlib import Path
import shutil
import subprocess
import unittest
from urllib.parse import urlsplit, urlunsplit
import uuid

SQL = Path(__file__).resolve().parents[2] / "sql" / "provision-entra-principal.sql"
SERVER_URL = os.environ.get("PROVISIONER_TEST_DATABASE_URL", "")
OBJECT_ID = "11111111-1111-1111-1111-111111111111"
OTHER_OBJECT_ID = "22222222-2222-2222-2222-222222222222"

STAND_INS = """
CREATE TABLE stub_principals (
  rolname text, principaltype text, objectid text, tenantid text, ismfa boolean, isadmin boolean
);
CREATE FUNCTION pgaadauth_list_principals(boolean)
  RETURNS TABLE (rolname text, principaltype text, objectid text, tenantid text, ismfa boolean,
                 isadmin boolean)
  LANGUAGE sql AS $$ SELECT * FROM stub_principals $$;
CREATE FUNCTION pgaadauth_create_principal_with_oid(text, text, text, boolean, boolean)
  RETURNS text LANGUAGE plpgsql AS $$
BEGIN
  EXECUTE format('CREATE ROLE %I', $1);
  INSERT INTO stub_principals VALUES ($1, $3, $2, 'stub-tenant', $5, $4);
  RETURN 'created';
END $$;
"""


def psql(url, *args, sql=None):
    return subprocess.run(["psql", "--no-psqlrc", "-v", "ON_ERROR_STOP=1", "-d", url, *args],
                          input=sql, text=True, capture_output=True, check=False)


def run_sql(url, sql):
    result = psql(url, "--quiet", sql=sql)
    if result.returncode != 0:
        raise AssertionError(f"psql exited {result.returncode}: {result.stderr}")


def query(url, sql):
    result = psql(url, "-At", "-c", sql)
    if result.returncode != 0:
        raise AssertionError(f"psql exited {result.returncode}: {result.stderr}")
    return result.stdout.strip()


@unittest.skipUnless(SERVER_URL, "PROVISIONER_TEST_DATABASE_URL names no disposable PostgreSQL server")
class PrincipalScriptTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not shutil.which("psql"):
            raise RuntimeError("PROVISIONER_TEST_DATABASE_URL is set, but psql is not on PATH")
        suffix = uuid.uuid4().hex[:8]
        cls.prefix = f"provisioner-test-{suffix}"
        database = f"provisioner_test_{suffix}"
        run_sql(SERVER_URL, f'CREATE DATABASE "{database}"')
        cls.addClassCleanup(run_sql, SERVER_URL, f'DROP DATABASE IF EXISTS "{database}" WITH (FORCE)')
        cls.url = urlunsplit(urlsplit(SERVER_URL)._replace(path=f"/{database}"))
        run_sql(cls.url, STAND_INS)

    def role(self, name, mapped_to=None):
        # mapped_to None leaves the role to the script; "" creates it without an Entra mapping.
        role = f"{self.prefix}-{name}"
        self.addCleanup(run_sql, SERVER_URL, f'DROP ROLE IF EXISTS "{role}"')
        if mapped_to is not None:
            run_sql(self.url, f'CREATE ROLE "{role}"')
            if mapped_to:
                run_sql(self.url, f"INSERT INTO stub_principals "
                                  f"VALUES ('{role}', 'service', '{mapped_to}', 'stub-tenant', false, false)")
        return role

    def provision(self, role):
        return psql(self.url, "-v", f"role_name={role}", "-v", f"mi_oid={OBJECT_ID}", "-f", str(SQL))

    def can_log_in(self, role):
        return query(self.url, f"SELECT rolcanlogin FROM pg_roles WHERE rolname = '{role}'") == "t"

    def test_role_without_entra_mapping_is_refused(self):
        role = self.role("plain", mapped_to="")
        result = self.provision(role)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.can_log_in(role))

    def test_role_mapped_to_another_object_id_is_refused(self):
        role = self.role("stale", mapped_to=OTHER_OBJECT_ID)
        result = self.provision(role)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.can_log_in(role))

    def test_missing_principal_is_created_and_can_log_in(self):
        role = self.role("new")
        result = self.provision(role)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(self.can_log_in(role))
        self.assertEqual(query(self.url, f"SELECT objectid FROM stub_principals WHERE rolname = '{role}'"),
                         OBJECT_ID)

    def test_principal_mapped_to_the_expected_object_id_can_log_in(self):
        role = self.role("mapped", mapped_to=OBJECT_ID)
        result = self.provision(role)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(self.can_log_in(role))


if __name__ == "__main__":
    unittest.main()
