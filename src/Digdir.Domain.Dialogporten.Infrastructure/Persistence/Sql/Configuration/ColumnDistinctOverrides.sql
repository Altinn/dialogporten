-- Pins the number of distinct values (n_distinct) for foreign key columns whose sampled estimate is
-- off by orders of magnitude.
--
-- Why: ANALYZE samples about 30,000 rows in random blocks and estimates distinct values from how
-- often values repeat in the sample (the Haas-Stokes Duj1 estimator). Child rows in the dialog tree
-- are inserted together with their parent and land on the same pages, so a block sample sees the
-- same parent over and over and concludes there are few distinct values. On tables with billions of
-- rows this makes the planner expect hundreds or thousands of rows per key, where the real number is
-- one to a few.
--
-- Join sizes are still estimated correctly (PostgreSQL uses the larger side's distinct count, and
-- foreign keys), but the cost of each index lookup in a nested loop comes from the looked-up
-- column's own n_distinct. With the inflated estimates, scanning a whole table looks cheaper than
-- the lookups, as in issue #4467, where GetDialog chose a sequential scan of Localization (14.1
-- billion rows) for a dialog with many transmissions.
--
-- The values below are not measured. Each follows from the data model, stated per column. A negative
-- n_distinct is a fraction of the table's rows (NULLs included), so it keeps scaling as the table
-- grows: rows per key = (1 - null_frac) / |n_distinct|. Precision matters little; what decides plans
-- is whether a lookup costs about 2 rows or thousands. Where unsure, values err towards slightly more
-- rows per key, which is the safer direction.
--
-- Estimated rows per key in prod before these overrides (2026-10-08) are given per column.
--
-- Each column is only altered (and re-analyzed, which applies the value) when its attoptions do not
-- already hold the value, so this is a no-op where it was set by hand. ALTER TABLE ... ALTER COLUMN
-- ... SET takes SHARE UPDATE EXCLUSIVE, which cancels a running autovacuum on the table.
-- To undo one: ALTER TABLE ... ALTER COLUMN ... RESET (n_distinct); ANALYZE <table> (<column>);
DO
$$
    DECLARE
        r record;
    BEGIN
        FOR r IN
            SELECT t.relation, t.column_name, t.n_distinct
            FROM (VALUES
                      -- A localization set holds one text per language, 1-3 in practice. Prod: 14.1B
                      -- rows over 7.8B sets (1.8 per set); estimated 9,092. -0.5 = 2 per set.
                      ('public."Localization"', 'LocalizationSetId', '-0.5'),

                      -- A transmission has a title plus an optional summary and content reference,
                      -- so 1-3 content rows.
                      -- Prod: 406M rows over 260M transmissions (1.6); estimated 163. -0.6 = 1.7.
                      ('public."DialogTransmissionContent"', 'TransmissionId', '-0.6'),

                      -- A dialog has a title plus a few optional content types (summary, sender
                      -- name, extended status, ...). Prod: 2.7B rows over 989M dialogs (2.7);
                      -- estimated 2,113. -0.35 = 2.9 per dialog.
                      ('public."DialogContent"', 'DialogId', '-0.35'),

                      -- Activities per dialog are few for nearly all dialogs; the few dialogs with
                      -- very many are covered by the most-common-values list. Prod: 2.3B rows over
                      -- 989M dialogs (2.3); estimated 1,798. -0.4 = 2.5 per dialog.
                      ('public."DialogActivity"', 'DialogId', '-0.4'),

                      -- API actions per dialog are few. Prod: 2.3B rows over 989M dialogs (2.3);
                      -- estimated 1,495. -0.4 = 2.5 per dialog.
                      ('public."DialogApiAction"', 'DialogId', '-0.4'),

                      -- GUI actions per dialog are one or two. Prod: 1.26B rows over 989M dialogs
                      -- (1.3); estimated 52. -0.7 = 1.4 per dialog.
                      ('public."DialogGuiAction"', 'DialogId', '-0.7'),

                      -- Search tags per dialog are few, and not every dialog has them, so the real
                      -- figure per tagged dialog is unknown but small. Prod: 630M rows, fewer than
                      -- dialogs; estimated 519. -0.5 = 2 per dialog.
                      ('public."DialogSearchTag"', 'DialogId', '-0.5'),

                      -- A label assignment log holds the few label changes on one end-user context.
                      -- Prod: 607M rows, fewer than contexts; estimated 178. -0.5 = 2 per context.
                      ('public."LabelAssignmentLog"', 'ContextId', '-0.5'),

                      -- Attachments directly on a dialog (60% of rows are on transmissions, so NULL
                      -- here) are few per dialog. Prod: 976M non-null rows, fewer than dialogs;
                      -- estimated 3,460. -0.2 = 2 per dialog (null_frac 0.6).
                      ('public."Attachment"', 'DialogId', '-0.2'),

                      -- Transmission attachments: prod has 1.48B non-null rows over at most 260M
                      -- transmissions, so at least 5.7 per transmission that has any; estimated
                      -- 8,807. -0.1 = 6 per transmission (null_frac 0.4).
                      ('public."Attachment"', 'TransmissionId', '-0.1')
                 ) AS t(relation, column_name, n_distinct)
            LOOP
                IF NOT EXISTS (SELECT 1
                               FROM pg_attribute a
                               WHERE a.attrelid = r.relation::regclass
                                 AND a.attname = r.column_name
                                 AND a.attoptions @> ARRAY ['n_distinct=' || r.n_distinct]) THEN
                    EXECUTE format('ALTER TABLE %s ALTER COLUMN %I SET (n_distinct = %s)',
                                   r.relation, r.column_name, r.n_distinct);
                    EXECUTE format('ANALYZE %s (%I)', r.relation, r.column_name);
                END IF;
            END LOOP;
    END
$$;
