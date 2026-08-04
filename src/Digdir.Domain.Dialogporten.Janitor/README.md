# Dialogporten Janitor

A console application for container app jobs or performing various synchronizations and janitorial tasks in Dialogporten.

## Commands

Below are the available commands (commands are always the first argument):

### sync-subject-resource-mappings

- **Description:**  
  Synchronizes the mappings of subjects (i.e., roles) and resources (i.e., apps) from the Altinn Resource Registry to Dialogporten's local copy used for authorization.

- **Argument(s):**
    - `-s` *Optional*: Override the time of the last synchronization. This argument should be a `DateTimeOffset`, e.g., `2024-08-15` (default: newest in local copy)
    - `-b` *Optional*: Override the batch size (default: 1000).

### sync-resource-policy-information

- **Description:**  
  Synchronizes resource policies from the Altinn Resource Registry to Dialogporten's local copy used for authorization.

- **Argument(s):**
    - `-s` *Optional*: Override the time of the last synchronization. This argument should be a `DateTimeOffset`, e.g., `2024-08-15` (default: newest in local copy)
    - `-c` *Optional*: Number of concurrent requests to fetch policies (default: 10).

### reindex-dialogsearch

* **Description:**
  Rebuilds the full-text search index for all dialogs in Dialogporten.
  This command is typically run as a maintenance job to regenerate the `search.DialogSearch` table, either fully, incrementally (since a timestamp), only for stale/outdated dialogs, or as a resumed background job.

* **Argument(s):**

    - `-f`, `--full`  
      *Optional*: Force a full reindex. Seeds **all dialogs** into the rebuild queue and rebuilds all search vectors.  
      *Cannot be combined with `--since`, `--resume`, or `--stale-only`.*

    - `-s`, `--since`
      *Optional*: Reindex only dialogs updated since the given timestamp (`DateTimeOffset`, e.g., `2024-08-15T00:00:00Z`).  
      *Cannot be combined with `--full`, `--resume`, or `--stale-only`.*

    - `-r`, `--resume`  
      *Optional*: Resume a previously started reindexing job. Uses existing rebuild queue without reseeding.  
      *Cannot be combined with `--full`, `--since`, or `--stale-only`.*

    - `-o`, `--stale-only`  
      *Optional*: Reindex only **stale or missing dialogs** (dialogs not present in `search.DialogSearch` or where `Dialog.UpdatedAt > DialogSearch.UpdatedAt`).  
      *Cannot be combined with `--full`, `--since`, or `--resume`.*

    - `--stale-first`  
      *Optional*: Prioritize reindexing stale or outdated dialogs **first** within each batch run.  
      This does **not** affect which dialogs are seeded—only the order in which they are processed.

    - `-b`, `--batch-size`  
      *Optional*: Batch size per worker (default: `1000`).

    - `-w`, `--workers`  
      *Optional*: Number of parallel workers (default: `1`).

    - `--throttle-ms`  
      *Optional*: Delay (in milliseconds) between processing batches for each worker (default: `0`).

    - `--work-mem-bytes`  
      *Optional*: PostgreSQL `work_mem` setting per worker in bytes (default: `268435456` ≈ 256 MB).

* **Examples:**

  ```bash
  # Full rebuild of all dialogs
  janitor reindex-dialogsearch --full

  # Reindex only dialogs updated since August 1st 2024
  janitor reindex-dialogsearch --since 2024-08-01T00:00:00Z

  # Reindex only stale/missing dialogs
  janitor reindex-dialogsearch --stale-only

  # Resume a previously started rebuild (does not reseed)
  janitor reindex-dialogsearch --resume

  # Run 4 workers with throttling and increased work_mem
  janitor reindex-dialogsearch --full -w 4 --batch-size 2000 --throttle-ms 100 --work-mem-bytes 536870912

  # Reindex stale dialogs only, prioritizing oldest ones first
  janitor reindex-dialogsearch --stale-only --stale-first -w 4
  ```

---


### generate-searchterms

- **Description:**  
  Generates the curated, per-language search-term lists used for search autocomplete, and persists them to the `SearchTermList` table (one row per language). The result is served from `GET /api/v1/metadata/searchterms`.

  The command samples dialogs per service resource, extracts words that are *common to all samples* of a resource (a strict per-language intersection, which filters out dialog-specific content such as names and reference numbers), filters the survivors, collapses inflections via Postgres stemming, and stores the result as an inverted index of `word → service resources`.

  Pipeline in brief:

    1. **Sampling (Stage A)** — a single global `TABLESAMPLE SYSTEM` pass over `Dialog`, with the sample percentage derived from `--pool-rows` and the estimated row count (clamped to 0.001–5 %). Rows are bucketed per service resource and the `--sample-size` most recently content-updated dialogs are kept per bucket, biasing terms toward fresh content.
    2. **Sampling (Stage B)** — resources that the random pool under-sampled are topped up through a direct index lookup, so small resources are not omitted.
    3. **Intersection** — titles and summaries of a resource's samples are tokenized (letters only, lowercased) per language, and only words present in *every* sample survive. A sample with no content in the given language collapses that resource/language intersection to empty.
    4. **Filtering** — survivors shorter than `--min-length` or present in the bundled stopword lists (`no.txt`, `en.txt`) are dropped.
    5. **Stemming** — remaining words are stemmed in bulk with `ts_lexize` using the same dictionaries as the search side (`norwegian_stem` for `nb`/`nn`, `english_stem` for `en`), and each stem is collapsed to one canonical surface form *globally per language*, so the same stem never yields duplicate suggestions across resources.
    6. **Persistence** — the inverted index is pivoted into one JSON document per configured language and written atomically. All documents from a run share the same `GeneratedAt`, which drives the endpoint's `ETag` / `Last-Modified`.

  The command is safe to run repeatedly and replaces the previous set on success. If no service resources or no samples are found, it logs a warning and leaves the existing data untouched rather than publishing an empty set.

- **Argument(s):**

    - `-n`, `--sample-size`  
      *Optional*: Samples per service resource, 3–100 (default: `3`). Higher values make the intersection stricter and yield fewer, more generic terms.

    - `--pool-rows`  
      *Optional*: Target number of rows for the Stage A `TABLESAMPLE` pool (default: `150000`).

    - `-m`, `--min-length`  
      *Optional*: Minimum word length to keep (default: `5`).

    - `-l`, `--languages`  
      *Optional*: Comma-separated language codes to generate documents for (default: `nb,nn,en`). Languages with no surviving words get an empty document, so the endpoint serves an empty list rather than a 404.

- **Examples:**

  ```bash
  # Generate with defaults (3 samples per resource, nb/nn/en)
  janitor generate-searchterms

  # Stricter intersection from a larger pool
  janitor generate-searchterms -n 10 --pool-rows 500000

  # Bokmål only, allowing shorter words
  janitor generate-searchterms -l nb -m 4
  ```

---


### collect-custom-metrics

- **Description:**  
  Collects custom metrics from the Dialogporten database and emits them via OpenTelemetry to Azure Monitor.

- **Current Metrics:**
  - `dialogporten.outbox.queue_size`: Count of rows in the MassTransitOutboxState table.

- **Example:**

  ```bash
  janitor collect-custom-metrics
  ```
