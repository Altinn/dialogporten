### Large Data Set Generator

This project generates large data sets for `yt01`, our performance environment. The data sets are created as CSV strings and written directly to the database using the following command:

```
COPY "X" ("Y", "X") FROM STDIN (FORMAT csv, HEADER false)
```

* **Independent and Parallel Table Generation**
    - Each table is generated independently and in parallel.
    - Correct primary and foreign keys are ensured via `DeterministicUuidV7.cs`.
        - Each dialog has a unique timestamp, and IDs are deterministically generated using the timestamp, table name, and an optional tie-breaker.


* **Configurable Parallelism**
    - The number of threads per table can be configured using the `numberOfTasks` parameter in the `CreateCopyTask` method.


* **Service Resource Assignment**
    - Service resources are selected in a round-robin manner from the `service_resources` file.


* **Party Selection**
    - The `parties` file, which is `.gitignore`-protected due to its potential size, must be created before running the project.
    - Each line in the file should follow one of these formats:

      ```
      urn:altinn:organization:identifier-no:589773608
      urn:altinn:person:identifier-no:19220092065
      ```
    - The `Party` property is deterministically set based on a hash of the dialog ID. This `Party` is also used in the SeenLog table, which contains one entry for each dialog.


* **Localization and Search Tags**
    - Optionally, two wordlist files (`wordlist_en` and `wordlist_nb`) can be created for random localizations and search tags.
    - These words are deterministically selected based on the dialog/localization ID.
    - Each file should contain one word per line.
    - These files are also added to `.gitignore`.

#### TODO

- **Parameterize Shell Scripts**
    - Minimize the risk of committing secrets to the repository.


- **Constraint and Index Management**
    - Add a script to remove all constraints and indexes using the SQL commands found in `Sql.cs`.
    - Optimize the parallel restoration of constraints and indexes to reduce processing time.


- **Fine-Tuning Parallelism**
    - Investigate increasing the number of threads for Actors, LocalizationSets, and Localizations.
