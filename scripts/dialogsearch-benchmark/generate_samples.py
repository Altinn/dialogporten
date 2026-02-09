#!/usr/bin/env python3
import os
import sys
import subprocess

DEFAULT_TIMEOUT_SECONDS = 30


def run_query(conn_str, sql, timeout_s=DEFAULT_TIMEOUT_SECONDS):
    """Executes a SQL command via psql and returns the stdout."""
    try:
        process = subprocess.Popen(
            ['psql', conn_str, '-t', '-A', '-q', '-c', sql],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, stderr = process.communicate(timeout=timeout_s)
        if process.returncode != 0:
            if stderr:
                print(f"SQL Error: {stderr.strip()}", file=sys.stderr)
            return None
        return stdout.strip()
    except subprocess.TimeoutExpired:
        process.kill()
        process.communicate()
        print(f"SQL Error: query timed out after {timeout_s} seconds.", file=sys.stderr)
        return None
    except FileNotFoundError:
        print("Error: 'psql' not found in PATH.", file=sys.stderr)
        sys.exit(1)

def main():
    # 1. Environment and Args Check
    conn_str = os.getenv('PG_CONNECTION_STRING')
    if not conn_str:
        print("Error: PG_CONNECTION_STRING is missing.", file=sys.stderr)
        sys.exit(1)

    if len(sys.argv) < 3:
        print("Usage: ./generate_samples.py <type> <count>", file=sys.stderr)
        print("Types: party, service", file=sys.stderr)
        sys.exit(1)

    sample_type = sys.argv[1].lower()
    try:
        target_count = int(sys.argv[2])
    except ValueError:
        print("Error: Count must be an integer.", file=sys.stderr)
        sys.exit(1)
    if target_count < 1:
        print("Error: Count must be >= 1.", file=sys.stderr)
        sys.exit(1)

    # 2. Map type to Column Name
    mapping = {
        "party": '"Party"',
        "service": '"ServiceResource"'
    }

    if sample_type not in mapping:
        print(f"Error: Unknown type '{sample_type}'. Use 'party' or 'service'.", file=sys.stderr)
        sys.exit(1)

    col_name = mapping[sample_type]

    # 3. Get Row Estimate for Table
    est_sql = "SELECT reltuples::bigint FROM pg_class WHERE relname = 'Dialog' LIMIT 1"
    res = run_query(conn_str, est_sql)
    
    # Default to 1B if estimate is missing or zero
    total_est = int(res) if res and res.isdigit() and int(res) > 0 else 1000000000

    # 4. Sampling Loop
    distinct_values = set()
    iteration = 1
    
    # We use a 50x multiplier to over-sample blocks, ensuring we find distinct values faster
    percent = (target_count / total_est) * 100 * 50
    percent = min(percent, 100.0)

    while len(distinct_values) < target_count and iteration <= 20:
        prev_count = len(distinct_values)
        query = (
            f'SELECT DISTINCT {col_name} FROM "Dialog" '
            f'TABLESAMPLE SYSTEM ({percent}) REPEATABLE ({iteration}) '
            f'LIMIT {target_count}'
        )
        
        output = run_query(conn_str, query)
        
        if output:
            for val in output.splitlines():
                if val.strip():
                    distinct_values.add(val.strip())

        if len(distinct_values) == prev_count:
            percent = min(percent * 2, 100.0)
        iteration += 1

    # 5. Output Clean Results
    results = list(distinct_values)[:target_count]
    if results:
        print("\n".join(results))
    else:
        print(f"No values found for {sample_type}.", file=sys.stderr)

if __name__ == "__main__":
    main()
