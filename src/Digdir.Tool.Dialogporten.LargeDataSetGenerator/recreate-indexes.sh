#!/bin/bash

# Database credentials
DB_HOST="" # DO NOT COMMIT
DB_PORT="5432"
DB_NAME="dialogporten"
DB_USER=""
DB_PASSWORD="" # DO NOT COMMIT

# SQL command
SQL_COMMAND=$(cat <<EOF
BEGIN;

-- Restore constraints and indexes
DO
\$\$
    DECLARE
        x RECORD;
    BEGIN
        FOR x IN
            SELECT create_script
            FROM constraint_index_backup
            ORDER BY priority
            LOOP
                EXECUTE x.create_script;
            END LOOP;
    END;
\$\$;

COMMIT;
EOF
)

# Start timer
START_TIME=$(date +%s)

# Export the password for the psql session
export PGPASSWORD="$DB_PASSWORD"

# Execute the SQL command
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "$SQL_COMMAND"

# End timer
END_TIME=$(date +%s)

# Calculate and print total execution time
TOTAL_TIME=$((END_TIME - START_TIME))
echo "SQL script executed in $TOTAL_TIME seconds 🚀"
