CREATE OR REPLACE FUNCTION partyresource.backfill_dialog_partyresource_batch(p_batch_size integer DEFAULT 50000)
RETURNS TABLE
(
    "ProcessedRows" integer,
    "InsertedParties" integer,
    "InsertedResources" integer,
    "InsertedPairs" integer,
    "Completed" boolean,
    "AllCompleted" boolean,
    "LastDialogId" uuid,
    "Phase" text,
    "FillStageMs" integer,
    "SelectBatchMs" integer,
    "ParseMs" integer,
    "InsertPartyMs" integer,
    "InsertResourceMs" integer,
    "InsertPairMs" integer,
    "UpdateStateMs" integer,
    "TotalMs" integer
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_started_at timestamp with time zone := clock_timestamp();
    v_stage_started_at timestamp with time zone;

    v_last_dialog_id uuid;
    v_next_last_dialog_id uuid;
    v_completed boolean;
    v_state_ctid tid;

    v_processed_rows integer := 0;
    v_inserted_parties integer := 0;
    v_inserted_resources integer := 0;
    v_inserted_pairs integer := 0;

    v_fill_stage_ms integer := 0;
    v_select_batch_ms integer := 0;
    v_parse_ms integer := 0;
    v_insert_party_ms integer := 0;
    v_insert_resource_ms integer := 0;
    v_insert_pair_ms integer := 0;
    v_update_state_ms integer := 0;
    v_total_ms integer := 0;

    v_phase text := 'Keyset';
BEGIN
    IF p_batch_size <= 0 THEN
        RAISE EXCEPTION 'p_batch_size must be > 0'
            USING ERRCODE = '22023';
    END IF;

    INSERT INTO partyresource."BackfillState" ("LastDialogId", "Completed", "UpdatedAt")
    SELECT
        NULL,
        false,
        now()
    WHERE NOT EXISTS (
        SELECT 1
        FROM partyresource."BackfillState"
    );

    SELECT s."LastDialogId", s."Completed", s.ctid
    INTO v_last_dialog_id, v_completed, v_state_ctid
    FROM partyresource."BackfillState" s
    FOR UPDATE
    LIMIT 1;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Backfill state row missing in partyresource."BackfillState".'
            USING ERRCODE = '55000';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM partyresource."BackfillState"
        OFFSET 1
    ) THEN
        RAISE EXCEPTION 'BackfillState contains more than one row; expected exactly one row.'
            USING ERRCODE = '55000';
    END IF;

    IF v_completed THEN
        v_total_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_started_at)) * 1000)::integer);

        RETURN QUERY
        SELECT
            0,
            0,
            0,
            0,
            true,
            true,
            v_last_dialog_id,
            'Completed'::text,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            v_total_ms;
        RETURN;
    END IF;

    IF to_regclass('pg_temp.pr_backfill_raw_batch') IS NULL THEN
        EXECUTE '
            CREATE TEMP TABLE pg_temp.pr_backfill_raw_batch
            (
                "Id" uuid NOT NULL,
                "Party" text NOT NULL,
                "ServiceResource" text NOT NULL
            )
            ON COMMIT PRESERVE ROWS
        ';
    END IF;

    IF to_regclass('pg_temp.pr_backfill_parsed_batch') IS NULL THEN
        EXECUTE '
            CREATE TEMP TABLE pg_temp.pr_backfill_parsed_batch
            (
                "Id" uuid NOT NULL,
                "ShortPrefix" char(1) NOT NULL,
                "UnprefixedPartyIdentifier" text NOT NULL,
                "UnprefixedResourceIdentifier" text NOT NULL
            )
            ON COMMIT PRESERVE ROWS
        ';
    END IF;

    TRUNCATE pg_temp.pr_backfill_raw_batch;
    TRUNCATE pg_temp.pr_backfill_parsed_batch;

    v_stage_started_at := clock_timestamp();
    INSERT INTO pg_temp.pr_backfill_raw_batch
    (
        "Id",
        "Party",
        "ServiceResource"
    )
    SELECT
        d."Id",
        d."Party",
        d."ServiceResource"
    FROM public."Dialog" d
    WHERE d."Id" > COALESCE(v_last_dialog_id, '00000000-0000-0000-0000-000000000000'::uuid)
    ORDER BY d."Id"
    LIMIT p_batch_size;
    v_select_batch_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_stage_started_at)) * 1000)::integer);
    GET DIAGNOSTICS v_processed_rows = ROW_COUNT;

    v_next_last_dialog_id := v_last_dialog_id;

    IF v_processed_rows > 0 THEN
        SELECT b."Id"
        INTO v_next_last_dialog_id
        FROM pg_temp.pr_backfill_raw_batch b
        ORDER BY b."Id" DESC
        LIMIT 1;

        v_stage_started_at := clock_timestamp();
        INSERT INTO pg_temp.pr_backfill_parsed_batch
        (
            "Id",
            "ShortPrefix",
            "UnprefixedPartyIdentifier",
            "UnprefixedResourceIdentifier"
        )
        SELECT
            b."Id",
            parsed_party."ShortPrefix",
            parsed_party."UnprefixedPartyIdentifier",
            partyresource.resource_from_urn(b."ServiceResource")
        FROM pg_temp.pr_backfill_raw_batch b
        CROSS JOIN LATERAL partyresource.party_parse_urn(b."Party") parsed_party;
        v_parse_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_stage_started_at)) * 1000)::integer);

        v_stage_started_at := clock_timestamp();
        INSERT INTO partyresource."Party" ("ShortPrefix", "UnprefixedPartyIdentifier")
        SELECT DISTINCT b."ShortPrefix", b."UnprefixedPartyIdentifier"
        FROM pg_temp.pr_backfill_parsed_batch b
        ON CONFLICT ("ShortPrefix", "UnprefixedPartyIdentifier") DO NOTHING;
        v_insert_party_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_stage_started_at)) * 1000)::integer);
        GET DIAGNOSTICS v_inserted_parties = ROW_COUNT;

        v_stage_started_at := clock_timestamp();
        INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
        SELECT DISTINCT b."UnprefixedResourceIdentifier"
        FROM pg_temp.pr_backfill_parsed_batch b
        ON CONFLICT ("UnprefixedResourceIdentifier") DO NOTHING;
        v_insert_resource_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_stage_started_at)) * 1000)::integer);
        GET DIAGNOSTICS v_inserted_resources = ROW_COUNT;

        v_stage_started_at := clock_timestamp();
        INSERT INTO partyresource."PartyResource" ("PartyId", "ResourceId")
        SELECT DISTINCT p."Id", r."Id"
        FROM pg_temp.pr_backfill_parsed_batch b
        INNER JOIN partyresource."Party" p
            ON p."ShortPrefix" = b."ShortPrefix"
           AND p."UnprefixedPartyIdentifier" = b."UnprefixedPartyIdentifier"
        INNER JOIN partyresource."Resource" r
            ON r."UnprefixedResourceIdentifier" = b."UnprefixedResourceIdentifier"
        ON CONFLICT ("PartyId", "ResourceId") DO NOTHING;
        v_insert_pair_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_stage_started_at)) * 1000)::integer);
        GET DIAGNOSTICS v_inserted_pairs = ROW_COUNT;

        v_completed := false;
        v_phase := 'Keyset';
    ELSE
        v_completed := true;
        v_phase := 'Completed';
    END IF;

    v_stage_started_at := clock_timestamp();
    UPDATE partyresource."BackfillState" s
    SET
        "LastDialogId" = v_next_last_dialog_id,
        "Completed" = v_completed,
        "UpdatedAt" = now()
    WHERE s.ctid = v_state_ctid;
    v_update_state_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_stage_started_at)) * 1000)::integer);

    v_total_ms := GREATEST(0, (EXTRACT(EPOCH FROM (clock_timestamp() - v_started_at)) * 1000)::integer);

    RETURN QUERY
    SELECT
        v_processed_rows,
        v_inserted_parties,
        v_inserted_resources,
        v_inserted_pairs,
        v_completed,
        v_completed,
        v_next_last_dialog_id,
        v_phase,
        v_fill_stage_ms,
        v_select_batch_ms,
        v_parse_ms,
        v_insert_party_ms,
        v_insert_resource_ms,
        v_insert_pair_ms,
        v_update_state_ms,
        v_total_ms;
END;
$$;
