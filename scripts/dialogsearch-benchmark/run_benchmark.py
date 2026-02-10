#!/usr/bin/env python3
import argparse
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from collections import defaultdict
from pathlib import Path
from typing import Optional

PLACEHOLDER = "--PARTIESANDSERVICESPLACEHOLDER--"
EXPLAIN_PREFIX = "EXPLAIN (ANALYZE, BUFFERS, TIMING)"


def warn(msg: str) -> None:
    print(f"[warn] {msg}", file=sys.stderr)


def log_info(msg: str) -> None:
    print(f"[info] {msg}", file=sys.stderr)


def read_cases(case_paths: list[Path]):
    cases = []
    for path in sorted(case_paths):
        try:
            data = json.loads(path.read_text())
        except Exception as ex:
            warn(f"Failed to parse JSON: {path} ({ex})")
            continue
        parties = {p for g in data for p in g.get("Parties", [])}
        services = {s for g in data for s in g.get("Services", [])}
        cases.append(
            {
                "path": path,
                "name": path.name,
                "data": data,
                "party_count": len(parties),
                "service_count": len(services),
            }
        )
    cases.sort(key=lambda c: (c["party_count"], c["service_count"], c["name"]))
    return cases


def ensure_explain(sql_text: str) -> str:
    lines = sql_text.splitlines()
    first_non_empty = ""
    for line in lines:
        if line.strip():
            first_non_empty = line.strip()
            break
    if first_non_empty.upper().startswith(EXPLAIN_PREFIX):
        return sql_text
    return f"{EXPLAIN_PREFIX}\n{sql_text}"


def run_sql(psql_bin: str, conn: str, sql_text: str, timeout_s: int) -> str:
    with tempfile.NamedTemporaryFile("w", delete=False, suffix=".sql") as tf:
        tf.write(sql_text)
        temp_path = tf.name
    try:
        cmd = [psql_bin, conn, "-q", "-f", temp_path]
        return subprocess.check_output(cmd, text=True, stderr=subprocess.STDOUT, timeout=timeout_s)
    finally:
        try:
            os.unlink(temp_path)
        except OSError:
            pass


def clean_explain_output(output: str) -> str:
    cleaned = []
    for line in output.splitlines():
        stripped = line.strip()
        if stripped == "QUERY PLAN":
            continue
        if stripped and all(ch == "-" for ch in stripped):
            continue
        cleaned.append(line)
    return "\n".join(cleaned)


def split_patterns(patterns: str) -> list[str]:
    return [pattern.strip() for pattern in patterns.split(",") if pattern.strip()]


def parse_shared_buffers(buffer_line: str) -> tuple[Optional[int], Optional[int], Optional[int]]:
    shared_match = re.search(r"\bshared\b(?P<section>.*)", buffer_line)
    if not shared_match:
        return None, None, None
    section = shared_match.group("section")
    hit_match = re.search(r"\bhit=(\d+)", section)
    read_match = re.search(r"\bread=(\d+)", section)
    dirtied_match = re.search(r"\bdirtied=(\d+)", section)
    return (
        int(read_match.group(1)) if read_match else 0,
        int(hit_match.group(1)) if hit_match else 0,
        int(dirtied_match.group(1)) if dirtied_match else 0,
    )


def parse_explain(output: str):
    exec_time = None
    m = re.search(r"Execution Time: ([0-9.]+) ms", output)
    if m:
        exec_time = float(m.group(1))
    buf_read = buf_hit = buf_dirtied = None
    lines = output.splitlines()
    planning_start = next((idx for idx, line in enumerate(lines) if line.strip() == "Planning:"), len(lines))
    plan_buffer_lines = []
    for line in lines[:planning_start]:
        if "Buffers:" not in line:
            continue
        indent = len(line) - len(line.lstrip())
        plan_buffer_lines.append((indent, line))
    if not plan_buffer_lines:
        for line in lines:
            if "Buffers:" not in line:
                continue
            indent = len(line) - len(line.lstrip())
            plan_buffer_lines.append((indent, line))
    if plan_buffer_lines:
        min_indent = min(indent for indent, _ in plan_buffer_lines)
        selected_line = [line for indent, line in plan_buffer_lines if indent == min_indent][-1]
        buf_read, buf_hit, buf_dirtied = parse_shared_buffers(selected_line)
    return exec_time, buf_read, buf_hit, buf_dirtied


def cache_status(shared_read, shared_hit) -> str:
    if shared_read is None and shared_hit is None:
        return "-"
    if shared_read and shared_read > 0:
        return "io"
    if shared_read == 0 and shared_hit and shared_hit > 0:
        return "cached"
    if shared_read == 0 and shared_hit == 0:
        return "none"
    return "?"


def category(party_count: int, service_count: int, party_hi: int, service_hi: int) -> str:
    p = "hpc" if party_count > party_hi else "lpc"
    s = "hsc" if service_count > service_hi else "lsc"
    return f"{p}/{s}"


def render_output(results, sql_files, args) -> int:
    if not results:
        warn("No results to report")
        return 1

    # group by case
    by_case = {}
    for row in results:
        info = by_case.setdefault(
            row["case"],
            {
                "party_count": row["party_count"],
                "service_count": row["service_count"],
                "category": row["category"],
                "rows": {},
            },
        )
        info["rows"][row["variant"]] = row

    # group by category
    categories = defaultdict(list)
    for case_name, info in by_case.items():
        categories[info["category"]].append((case_name, info))

    for cat in categories:
        categories[cat].sort(key=lambda x: (x[1]["party_count"], x[1]["service_count"], x[0]))

    variants = [s["name"] for s in sql_files]

    if args.csv:
        print(
            "category,case,party_count,service_count,variant,exec_ms,shared_read,shared_hit,shared_dirtied,cache_status"
        )
        for cat in sorted(categories.keys()):
            for case_name, info in categories[cat]:
                for v in variants:
                    row = info["rows"].get(v)
                    if not row:
                        continue
                    print(
                        f"{cat},{case_name},{info['party_count']},{info['service_count']},{v},"
                        f"{row['exec_ms']},{row['shared_read']},{row['shared_hit']},"
                        f"{row['shared_dirtied']},{row['cache_status']}"
                    )
        return 0

    # Markdown output
    for cat in sorted(categories.keys()):
        print(f"## {cat}")
        header = "| case | p | s | " + " | ".join(
            f"{v} (ms/read/hit/dirtied/cache)" for v in variants
        ) + " |"
        sep = "| --- | ---: | ---: | " + " | ".join("---" for _ in variants) + " |"
        print(header)
        print(sep)
        for case_name, info in categories[cat]:
            cells = []
            for v in variants:
                row = info["rows"].get(v)
                if not row or row["exec_ms"] is None:
                    cells.append("-/-/-/-/-")
                    continue
                read = row["shared_read"] if row["shared_read"] is not None else "-"
                hit = row["shared_hit"] if row["shared_hit"] is not None else "-"
                dirtied = row["shared_dirtied"] if row["shared_dirtied"] is not None else "-"
                cells.append(
                    f"{row['exec_ms']:.2f}/{read}/{hit}/{dirtied}/{row['cache_status']}"
                )
            print(
                f"| {case_name} | {info['party_count']} | {info['service_count']} | "
                + " | ".join(cells)
                + " |"
            )
        print()
    return 0


def main():
    parser = argparse.ArgumentParser(description="Run DialogSearch SQL variants across test cases.")
    parser.add_argument(
        "--cases",
        required=True,
        help="Comma-separated quoted glob(s) for JSON cases (e.g. 'defaultset/*.json')",
    )
    parser.add_argument(
        "--sqls",
        required=True,
        help="Comma-separated quoted glob(s) for SQL files (e.g. 'sql/*.sql')",
    )
    parser.add_argument("--csv", action="store_true", help="Emit CSV instead of Markdown")
    parser.add_argument("--print-explain", action="store_true", help="Print full EXPLAIN output")
    parser.add_argument("--party-hi", type=int, default=10, help="High party threshold (default: 10)")
    parser.add_argument("--service-hi", type=int, default=20, help="High service threshold (default: 20)")
    parser.add_argument("--timeout", type=int, default=30, help="Per-run timeout in seconds (default: 30)")
    args = parser.parse_args()

    psql_bin = shutil.which("psql")
    if not psql_bin:
        warn("psql not found in PATH")
        return 1

    conn = os.getenv("PG_CONNECTION_STRING")
    if not conn:
        warn("PG_CONNECTION_STRING not set")
        return 1

    case_globs = split_patterns(args.cases)
    case_paths = []
    for pattern in case_globs:
        case_paths.extend(Path(p) for p in glob.glob(pattern))
    case_paths = [p for p in case_paths if p.is_file()]

    if not case_paths:
        warn(f"No case files found for: {args.cases}")
        return 1

    cases = read_cases(case_paths)
    if not cases:
        warn("No JSON cases found")
        return 1

    sql_files = []
    sql_globs = split_patterns(args.sqls)
    sql_paths = []
    for pattern in sql_globs:
        sql_paths.extend(Path(p) for p in glob.glob(pattern))
    sql_paths = [p for p in sql_paths if p.is_file()]
    for path in sorted(sql_paths):
        sql_text = path.read_text()
        if PLACEHOLDER not in sql_text:
            warn(f"Placeholder missing in {path}, skipping")
            continue
        sql_text = ensure_explain(sql_text)
        sql_files.append({"path": path, "name": path.stem, "sql_text": sql_text})

    if not sql_files:
        warn(f"No SQL files to run for: {args.sqls}")
        return 1

    total_runs = len(cases) * len(sql_files)
    log_info(f"Found {len(sql_files)} SQL files and {len(cases)} test cases ({total_runs} runs total)")

    results = []
    interrupted = False

    run_index = 0
    index_width = len(str(total_runs))
    try:
        for case in cases:
            case_cat = category(case["party_count"], case["service_count"], args.party_hi, args.service_hi)
            case_json = json.dumps(case["data"])
            for sql in sql_files:
                run_index += 1
                log_info(
                    f"[{run_index:0{index_width}d}/{total_runs}] "
                    f"Running {sql['path'].name} on {case['name']}"
                )
                filled = sql["sql_text"].replace(PLACEHOLDER, case_json)
                try:
                    out = run_sql(psql_bin, conn, filled, args.timeout)
                except subprocess.TimeoutExpired:
                    warn(f"Timeout after {args.timeout}s: {sql['path'].name} on {case['name']}")
                    exec_ms = buf_read = buf_hit = buf_dirtied = None
                except subprocess.CalledProcessError as ex:
                    warn(
                        (
                            f"SQL error for {sql['path'].name} on {case['name']}: "
                            f"exit code {ex.returncode}"
                        )
                    )
                    if ex.output:
                        warn(ex.output.strip())
                    exec_ms = buf_read = buf_hit = buf_dirtied = None
                else:
                    if args.print_explain:
                        print(
                            f"EXPLAIN {sql['path'].name} {case['name']}",
                            file=sys.stderr,
                        )
                        cleaned = clean_explain_output(out)
                        print(cleaned.rstrip(), file=sys.stderr)
                    exec_ms, buf_read, buf_hit, buf_dirtied = parse_explain(out)
                results.append(
                    {
                        "case": case["name"],
                        "party_count": case["party_count"],
                        "service_count": case["service_count"],
                        "category": case_cat,
                        "variant": sql["name"],
                        "exec_ms": exec_ms,
                        "shared_read": buf_read,
                        "shared_hit": buf_hit,
                        "shared_dirtied": buf_dirtied,
                        "cache_status": cache_status(buf_read, buf_hit),
                    }
                )
    except KeyboardInterrupt:
        interrupted = True
        warn("Interrupted by user; printing collected results")

    exit_code = render_output(results, sql_files, args)
    if interrupted:
        return 130 if exit_code == 0 else exit_code
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
