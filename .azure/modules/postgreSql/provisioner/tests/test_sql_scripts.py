"""Static checks on the provisioning SQL scripts the provisioner image runs."""
from pathlib import Path
import re
import unittest

SQL_DIR = Path(__file__).resolve().parents[2] / "sql"
QUIT = re.compile(r"\\q(?:uit)?\b")


class SqlScriptTests(unittest.TestCase):
    def test_scripts_stop_with_errors_not_quit(self):
        # psql's \quit takes no exit status and exits 0, so a script that stops with it reports
        # success and the provisioner carries on with the next step. A script has to stop with an
        # SQL error instead, which ON_ERROR_STOP turns into a non-zero exit.
        scripts = sorted(SQL_DIR.glob("*.sql"))
        self.assertTrue(scripts, f"no SQL scripts found in {SQL_DIR}")
        for script in scripts:
            for number, line in enumerate(script.read_text().splitlines(), start=1):
                code = line.split("--", 1)[0]
                with self.subTest(script=script.name, line=number):
                    self.assertIsNone(QUIT.search(code), f"{script.name}:{number}: {line.strip()}")


if __name__ == "__main__":
    unittest.main()
