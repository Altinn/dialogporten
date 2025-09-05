### Restoring a Database from an Azure Backup

Before proceeding, back up your current database in case you need to restore it later:
```sh
PGPASSWORD='supersecret' pg_dump -h localhost -p 5432 -U postgres -d dialogporten -F c -f local-db-backup.dump
```

If you need to verify migrations against a proper database backup (e.g., from the tt02 environment), you’ll first need to use the database forwarder.  
See [these docs](../scripts/database-forwarder/README.md) for setup instructions.

Once the forwarder is running, set the database password for the current terminal session:
```sh
export PGPASSWORD="get-password-from-key-vault"
```

Then, run the backup:
```sh
pg_dump -e pg_trgm -h localhost -p 15432 -U dialogportenPgAdmin -d dialogporten -F c -f azure-db-backup.dump
```

After this, you should have a file called `azure-db-backup.dump` in the current directory.

Now, reset the password back to the local default:
```sh
export PGPASSWORD="supersecret"
```

### Creating Users for the Restored Database

Azure databases may have different users than your local setup. To match them, create the required users:

```sh
psql -h localhost -U postgres -c 'CREATE ROLE "dialogportenPgAdmin" WITH SUPERUSER LOGIN PASSWORD '\''supersecret'\'';'
psql -h localhost -U postgres -c "CREATE ROLE azure_pg_admin WITH SUPERUSER LOGIN PASSWORD 'fakepassword';"
```

### Restoring the Backup

To restore the database from the Azure backup, run:

```sh
pg_restore -h localhost -U postgres -d dialogporten --clean azure-db-backup.dump
```

### Restoring the Local Database
If you want to restore the local database backup, run:
```sh
pg_restore -h localhost -U postgres -d dialogporten --clean local-db-backup.dump
```
