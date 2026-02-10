# Dialogsearch Benchmark Scripts

This folder contains a small toolkit for generating test samples, producing case sets, running SQL variants, and aggregating results across multiple iterations. The main entrypoint is `run_iterated_benchmark.py`, which orchestrates the others.

`run_iterated_benchmark.py` now defaults to a fairness-oriented execution model:
- Each SQL file is run separately on the same caseset.
- SQL execution order is rotated per round/iteration and alternates forward/reverse.
- This reduces systematic "later SQL gets warmer cache" bias.

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
  --generate-party-pool-with-count 50000 \
  --generate-service-pool-with-count 5000 \
  --generate-set "1,1,1; 1,3000,1; 5,3000,2; 100,3000,20; 200,3000,40; 1000,1,1; 2000,1,1; 10000,1,1" \
  --sqls "sql/*.sql" \
  --iterations 10 \
  --seed 1337 \
  --rounds-per-iteration 2
```

This creates a new output directory named `benchmark-YYYYMMDD-HHMM` in the current working directory (unless you override it with `--out-dir`).

## Output Layout (run_iterated_benchmark)

```text
benchmark-YYYYMMDD-HHMM/
  casesets/
    2000/
      001-1p-1s-1g.json
      002-1p-3000s-1g.json
      ...
    2001/
      001-1p-1s-1g.json
      ...
  sqls/
    party.sql
    service.sql
    ...
  output/
    parties.txt
    services.txt
    csvs/
      2000.csv
      2001.csv
    explains/
      2000/
        <case>__<sql>__rXX_pYY.txt
      2001/
        <case>__<sql>__rXX_pYY.txt
  summary-YYYYMMDDHHMM.csv
  summary-round1.csv
  summary-round2.csv
  summary-roundN.csv
  summary-YYYYMMDDHHMM.xlsx
  explains_all.txt
  explains_all.txt.condensed.txt
```

Notes:
- Each iteration is seeded from `--seed` + iteration index, and the directory name is the seed (zero‑padded).
- Case filenames omit the seed (stable names across iterations) so aggregation groups cleanly.
- Each `output/csvs/<seed>.csv` file contains combined rows from all rounds and SQL positions for that seed.
- `summary-YYYYMMDDHHMM.csv` is aggregated per `(sql, case)` across all iterations/rounds, with completion rate and exec/read/hit stats.
- `summary-roundN.csv` files are generated for each fairness round (`N = 1..--rounds-per-iteration`).
- `summary-YYYYMMDDHHMM.xlsx` contains:
  - `Summary`: total aggregate plus one table/chart per round.
  - `Details - total` and `Details - rN`: per-case rows from total and each round summary.
- `explains_all.txt.condensed.txt` is a compressed version of `explains_all.txt`.

## Script Reference

### `run_iterated_benchmark.py`
Runs full benchmark iterations end‑to‑end.

Key options:
- `--generate-party-pool-with-count`: generate party pool with the given size.
- `--with-party-pool-file`: use an existing party pool file.
- `--generate-service-pool-with-count`: generate service pool with the given size.
- `--with-service-pool-file`: use an existing service pool file.
- `--generate-set`: semicolon‑separated list of `parties,services,groups`.
- `--sqls`: comma-separated quoted glob(s) for SQL files.
- `--iterations`: number of iterations.
- `--seed`: base seed.
- `--rounds-per-iteration`: fairness rounds per iteration (default `2`).
- `--out-dir`: override output directory (optional).
- `--padding`: zero‑padding width for iteration dirs (default 3).

Behavior:
1. Generates `parties.txt` and `services.txt` once (via `generate_samples.py`).
2. For each iteration:
   - Generates JSON cases (via `generate_cases.py`).
   - Runs each SQL file separately via `run_benchmark.py` with `--csv` and `--print-explain`.
   - Rotates SQL order per round/iteration, alternating forward/reverse order.
   - Stores one combined CSV per seed and per-run explain outputs.
3. Aggregates all runs into:
   - `summary-YYYYMMDDHHMM.csv` (total),
   - `summary-roundN.csv` for all rounds,
   and builds `summary-YYYYMMDDHHMM.xlsx` from them.
4. Concatenates all explains into `explains_all.txt`.

### `run_benchmark.py`
Runs a set of SQL files against a set of JSON cases.

Key options:
- `--cases`: comma-separated quoted glob(s) for JSON cases.
- `--sqls`: comma-separated quoted glob(s) for SQL files.
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
- To use a remote environment (i.e. an SSH jumper) where there is no immediately available way to upload/download files, you can:
  1. Create a tarball with `tar cfz benchmark.tgz dialogsearch-benchmark/`
  2. Upload the file to a Filebin, i.e. `curl -sS -X POST --data-binary @benchmark.tgz -H "Content-Type: application/octet-stream" https://filebin.net/<bin>/benchmark.tgz`
  3. Unpack the tarball with `tar zxvf benchmark.tgz`
  4. Run the scripts within `dialogsearch-benchmark/`
- The same procedure can be used to download an output directory for local analysis.
