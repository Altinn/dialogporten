-- Column statistics targets and extended statistics that the prod search plans depend on.
--
-- Each object is only changed when it differs, so this is a no-op (and takes no lock) where it is
-- already in place. Both ALTER TABLE ... SET STATISTICS and CREATE STATISTICS take
-- SHARE UPDATE EXCLUSIVE, which would otherwise cancel a running autovacuum on the table.
-- New targets take effect at the next (auto)analyze.
DO
$$
    DECLARE
        r record;
    BEGIN
        FOR r IN
            SELECT t.relation, t.column_name, t.target
            FROM (VALUES ('public."Dialog"', 'Party', 2000),
                         ('public."Dialog"', 'ServiceResource', 1000),
                         ('public."DialogEndUserContextSystemLabel"', 'DialogEndUserContextId', 1000),
                         ('public."DialogGuiAction"', 'DialogId', 1000),
                         ('public."LocalizationSet"', 'DialogGuiActionPrompt_GuiActionId', 1000)) AS t(relation, column_name, target)
            LOOP
                IF NOT EXISTS (SELECT 1
                               FROM pg_attribute a
                               WHERE a.attrelid = r.relation::regclass
                                 AND a.attname = r.column_name
                                 AND a.attstattarget = r.target) THEN
                    EXECUTE format('ALTER TABLE %s ALTER COLUMN %I SET STATISTICS %s', r.relation, r.column_name, r.target);
                END IF;
            END LOOP;

        IF NOT EXISTS (SELECT 1
                       FROM pg_statistic_ext s
                       WHERE s.stxnamespace = 'public'::regnamespace
                         AND s.stxname = 'STATS_Dialog_Party_ServiceResource_MCV') THEN
            CREATE STATISTICS public."STATS_Dialog_Party_ServiceResource_MCV" (mcv)
                ON "ServiceResource", "Party" FROM public."Dialog";
        END IF;
    END
$$;
