### Large Data Set Generator

This project generates large data sets for `yt01`, our performance environment.


#### Long-running jobs in yt01
Long-running jobs in yt01 should be done from the [ssh jumper](../../scripts/database-forwarder/README.md) in Azure.

1. Suspend the CI/CD pipeline for YT01 in GitHub Actions GUI
   "Actions" => "CI/CD YT01" => "Disable workflow", menu top right
2. Turn off all container apps and jobs, WebAPIs, GraphQL, Service, Sync jobs
3. Find the VM in the resource group => Operations => Updates => Update Settings =>  
   Select "Disable" on periodic assessment, and "ImageDefault" on patch orchestration, click Save
4. Connect to the jumper
5. Clone the repository if you haven't already
6. Run using the `runner.sh` script. 

#### Date ranges and number of loops:  
When changing start/end dates, only change the year. (The year should be the same for both start and end dates.)

Each execution runs 50 loops with 2 million dialogs in each loop. This leaves us with 100 million dialogs per run, and with a gap without data from March to December in the last year in a 5-year period.  

If you start in 1970, you will have no dialogs in the period March to December in 1974.
If you then were to add another set of data, start in 1975, and you would have no dialogs in the period March to December in 1979.

This leaves us with gaps in the timeline every 5 years to play with custom/manual inserts and updates.


The data sets are created as CSV strings and written directly to the database using the following command:
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
    - Take 1 year as a parameter, use it for both start and end date.

- **Constraint and Index Management**
    - Add a script to remove all constraints and indexes using the SQL commands found in `Sql.cs`.
    - Optimize the parallel restoration of constraints and indexes to reduce processing time.

- **Fine-Tuning Parallelism**
    - Investigate increasing the number of threads for Actors, LocalizationSets, and Localizations.
