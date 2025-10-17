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


