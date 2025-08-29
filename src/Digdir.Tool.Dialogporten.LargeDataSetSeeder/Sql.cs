// Keeping this SQL for future improvement, disables all indexes and constraints in the database
// Could be moved to runner.sh, or another pre-script

// namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;
//
// internal static class Sql
// {
//     internal static string DisableAllIndexesConstraints
//         => """
//         BEGIN;
//
//         -- drop indexes/constraints
//         DROP TABLE IF EXISTS constraint_index_backup;
//         CREATE TABLE IF NOT EXISTS constraint_index_backup
//         AS
//         SELECT 'index' type
//              ,4 priority
//              ,format('DROP INDEX IF EXISTS %I.%I', schemaname, indexname) drop_script
//              ,indexdef create_script
//         FROM pg_indexes
//         LEFT JOIN pg_constraint
//             ON pg_indexes.indexname = pg_constraint.conname
//         WHERE schemaname = 'public'
//             AND pg_constraint.oid IS NULL
//         UNION SELECT 'constraint' type
//             ,CASE contype
//                 WHEN 'p' THEN 1
//                 WHEN 'u' THEN 2
//                 ELSE 3
//             END priority
//                 ,format('ALTER TABLE %s DROP CONSTRAINT IF EXISTS %I CASCADE', conrelid::regclass, conname) drop_script
//                 ,format('ALTER TABLE %s ADD CONSTRAINT %I %s', conrelid::regclass, conname, pg_get_constraintdef(c.oid)) create_script
//         FROM pg_constraint c
//         JOIN pg_namespace n
//             ON n.oid = c.connamespace
//         WHERE n.nspname = 'public';
//         --     AND contype != 'p';
//
//         -- Drop constraints and indexes
//         DO
//         $$
//         DECLARE x RECORD;
//         BEGIN
//         FOR x IN
//         SELECT drop_script
//         FROM constraint_index_backup
//         ORDER BY priority DESC
//             LOOP
//                 EXECUTE x.drop_script;
//             END LOOP;
//         END;
//         $$;
//
//         COMMIT;
//         """;
// }
