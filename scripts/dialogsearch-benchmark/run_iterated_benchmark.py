#!/usr/bin/env python3
import argparse
import csv
import datetime as dt
import glob
import io
import math
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple, cast

SCRIPT_DIR = Path(__file__).resolve().parent

try:
    from stats_utils import summarize
except ModuleNotFoundError:
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


def die(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    raise SystemExit(1)


def warn(message: str) -> None:
    print(f"warning: {message}", file=sys.stderr)


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def run_command(
    cmd: List[str], cwd: Optional[Path] = None, timeout_s: Optional[int] = None
) -> Tuple[int, str, str]:
    try:
        process = subprocess.run(
            cmd,
            cwd=str(cwd) if cwd else None,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=timeout_s,
            check=False,
        )
    except subprocess.TimeoutExpired as exc:
        stdout = exc.stdout or ""
        stderr = (exc.stderr or "").rstrip()
        timeout_suffix = f"error: command timed out after {timeout_s} seconds"
        stderr = f"{stderr}\n{timeout_suffix}" if stderr else timeout_suffix
        return 124, stdout, stderr
    return process.returncode, process.stdout, process.stderr


def log_command(cmd: List[str]) -> None:
    print(f"[cmd] {' '.join(cmd)}", file=sys.stderr)


def log_info(message: str) -> None:
    print(f"[info] {message}", file=sys.stderr)


def split_patterns(patterns: str) -> List[str]:
    return [pattern.strip() for pattern in patterns.split(",") if pattern.strip()]


def resolve_globs(patterns: str) -> List[Path]:
    globs = split_patterns(patterns)
    paths: List[Path] = []
    for pattern in globs:
        paths.extend(Path(p) for p in glob.glob(pattern))
    return [p for p in paths if p.is_file()]


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


def parse_csv_text(content: str) -> List[Dict[str, str]]:
    handle = io.StringIO(content)
    return list(csv.DictReader(handle))


def write_csv_rows(path: Path, rows: List[Dict[str, str]], fieldnames: List[str]) -> None:
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def rotated_paths(paths: List[Path], offset: int) -> List[Path]:
    if not paths:
        return []
    normalized = offset % len(paths)
    return paths[normalized:] + paths[:normalized]


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


SUMMARY_FIELDS = [
    "variant",
    "case",
    "category",
    "party_count",
    "service_count",
    "samples",
    "attempted",
    "completion_rate_pct",
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


def build_summary_rows(rows: List[Dict[str, str]]) -> List[Dict[str, str]]:
    summary_rows: List[Dict[str, str]] = []
    grouped: Dict[Tuple[str, str], Dict[str, object]] = {}
    meta: Dict[Tuple[str, str], Dict[str, str]] = {}

    for row in rows:
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
        bucket = grouped.setdefault(
            key, {"exec_ms": [], "shared_read": [], "shared_hit": [], "attempted": 0}
        )
        bucket["attempted"] = int(bucket["attempted"]) + 1
        exec_ms = safe_float(row.get("exec_ms"))
        if exec_ms is not None:
            cast(List[float], bucket["exec_ms"]).append(exec_ms)
        shared_read = safe_int(row.get("shared_read"))
        if shared_read is not None:
            cast(List[float], bucket["shared_read"]).append(float(shared_read))
        shared_hit = safe_int(row.get("shared_hit"))
        if shared_hit is not None:
            cast(List[float], bucket["shared_hit"]).append(float(shared_hit))

    for key, values in grouped.items():
        meta_row = meta.get(key, {})
        exec_values = cast(List[float], values["exec_ms"])
        read_values = cast(List[float], values["shared_read"])
        hit_values = cast(List[float], values["shared_hit"])
        attempted = int(values["attempted"])
        samples = len(exec_values)
        completion_rate = (samples / attempted * 100.0) if attempted > 0 else math.nan
        exec_stats = summarize(exec_values)
        read_stats = summarize(read_values)
        hit_stats = summarize(hit_values)
        summary_rows.append(
            {
                **meta_row,
                "samples": str(samples),
                "attempted": str(attempted),
                "completion_rate_pct": (
                    f"{completion_rate:.2f}" if not math.isnan(completion_rate) else ""
                ),
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

    return sorted(summary_rows, key=lambda row: (row.get("variant", ""), row.get("case", "")))


def main() -> int:
    parser = argparse.ArgumentParser(description="Run iterated benchmarks with generated samples.")
    party_group = parser.add_mutually_exclusive_group(required=True)
    party_group.add_argument(
        "--generate-party-pool-with-count",
        type=int,
        help="Generate party pool with the given size",
    )
    party_group.add_argument(
        "--with-party-pool-file",
        help="Use an existing party pool file (one value per line)",
    )

    service_group = parser.add_mutually_exclusive_group(required=True)
    service_group.add_argument(
        "--generate-service-pool-with-count",
        type=int,
        help="Generate service pool with the given size",
    )
    service_group.add_argument(
        "--with-service-pool-file",
        help="Use an existing service pool file (one value per line)",
    )
    parser.add_argument(
        "--generate-set",
        required=True,
        help="Semicolon-separated list of parties,services,groups",
    )
    parser.add_argument("--sqls", required=True, help="Comma-separated quoted glob(s) for SQL files")
    parser.add_argument("--iterations", type=int, required=True, help="Number of iterations")
    parser.add_argument("--seed", type=int, required=True, help="Base seed")
    parser.add_argument(
        "--rounds-per-iteration",
        type=int,
        default=2,
        help=(
            "Number of fairness rounds per iteration (default: 2). "
            "Each round runs each SQL separately with rotated order."
        ),
    )
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
    parser.add_argument(
        "--script-timeout",
        type=int,
        default=600,
        help="Timeout in seconds for each child script invocation (default: 600)",
    )

    args = parser.parse_args()

    if args.iterations < 1:
        die("iterations must be >= 1")
    if args.generate_party_pool_with_count is not None and args.generate_party_pool_with_count < 1:
        die("generate-party-pool-with-count must be >= 1")
    if args.generate_service_pool_with_count is not None and args.generate_service_pool_with_count < 1:
        die("generate-service-pool-with-count must be >= 1")
    if args.rounds_per_iteration < 1:
        die("rounds-per-iteration must be >= 1")
    if args.script_timeout < 1:
        die("script-timeout must be >= 1")

    now = dt.datetime.now()
    timestamp = now.strftime("%Y%m%d%H%M")
    timestamp_for_dir = now.strftime("%Y%m%d-%H%M")
    if args.out_dir:
        root_dir = Path(args.out_dir)
    else:
        root_dir = Path.cwd() / f"benchmark-{timestamp_for_dir}"
    casesets_dir = root_dir / "casesets"
    output_dir = root_dir / "output"
    sqls_dir = root_dir / "sqls"
    csvs_dir = output_dir / "csvs"
    explains_dir = output_dir / "explains"

    ensure_dir(casesets_dir)
    ensure_dir(csvs_dir)
    ensure_dir(explains_dir)
    ensure_dir(sqls_dir)

    parties_path = output_dir / "parties.txt"
    services_path = output_dir / "services.txt"

    sql_paths = resolve_globs(args.sqls)
    if not sql_paths:
        die(f"No SQL files found for: {args.sqls}")
    log_info(f"Copying {len(sql_paths)} SQL files into {sqls_dir}")
    copied_sql_paths: List[Path] = []
    for path in sorted(sql_paths):
        copied = sqls_dir / path.name
        shutil.copy2(path, copied)
        copied_sql_paths.append(copied)

    if args.with_party_pool_file:
        src = Path(args.with_party_pool_file)
        if not src.is_file():
            die(f"Party pool file not found: {src}")
        log_info(f"Using existing party pool file: {src}")
        shutil.copy2(src, parties_path)
    else:
        log_info("Generating party samples")
        cmd = [
            sys.executable,
            str(SCRIPT_DIR / "generate_samples.py"),
            "party",
            str(args.generate_party_pool_with_count),
        ]
        log_command(cmd)
        code, stdout, stderr = run_command(cmd, timeout_s=args.script_timeout)
        if code != 0:
            print(stderr, file=sys.stderr)
            die("generate_samples.py party failed")
        if not stdout.strip():
            if stderr.strip():
                print(stderr, file=sys.stderr)
            die("generate_samples.py party returned no data")
        write_text(parties_path, stdout.strip() + "\n")

    if args.with_service_pool_file:
        src = Path(args.with_service_pool_file)
        if not src.is_file():
            die(f"Service pool file not found: {src}")
        log_info(f"Using existing service pool file: {src}")
        shutil.copy2(src, services_path)
    else:
        log_info("Generating service samples")
        cmd = [
            sys.executable,
            str(SCRIPT_DIR / "generate_samples.py"),
            "service",
            str(args.generate_service_pool_with_count),
        ]
        log_command(cmd)
        code, stdout, stderr = run_command(cmd, timeout_s=args.script_timeout)
        if code != 0:
            print(stderr, file=sys.stderr)
            die("generate_samples.py service failed")
        if not stdout.strip():
            if stderr.strip():
                print(stderr, file=sys.stderr)
            die("generate_samples.py service returned no data")
        write_text(services_path, stdout.strip() + "\n")

    aggregate_rows: List[Dict[str, str]] = []
    explain_catalog: List[Tuple[str, str]] = []

    for iteration in range(args.iterations):
        iter_seed = args.seed + iteration
        iter_name = f"{iter_seed:0{args.padding}d}"
        log_info(f"Iteration {iteration + 1}/{args.iterations} (seed {iter_seed})")
        iter_cases_dir = casesets_dir / iter_name
        iter_explains_dir = explains_dir / iter_name
        ensure_dir(iter_cases_dir)
        ensure_dir(iter_explains_dir)
        cmd = [
            sys.executable,
            str(SCRIPT_DIR / "generate_cases.py"),
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
        log_command(cmd)
        code, stdout, stderr = run_command(cmd, timeout_s=args.script_timeout)
        if code != 0:
            print(stderr, file=sys.stderr)
            die(f"generate_cases.py failed for iteration {iter_name}")

        csv_path = csvs_dir / f"{iter_name}.csv"
        iteration_rows: List[Dict[str, str]] = []
        sql_count = len(copied_sql_paths)
        for round_index in range(args.rounds_per_iteration):
            rotation_offset = iteration + (round_index // 2)
            if round_index % 2 == 0:
                ordered_sql_paths = rotated_paths(copied_sql_paths, rotation_offset)
            else:
                ordered_sql_paths = rotated_paths(
                    list(reversed(copied_sql_paths)), rotation_offset
                )
            for position, sql_path in enumerate(ordered_sql_paths, start=1):
                cmd = [
                    sys.executable,
                    str(SCRIPT_DIR / "run_benchmark.py"),
                    "--cases",
                    str(iter_cases_dir / "*.json"),
                    "--sqls",
                    str(sql_path),
                    "--csv",
                    "--print-explain",
                ]
                log_info(
                    (
                        f"Iteration {iteration + 1}/{args.iterations} seed {iter_seed} "
                        f"round {round_index + 1}/{args.rounds_per_iteration} "
                        f"position {position}/{sql_count} ({sql_path.name})"
                    )
                )
                log_command(cmd)
                code, stdout, stderr = run_command(cmd, timeout_s=args.script_timeout)

                if stderr.strip():
                    explain_blocks = parse_explains(stderr)
                    for (case_name, sql_name), lines in explain_blocks.items():
                        filename = (
                            f"{case_name}__{sql_name}"
                            f"__r{round_index + 1:02d}_p{position:02d}.txt"
                        )
                        explain_path = iter_explains_dir / filename
                        content = "\n".join(lines).rstrip()
                        write_text(explain_path, content + "\n")
                        explain_catalog.append((filename, content))

                if code != 0:
                    warn(
                        (
                            "run_benchmark.py returned "
                            f"{code} for iteration {iter_name}, round {round_index + 1}, "
                            f"position {position}, sql {sql_path.name}"
                        )
                    )
                    continue

                for row in parse_csv_text(stdout):
                    row["iteration_seed"] = str(iter_seed)
                    row["round"] = str(round_index + 1)
                    row["sql_position"] = str(position)
                    row["sql_count"] = str(sql_count)
                    row["sql_order"] = "reverse" if round_index % 2 == 1 else "forward"
                    iteration_rows.append(row)

        if not iteration_rows:
            warn(f"No benchmark rows produced for iteration {iter_name}")
            continue

        iteration_fields = [
            "category",
            "case",
            "party_count",
            "service_count",
            "variant",
            "exec_ms",
            "shared_read",
            "shared_hit",
            "shared_dirtied",
            "cache_status",
            "iteration_seed",
            "round",
            "sql_position",
            "sql_count",
            "sql_order",
        ]
        write_csv_rows(csv_path, iteration_rows, iteration_fields)
        aggregate_rows.extend(iteration_rows)

    log_info("Writing summary and concatenated explains")
    summary_path = root_dir / f"summary-{timestamp}.csv"
    write_csv_rows(summary_path, build_summary_rows(aggregate_rows), SUMMARY_FIELDS)
    for round_number in range(1, args.rounds_per_iteration + 1):
        round_path = root_dir / f"summary-round{round_number}.csv"
        round_rows = [row for row in aggregate_rows if row.get("round") == str(round_number)]
        write_csv_rows(round_path, build_summary_rows(round_rows), SUMMARY_FIELDS)

    explains_all_path = root_dir / "explains_all.txt"
    with explains_all_path.open("w", encoding="utf-8") as handle:
        for filename, content in explain_catalog:
            handle.write(f"== {filename} ==\n")
            handle.write(content)
            handle.write("\n\n")

    log_info("Condensing explains")
    condensed_path = root_dir / "explains_all.txt.condensed.txt"
    cmd = [
        sys.executable,
        str(SCRIPT_DIR / "condense_explains.py"),
        str(explains_all_path),
        "--out",
        str(condensed_path),
    ]
    log_command(cmd)
    code, stdout, stderr = run_command(cmd, timeout_s=args.script_timeout)
    if code != 0:
        print(stderr, file=sys.stderr)
        warn("condense_explains.py failed")

    log_info("Generating Excel summary")
    excel_path = root_dir / f"summary-{timestamp}.xlsx"
    cmd = [
        sys.executable,
        str(SCRIPT_DIR / "generate_excel_summary.py"),
        str(summary_path),
        "--out",
        str(excel_path),
    ]
    log_command(cmd)
    code, stdout, stderr = run_command(cmd, timeout_s=args.script_timeout)
    if code != 0:
        print(stderr, file=sys.stderr)
        warn("generate_excel_summary.py failed")

    print(str(root_dir))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
