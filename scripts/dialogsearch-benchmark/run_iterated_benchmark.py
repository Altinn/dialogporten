#!/usr/bin/env python3
import argparse
import csv
import datetime as dt
import math
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple


def die(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    raise SystemExit(1)


def warn(message: str) -> None:
    print(f"warning: {message}", file=sys.stderr)


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def run_command(cmd: List[str], cwd: Optional[Path] = None) -> Tuple[int, str, str]:
    process = subprocess.Popen(
        cmd,
        cwd=str(cwd) if cwd else None,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    stdout, stderr = process.communicate()
    return process.returncode, stdout, stderr


def log_command(cmd: List[str]) -> None:
    print(f"[cmd] {' '.join(cmd)}", file=sys.stderr)


def log_info(message: str) -> None:
    print(f"[info] {message}", file=sys.stderr)


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def parse_explains(output: str) -> Dict[Tuple[str, str], List[str]]:
    blocks: Dict[Tuple[str, str], List[str]] = {}
    current_key = None
    current_lines: List[str] = []
    header_pattern = re.compile(r"^EXPLAIN\s+(\S+)\s+(\S+)")

    def flush():
        nonlocal current_key, current_lines
        if current_key is None:
            return
        blocks.setdefault(current_key, []).extend(current_lines)
        current_key = None
        current_lines = []

    for line in output.splitlines():
        match = header_pattern.match(line.strip())
        if match:
            flush()
            sql_name = Path(match.group(1)).stem
            case_name = Path(match.group(2)).stem
            current_key = (case_name, sql_name)
            current_lines = []
            continue
        if current_key is not None:
            current_lines.append(line)
    flush()
    return blocks


def percentile(values: List[float], pct: float) -> float:
    if not values:
        return math.nan
    ordered = sorted(values)
    if pct <= 0:
        return ordered[0]
    if pct >= 100:
        return ordered[-1]
    rank = (pct / 100) * (len(ordered) - 1)
    low = int(math.floor(rank))
    high = int(math.ceil(rank))
    if low == high:
        return ordered[low]
    weight = rank - low
    return ordered[low] * (1 - weight) + ordered[high] * weight


def summarize(values: List[float]) -> Dict[str, float]:
    if not values:
        return {
            "avg": math.nan,
            "min": math.nan,
            "max": math.nan,
            "p50": math.nan,
            "p95": math.nan,
            "p99": math.nan,
        }
    return {
        "avg": sum(values) / len(values),
        "min": min(values),
        "max": max(values),
        "p50": percentile(values, 50),
        "p95": percentile(values, 95),
        "p99": percentile(values, 99),
    }


def parse_csv_rows(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def safe_float(value: Optional[str]) -> Optional[float]:
    if value is None or value == "" or value == "None":
        return None
    try:
        return float(value)
    except ValueError:
        return None


def safe_int(value: Optional[str]) -> Optional[int]:
    if value is None or value == "" or value == "None":
        return None
    try:
        return int(float(value))
    except ValueError:
        return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Run iterated benchmarks with generated samples.")
    parser.add_argument("--party-pool", type=int, required=True, help="Total party pool size")
    parser.add_argument("--service-pool", type=int, required=True, help="Total service pool size")
    parser.add_argument(
        "--generate-set",
        required=True,
        help="Semicolon-separated list of parties,services,groups",
    )
    parser.add_argument("--sqls", required=True, help="Quoted glob(s) for SQL files")
    parser.add_argument("--iterations", type=int, required=True, help="Number of iterations")
    parser.add_argument("--seed", type=int, required=True, help="Base seed")
    parser.add_argument(
        "--padding",
        type=int,
        default=3,
        help="Zero padding width for iteration numbers (default: 3)",
    )
    parser.add_argument(
        "--out-dir",
        help="Output directory (default: benchmark-YYYYMMDD-HHMM in cwd)",
    )

    args = parser.parse_args()

    if args.iterations < 1:
        die("iterations must be >= 1")
    if args.party_pool < 1 or args.service_pool < 1:
        die("party-pool and service-pool must be >= 1")

    if args.out_dir:
        root_dir = Path(args.out_dir)
    else:
        timestamp = dt.datetime.now().strftime("%Y%m%d-%H%M")
        root_dir = Path.cwd() / f"benchmark-{timestamp}"
    casesets_dir = root_dir / "casesets"
    output_dir = root_dir / "output"
    csvs_dir = output_dir / "csvs"
    explains_dir = output_dir / "explains"

    ensure_dir(casesets_dir)
    ensure_dir(csvs_dir)
    ensure_dir(explains_dir)

    parties_path = output_dir / "parties.txt"
    services_path = output_dir / "services.txt"

    log_info("Generating party samples")
    log_command([sys.executable, "generate_samples.py", "party", str(args.party_pool)])
    code, stdout, stderr = run_command(
        [sys.executable, "generate_samples.py", "party", str(args.party_pool)]
    )
    if code != 0:
        print(stderr, file=sys.stderr)
        die("generate_samples.py party failed")
    if not stdout.strip():
        if stderr.strip():
            print(stderr, file=sys.stderr)
        die("generate_samples.py party returned no data")
    write_text(parties_path, stdout.strip() + ("\n" if stdout.strip() else ""))

    log_info("Generating service samples")
    log_command([sys.executable, "generate_samples.py", "service", str(args.service_pool)])
    code, stdout, stderr = run_command(
        [sys.executable, "generate_samples.py", "service", str(args.service_pool)]
    )
    if code != 0:
        print(stderr, file=sys.stderr)
        die("generate_samples.py service failed")
    if not stdout.strip():
        if stderr.strip():
            print(stderr, file=sys.stderr)
        die("generate_samples.py service returned no data")
    write_text(services_path, stdout.strip() + ("\n" if stdout.strip() else ""))

    aggregate_rows: List[Dict[str, str]] = []
    details_rows: List[Dict[str, str]] = []
    explain_catalog: List[Tuple[str, str]] = []

    for iteration in range(args.iterations):
        iter_seed = args.seed + iteration
        iter_name = f"{iter_seed:0{args.padding}d}"
        log_info(f"Iteration {iteration + 1}/{args.iterations} (seed {iter_seed})")
        iter_cases_dir = casesets_dir / iter_name
        iter_explains_dir = explains_dir / iter_name
        ensure_dir(iter_cases_dir)
        ensure_dir(iter_explains_dir)
        log_command(
            [
                sys.executable,
                "generate_cases.py",
                "--parties-path",
                str(parties_path),
                "--services-path",
                str(services_path),
                "--out-dir",
                str(iter_cases_dir),
                "--seed",
                str(iter_seed),
                "--omit-seed-in-filename",
                "--generate-set",
                args.generate_set,
            ]
        )
        code, stdout, stderr = run_command(
            [
                sys.executable,
                "generate_cases.py",
                "--parties-path",
                str(parties_path),
                "--services-path",
                str(services_path),
                "--out-dir",
                str(iter_cases_dir),
                "--seed",
                str(iter_seed),
                "--omit-seed-in-filename",
                "--generate-set",
                args.generate_set,
            ]
        )
        if code != 0:
            print(stderr, file=sys.stderr)
            die(f"generate_cases.py failed for iteration {iter_name}")

        csv_path = csvs_dir / f"{iter_name}.csv"
        log_command(
            [
                sys.executable,
                "run_benchmark.py",
                "--cases",
                str(iter_cases_dir / "*.json"),
                "--sqls",
                args.sqls,
                "--csv",
                "--print-explain",
            ]
        )
        code, stdout, stderr = run_command(
            [
                sys.executable,
                "run_benchmark.py",
                "--cases",
                str(iter_cases_dir / "*.json"),
                "--sqls",
                args.sqls,
                "--csv",
                "--print-explain",
            ]
        )
        write_text(csv_path, stdout)

        if stderr.strip():
            explain_blocks = parse_explains(stderr)
            for (case_name, sql_name), lines in explain_blocks.items():
                filename = f"{case_name}__{sql_name}.txt"
                explain_path = iter_explains_dir / filename
                write_text(explain_path, "\n".join(lines).rstrip() + "\n")
                explain_catalog.append((filename, "\n".join(lines).rstrip()))

        if code != 0:
            warn(f"run_benchmark.py returned {code} for iteration {iter_name}")

        if csv_path.exists():
            iteration_rows = parse_csv_rows(csv_path)
            aggregate_rows.extend(iteration_rows)

    log_info("Writing summary and concatenated explains")
    summary_path = root_dir / "summary.csv"
    summary_rows: List[Dict[str, str]] = []

    grouped: Dict[Tuple[str, str], Dict[str, List[float]]] = {}
    meta: Dict[Tuple[str, str], Dict[str, str]] = {}

    for row in aggregate_rows:
        case_name = row.get("case") or ""
        variant = row.get("variant") or ""
        if not case_name or not variant:
            continue
        key = (variant, case_name)
        meta[key] = {
            "variant": variant,
            "case": case_name,
            "category": row.get("category") or "",
            "party_count": row.get("party_count") or "",
            "service_count": row.get("service_count") or "",
        }
        bucket = grouped.setdefault(key, {"exec_ms": [], "shared_read": [], "shared_hit": []})
        exec_ms = safe_float(row.get("exec_ms"))
        if exec_ms is not None:
            bucket["exec_ms"].append(exec_ms)
        shared_read = safe_int(row.get("shared_read"))
        if shared_read is not None:
            bucket["shared_read"].append(float(shared_read))
        shared_hit = safe_int(row.get("shared_hit"))
        if shared_hit is not None:
            bucket["shared_hit"].append(float(shared_hit))

    for key, values in grouped.items():
        meta_row = meta.get(key, {})
        exec_stats = summarize(values["exec_ms"])
        read_stats = summarize(values["shared_read"])
        hit_stats = summarize(values["shared_hit"])
        summary_rows.append(
            {
                **meta_row,
                "exec_avg": f"{exec_stats['avg']:.4f}" if not math.isnan(exec_stats["avg"]) else "",
                "exec_min": f"{exec_stats['min']:.4f}" if not math.isnan(exec_stats["min"]) else "",
                "exec_max": f"{exec_stats['max']:.4f}" if not math.isnan(exec_stats["max"]) else "",
                "exec_p50": f"{exec_stats['p50']:.4f}" if not math.isnan(exec_stats["p50"]) else "",
                "exec_p95": f"{exec_stats['p95']:.4f}" if not math.isnan(exec_stats["p95"]) else "",
                "exec_p99": f"{exec_stats['p99']:.4f}" if not math.isnan(exec_stats["p99"]) else "",
                "read_avg": f"{read_stats['avg']:.2f}" if not math.isnan(read_stats["avg"]) else "",
                "read_min": f"{read_stats['min']:.0f}" if not math.isnan(read_stats["min"]) else "",
                "read_max": f"{read_stats['max']:.0f}" if not math.isnan(read_stats["max"]) else "",
                "read_p50": f"{read_stats['p50']:.0f}" if not math.isnan(read_stats["p50"]) else "",
                "read_p95": f"{read_stats['p95']:.0f}" if not math.isnan(read_stats["p95"]) else "",
                "read_p99": f"{read_stats['p99']:.0f}" if not math.isnan(read_stats["p99"]) else "",
                "hit_avg": f"{hit_stats['avg']:.2f}" if not math.isnan(hit_stats["avg"]) else "",
                "hit_min": f"{hit_stats['min']:.0f}" if not math.isnan(hit_stats["min"]) else "",
                "hit_max": f"{hit_stats['max']:.0f}" if not math.isnan(hit_stats["max"]) else "",
                "hit_p50": f"{hit_stats['p50']:.0f}" if not math.isnan(hit_stats["p50"]) else "",
                "hit_p95": f"{hit_stats['p95']:.0f}" if not math.isnan(hit_stats["p95"]) else "",
                "hit_p99": f"{hit_stats['p99']:.0f}" if not math.isnan(hit_stats["p99"]) else "",
            }
        )

    summary_fields = [
        "variant",
        "case",
        "category",
        "party_count",
        "service_count",
        "exec_avg",
        "exec_min",
        "exec_max",
        "exec_p50",
        "exec_p95",
        "exec_p99",
        "read_avg",
        "read_min",
        "read_max",
        "read_p50",
        "read_p95",
        "read_p99",
        "hit_avg",
        "hit_min",
        "hit_max",
        "hit_p50",
        "hit_p95",
        "hit_p99",
    ]

    with summary_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=summary_fields)
        writer.writeheader()
        writer.writerows(summary_rows)

    explains_all_path = root_dir / "explains_all.txt"
    with explains_all_path.open("w", encoding="utf-8") as handle:
        for filename, content in explain_catalog:
            handle.write(f"== {filename} ==\n")
            handle.write(content)
            handle.write("\n\n")

    log_info("Generating Excel summary")
    excel_path = root_dir / "summary.xlsx"
    log_command(
        [
            sys.executable,
            "generate_excel_summary.py",
            str(summary_path),
            "--out",
            str(excel_path),
        ]
    )
    code, stdout, stderr = run_command(
        [sys.executable, "generate_excel_summary.py", str(summary_path), "--out", str(excel_path)]
    )
    if code != 0:
        print(stderr, file=sys.stderr)
        warn("generate_excel_summary.py failed")

    print(str(root_dir))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
