#!/bin/bash

DB_HOST="localhost" # DO NOT COMMIT
DB_PORT="5432"
DB_NAME="dialogporten"
DB_USER="postgres"
DB_PASSWORD="supersecret" # DO NOT COMMIT

SQL_COMMAND=$(cat <<EOF
SET maintenance_work_mem = '1GB';

DO
\$\$
DECLARE
    x RECORD;
    loop_counter INTEGER := 0; -- Initialize loop counter
    total_loops INTEGER;       -- Variable to store total number of loops
    priority_values INTEGER[] := ARRAY[1, 2, 3, 4]; -- Array of priorities
BEGIN
    -- Calculate the total number of loops (rows in the SELECT statement)
    SELECT COUNT(*) INTO total_loops
    FROM constraint_index_backup
    WHERE priority = ANY(priority_values);

    -- Log the total number of loops before starting
    RAISE NOTICE '============================================================';
    RAISE NOTICE 'Starting loop execution. Total number of loops: %', total_loops;
    RAISE NOTICE 'Priority values being processed: %', priority_values;
    RAISE NOTICE '============================================================';

    -- Iterate over the rows
    FOR x IN
        SELECT create_script
        FROM constraint_index_backup
        WHERE priority = ANY(priority_values)
        ORDER BY priority
    LOOP
        -- Increment the loop counter
        loop_counter := loop_counter + 1;

        -- Log a splitter before each iteration
        RAISE NOTICE '------------------------------------------------------------';

        -- Log each iteration with the updated timestamp, loop count, and total count
        RAISE NOTICE 'Loop % of %', loop_counter, total_loops;
        RAISE NOTICE 'Timestamp: %', clock_timestamp();
        RAISE NOTICE 'Executing script: %', x.create_script;

        -- Execute the script
        EXECUTE x.create_script;

        -- Log a splitter after each iteration
        RAISE NOTICE '------------------------------------------------------------';
    END LOOP;

    -- Final log message
    RAISE NOTICE '============================================================';
    RAISE NOTICE 'Completed all % loops.', total_loops;
    RAISE NOTICE '============================================================';

    RAISE NOTICE '============================================================';
    RAISE NOTICE 'Starting ANALYZE on all tables to update statistics...';
    RAISE NOTICE 'Timestamp: %', clock_timestamp();
    RAISE NOTICE '============================================================';
    
    ANALYZE;
    
    RAISE NOTICE '============================================================';
    RAISE NOTICE 'ANALYZE completed.';
    RAISE NOTICE 'Timestamp: %', clock_timestamp();
    RAISE NOTICE '============================================================';
END;
\$\$;

EOF
)

START_TIME=$(date +%s)

export PGPASSWORD="$DB_PASSWORD"

psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "$SQL_COMMAND"

END_TIME=$(date +%s)

TOTAL_TIME=$((END_TIME - START_TIME))
echo "SQL script executed in $TOTAL_TIME seconds 🚀"
