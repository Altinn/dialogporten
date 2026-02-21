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
    v_attempt integer := 0;
    v_max_attempts constant integer := 5;
BEGIN
    IF p_batch_size <= 0 THEN
        RAISE EXCEPTION 'p_batch_size must be > 0'
            USING ERRCODE = '22023';
    END IF;

    LOOP
        v_attempt := v_attempt + 1;

        BEGIN
            v_processed_rows := 0;
            v_inserted_parties := 0;
            v_inserted_resources := 0;
            v_inserted_pairs := 0;
            v_shard_completed := false;
            v_all_shards_completed := false;

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
                    true,
                    v_all_shards_completed,
                    NULL::uuid;
                RETURN;
            END IF;

            WITH batch AS MATERIALIZED (
                SELECT d."Id", d."Party", d."ServiceResource"
                FROM public."Dialog" d
                WHERE d."Id" > v_last_dialog_id
                  AND MOD((HASHTEXT(d."Id"::text)::bigint & 2147483647), v_shard_count) = v_shard_id
                ORDER BY d."Id"
                LIMIT p_batch_size
            ),
            valid AS MATERIALIZED (
                SELECT b."Id", b."Party", b."ServiceResource"
                FROM batch b
                WHERE b."ServiceResource" LIKE 'urn:altinn:resource:%'
                  AND (
                    b."Party" LIKE 'urn:altinn:organization:identifier-no:%'
                    OR b."Party" LIKE 'urn:altinn:person:identifier-no:%'
                    OR b."Party" LIKE 'urn:altinn:person:legacy-selfidentified:%'
                    OR b."Party" LIKE 'urn:altinn:person:idporten-email:%'
                    OR b."Party" LIKE 'urn:altinn:systemuser:uuid:%'
                    OR b."Party" LIKE 'urn:altinn:feide-subject:%'
                )
            ),
            parsed AS MATERIALIZED (
                SELECT
                    v."Id",
                    parsed_party."ShortPrefix",
                    parsed_party."UnprefixedPartyIdentifier",
                    partyresource.resource_from_urn(v."ServiceResource") AS "UnprefixedResourceIdentifier"
                FROM valid v
                CROSS JOIN LATERAL partyresource.party_parse_urn(v."Party") parsed_party
            ),
            party_candidates AS MATERIALIZED (
                SELECT DISTINCT p."ShortPrefix", p."UnprefixedPartyIdentifier"
                FROM parsed p
            ),
            resource_candidates AS MATERIALIZED (
                SELECT DISTINCT p."UnprefixedResourceIdentifier"
                FROM parsed p
            ),
            inserted_parties AS (
                INSERT INTO partyresource."Party" ("ShortPrefix", "UnprefixedPartyIdentifier")
                SELECT c."ShortPrefix", c."UnprefixedPartyIdentifier"
                FROM party_candidates c
                LEFT JOIN partyresource."Party" existing
                  ON existing."ShortPrefix" = c."ShortPrefix"
                 AND existing."UnprefixedPartyIdentifier" = c."UnprefixedPartyIdentifier"
                WHERE existing."Id" IS NULL
                ORDER BY c."ShortPrefix", c."UnprefixedPartyIdentifier"
                ON CONFLICT ("ShortPrefix", "UnprefixedPartyIdentifier") DO NOTHING
                RETURNING 1
            ),
            inserted_resources AS (
                INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
                SELECT c."UnprefixedResourceIdentifier"
                FROM resource_candidates c
                LEFT JOIN partyresource."Resource" existing
                  ON existing."UnprefixedResourceIdentifier" = c."UnprefixedResourceIdentifier"
                WHERE existing."Id" IS NULL
                ORDER BY c."UnprefixedResourceIdentifier"
                ON CONFLICT ("UnprefixedResourceIdentifier") DO NOTHING
                RETURNING 1
            ),
            pair_candidates AS MATERIALIZED (
                SELECT DISTINCT pty."Id" AS "PartyId", res."Id" AS "ResourceId"
                FROM parsed p
                INNER JOIN partyresource."Party" pty
                    ON pty."ShortPrefix" = p."ShortPrefix"
                   AND pty."UnprefixedPartyIdentifier" = p."UnprefixedPartyIdentifier"
                INNER JOIN partyresource."Resource" res
                    ON res."UnprefixedResourceIdentifier" = p."UnprefixedResourceIdentifier"
            ),
            inserted_pairs AS (
                INSERT INTO partyresource."PartyResource" ("PartyId", "ResourceId")
                SELECT c."PartyId", c."ResourceId"
                FROM pair_candidates c
                LEFT JOIN partyresource."PartyResource" existing
                  ON existing."PartyId" = c."PartyId"
                 AND existing."ResourceId" = c."ResourceId"
                WHERE existing."PartyId" IS NULL
                ORDER BY c."PartyId", c."ResourceId"
                ON CONFLICT ("PartyId", "ResourceId") DO NOTHING
                RETURNING 1
            ),
            stats AS (
                SELECT
                    (SELECT COUNT(*)::integer FROM batch) AS "ProcessedRows",
                    (SELECT COUNT(*)::integer FROM inserted_parties) AS "InsertedParties",
                    (SELECT COUNT(*)::integer FROM inserted_resources) AS "InsertedResources",
                    (SELECT COUNT(*)::integer FROM inserted_pairs) AS "InsertedPairs",
                    (SELECT b."Id" FROM batch b ORDER BY b."Id" DESC LIMIT 1) AS "MaxDialogId"
            )
            SELECT
                s."ProcessedRows",
                s."InsertedParties",
                s."InsertedResources",
                s."InsertedPairs",
                COALESCE(s."MaxDialogId", v_last_dialog_id)
            INTO
                v_processed_rows,
                v_inserted_parties,
                v_inserted_resources,
                v_inserted_pairs,
                v_last_dialog_id
            FROM stats s;

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
            RETURN;
        EXCEPTION
            WHEN deadlock_detected THEN
                IF v_attempt >= v_max_attempts THEN
                    RAISE;
                END IF;

                PERFORM pg_sleep((0.02 * v_attempt) + (random() * 0.08));
        END;
    END LOOP;
END;
$$;
