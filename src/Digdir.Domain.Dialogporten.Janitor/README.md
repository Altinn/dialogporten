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
