-- Produces weighted tsvectors per dialog so upserts remain a simple INSERT ... SELECT.
CREATE OR REPLACE VIEW search."VDialogDocument" AS
SELECT d."Id"    AS "DialogId"
     , (
    SELECT public.tsvector_agg(
               SETWEIGHT(
                   -- Fall back to simple when the language map lacks a match.
                   TO_TSVECTOR(COALESCE(isomap."TsConfigName", 'simple')::regconfig, c."Value"),
                   c."Weight"::"char"
               )
           )
    FROM search."VDialogContent" c
    LEFT JOIN search."Iso639TsVectorMap" isomap
        ON c."LanguageCode" = isomap."IsoCode"
    WHERE c."DialogId" = d."Id"
)                AS "Document"
     , d."Party" AS "Party"
FROM "Dialog" d;
