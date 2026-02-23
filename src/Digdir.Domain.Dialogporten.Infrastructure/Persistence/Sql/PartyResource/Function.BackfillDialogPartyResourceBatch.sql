CREATE OR REPLACE FUNCTION partyresource.backfill_dialog_partyresource_batch(p_batch_size integer DEFAULT 50000)
RETURNS TABLE
(
    "ShardId" integer,
    "ProcessedRows" integer,
    "InsertedParties" integer,
    "InsertedResources" integer,
    "InsertedPairs" integer,
    "ShardCompleted" boolean,
    "AllShardsCompleted" boolean,
    "LastDialogId" uuid
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_shard_id integer;
    v_shard_count integer;
    v_last_dialog_id uuid;
    v_processed_rows integer := 0;
    v_inserted_parties integer := 0;
    v_inserted_resources integer := 0;
    v_inserted_pairs integer := 0;
    v_shard_completed boolean := false;
    v_all_shards_completed boolean := false;
BEGIN
    IF p_batch_size <= 0 THEN
        RAISE EXCEPTION 'p_batch_size must be > 0'
            USING ERRCODE = '22023';
    END IF;

    SELECT s."ShardId", s."ShardCount", s."LastDialogId"
    INTO v_shard_id, v_shard_count, v_last_dialog_id
    FROM partyresource."BackfillShardState" s
    WHERE NOT s."Completed"
    ORDER BY s."UpdatedAt", s."ShardId"
    FOR UPDATE SKIP LOCKED
    LIMIT 1;

    IF NOT FOUND THEN
        SELECT COALESCE(bool_and(s."Completed"), true)
        INTO v_all_shards_completed
        FROM partyresource."BackfillShardState" s;

        RETURN QUERY
        SELECT
            NULL::integer,
            0,
            0,
            0,
            0,
            false,
            v_all_shards_completed,
            NULL::uuid;
        RETURN;
    END IF;

    CREATE TEMP TABLE IF NOT EXISTS pg_temp.pr_backfill_batch
    (
        "Id" uuid NOT NULL,
        "Party" text NOT NULL,
        "ServiceResource" text NOT NULL
    )
    ON COMMIT DROP;

    CREATE TEMP TABLE IF NOT EXISTS pg_temp.pr_backfill_parsed
    (
        "ShortPrefix" char(1) NOT NULL,
        "UnprefixedPartyIdentifier" text NOT NULL,
        "UnprefixedResourceIdentifier" text NOT NULL
    )
    ON COMMIT DROP;

    TRUNCATE pg_temp.pr_backfill_batch;
    TRUNCATE pg_temp.pr_backfill_parsed;

    INSERT INTO pg_temp.pr_backfill_batch ("Id", "Party", "ServiceResource")
    SELECT d."Id", d."Party", d."ServiceResource"
    FROM public."Dialog" d
    WHERE d."Id" > v_last_dialog_id
      AND MOD((HASHTEXT(d."Id"::text)::bigint & 2147483647), v_shard_count) = v_shard_id
    ORDER BY d."Id"
    LIMIT p_batch_size;
    GET DIAGNOSTICS v_processed_rows = ROW_COUNT;

    IF v_processed_rows > 0 THEN
        SELECT COALESCE(
            (
                SELECT b."Id"
                FROM pg_temp.pr_backfill_batch b
                ORDER BY b."Id" DESC
                LIMIT 1
            ),
            v_last_dialog_id
        )
        INTO v_last_dialog_id;

        INSERT INTO pg_temp.pr_backfill_parsed ("ShortPrefix", "UnprefixedPartyIdentifier", "UnprefixedResourceIdentifier")
        SELECT
            parsed_party."ShortPrefix",
            parsed_party."UnprefixedPartyIdentifier",
            partyresource.resource_from_urn(b."ServiceResource")
        FROM pg_temp.pr_backfill_batch b
        CROSS JOIN LATERAL partyresource.party_parse_urn(b."Party") parsed_party
        WHERE b."ServiceResource" LIKE 'urn:altinn:resource:%'
        ;

        INSERT INTO partyresource."Party" ("ShortPrefix", "UnprefixedPartyIdentifier")
        SELECT DISTINCT b."ShortPrefix", b."UnprefixedPartyIdentifier"
        FROM pg_temp.pr_backfill_parsed b
        ORDER BY b."ShortPrefix", b."UnprefixedPartyIdentifier"
        ON CONFLICT ("ShortPrefix", "UnprefixedPartyIdentifier") DO NOTHING;
        GET DIAGNOSTICS v_inserted_parties = ROW_COUNT;

        INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
        SELECT DISTINCT b."UnprefixedResourceIdentifier"
        FROM pg_temp.pr_backfill_parsed b
        ORDER BY b."UnprefixedResourceIdentifier"
        ON CONFLICT ("UnprefixedResourceIdentifier") DO NOTHING;
        GET DIAGNOSTICS v_inserted_resources = ROW_COUNT;

        -- Join against base tables in a separate statement to avoid same-snapshot CTE visibility pitfalls.
        INSERT INTO partyresource."PartyResource" ("PartyId", "ResourceId")
        SELECT DISTINCT p."Id", r."Id"
        FROM pg_temp.pr_backfill_parsed b
        INNER JOIN partyresource."Party" p
            ON p."ShortPrefix" = b."ShortPrefix"
           AND p."UnprefixedPartyIdentifier" = b."UnprefixedPartyIdentifier"
        INNER JOIN partyresource."Resource" r
            ON r."UnprefixedResourceIdentifier" = b."UnprefixedResourceIdentifier"
        ORDER BY p."Id", r."Id"
        ON CONFLICT ("PartyId", "ResourceId") DO NOTHING;
        GET DIAGNOSTICS v_inserted_pairs = ROW_COUNT;
    END IF;

    v_shard_completed := v_processed_rows = 0;

    UPDATE partyresource."BackfillShardState" s
    SET
        "LastDialogId" = v_last_dialog_id,
        "Completed" = v_shard_completed,
        "UpdatedAt" = now()
    WHERE s."ShardId" = v_shard_id;

    SELECT COALESCE(bool_and(s."Completed"), true)
    INTO v_all_shards_completed
    FROM partyresource."BackfillShardState" s;

    RETURN QUERY
    SELECT
        v_shard_id,
        v_processed_rows,
        v_inserted_parties,
        v_inserted_resources,
        v_inserted_pairs,
        v_shard_completed,
        v_all_shards_completed,
        v_last_dialog_id;
END;
$$;
