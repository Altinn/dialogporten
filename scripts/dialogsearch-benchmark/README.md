# Dialogsearch Benchmark Scripts

This folder contains a small toolkit for generating test samples, producing case sets, running SQL variants, and aggregating results across multiple iterations. The main entrypoint is `run_iterated_benchmark.py`, which orchestrates the others.

## Prerequisites

- Python 3.9+
- `psql` in `PATH`
- Environment variable `PG_CONNECTION_STRING` pointing to your Postgres instance, having the form `postgresql://postgres@localhost:5432/dialogporten`. This will utilize `.pgpass`.
- Python package: `openpyxl` (for Excel output)

Install:

```bash
pip install openpyxl
```

## Quick Start (Iterated Benchmark)

```bash
./run_iterated_benchmark.py \
  --party-pool 50000 \
  --service-pool 5000 \
  --generate-set "1,1,1; 1,3000,1; 5,3000,2; 100,3000,20; 200,3000,40; 1000,1,1; 2000,1,1; 10000,1,1" \
  --sqls "sql/*.sql" \
  --iterations 10 \
  --seed 1337
```

This creates a new output directory named `benchmark-YYYYMMDD-HHMM` in the current working directory (unless you override it with `--out-dir`).

## Output Layout (run_iterated_benchmark)

```
benchmark-YYYYMMDD-HHMM/
  casesets/
    2000/
      001-1p-1s-1g.json
      002-1p-3000s-1g.json
      ...
    2001/
      001-1p-1s-1g.json
      ...
  output/
    parties.txt
    services.txt
    csvs/
      2000.csv
      2001.csv
    explains/
      2000/
        <case>__<sql>.txt
      2001/
        <case>__<sql>.txt
  summary.csv
  summary.xlsx
  explains_all.txt
```

Notes:
- Each iteration is seeded from `--seed` + iteration index, and the directory name is the seed (zero‑padded).
- Case filenames omit the seed (stable names across iterations) so aggregation groups cleanly.
- `summary.csv` is aggregated per `(sql, case)` across all iterations, with exec/read/hit stats.
- `summary.xlsx` contains a Summary sheet (aggregated by sql) and a Details sheet (per case).

## Script Reference

### `run_iterated_benchmark.py`
Runs full benchmark iterations end‑to‑end.

Key options:
- `--party-pool` / `--service-pool`: pool sizes for sampling.
- `--generate-set`: semicolon‑separated list of `parties,services,groups`.
- `--sqls`: quoted glob(s) for SQL files.
- `--iterations`: number of iterations.
- `--seed`: base seed.
- `--out-dir`: override output directory (optional).
- `--padding`: zero‑padding width for iteration dirs (default 3).

Behavior:
1. Generates `parties.txt` and `services.txt` once (via `generate_samples.py`).
2. For each iteration:
   - Generates JSON cases (via `generate_cases.py`).
   - Runs `run_benchmark.py` with `--csv` and `--print-explain`.
   - Stores CSVs and per‑case explain outputs.
3. Aggregates all runs into `summary.csv` and builds `summary.xlsx`.
4. Concatenates all explains into `explains_all.txt`.

### `run_benchmark.py`
Runs a set of SQL files against a set of JSON cases.

Key options:
- `--cases`: quoted glob(s) for JSON cases.
- `--sqls`: quoted glob(s) for SQL files.
- `--csv`: emit CSV instead of Markdown.
- `--print-explain`: prints the full EXPLAIN output for each run to stderr (cleaned of `QUERY PLAN` and separators).
- `--timeout`: per‑run timeout.

It extracts:
- `exec_ms`
- `shared_read`, `shared_hit`, `shared_dirtied`
- `cache_status` (io/cached/none/?)

### `generate_cases.py`
Creates JSON case files for parties/services/groups.

Modes:
- `--generate-default-set`
- `--generate-set "p,s,g;..."`
- `--parties/--services/--groups` for single case

Useful flags:
- `--omit-seed-in-filename` to produce stable names across iterations.

### `generate_samples.py`
Samples distinct `Party` and `ServiceResource` values from the `Dialog` table using `TABLESAMPLE`. Output is plain text; one value per line.

Usage:
```bash
./generate_samples.py party 50000
./generate_samples.py service 5000
```

### `generate_excel_summary.py`
Builds `summary.xlsx` from `summary.csv`.

- Summary sheet: aggregated by SQL file
- Details sheet: per‑case rows from `summary.csv`
- Conditional formatting on response times
- Horizontal bar chart for p50/p95/p99

### `condense_explains.py`
Condenses `explains_all.txt` for LLM‑friendly processing.

- Drops costs/widths/memory/cache stats.
- Keeps plan nodes, conditions, buffer stats, and timing.
- Replaces common EXPLAIN terms with single‑character tokens.
- Replaces identifiers (tables/indexes/quoted columns) with two‑char codes.

Usage:
```bash
./condense_explains.py benchmark/explains_all.txt
```

## Tips

- Ensure `PG_CONNECTION_STRING` is set before running anything.
- Use `--out-dir` to keep outputs separate when testing multiple runs.


