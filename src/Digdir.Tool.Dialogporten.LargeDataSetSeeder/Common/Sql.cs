namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class Sql
{
    // language=psql
    internal const string DisableAllIndexesConstraints =
        """
        BEGIN;

        -- drop indexes/constraints
        DROP TABLE IF EXISTS constraint_index_backup;
        CREATE TABLE IF NOT EXISTS constraint_index_backup
        AS
        SELECT 'index' type
             ,4 priority
             ,format('DROP INDEX IF EXISTS %I.%I', schemaname, indexname) drop_script
             ,indexdef create_script
        FROM pg_indexes
        LEFT JOIN pg_constraint
            ON pg_indexes.indexname = pg_constraint.conname
        WHERE schemaname = 'public'
            AND pg_constraint.oid IS NULL
        UNION SELECT 'constraint' type
            ,CASE contype
                WHEN 'p' THEN 1
                WHEN 'u' THEN 2
                ELSE 3
            END priority
                ,format('ALTER TABLE %s DROP CONSTRAINT IF EXISTS %I CASCADE', conrelid::regclass, conname) drop_script
                ,format('ALTER TABLE %s ADD CONSTRAINT %I %s', conrelid::regclass, conname, pg_get_constraintdef(c.oid)) create_script
        FROM pg_constraint c
        JOIN pg_namespace n
            ON n.oid = c.connamespace
        WHERE n.nspname = 'public';
        --     AND contype != 'p';

        -- Drop constraints and indexes
        DO
        $$
        DECLARE x RECORD;
        BEGIN
        FOR x IN
        SELECT drop_script
        FROM constraint_index_backup
        ORDER BY priority DESC
            LOOP
                EXECUTE x.drop_script;
            END LOOP;
        END;
        $$;

        COMMIT;
        """;

    // language=psql
    internal const string EnableAllIndexesConstraints =
        """
        SET maintenance_work_mem = '1GB';
        
        DO
        $$
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
        $$;
        """;
}
