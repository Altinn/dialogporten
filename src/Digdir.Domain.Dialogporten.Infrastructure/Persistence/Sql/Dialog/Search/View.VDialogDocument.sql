-- Produces weighted tsvectors per dialog so upserts remain a simple INSERT ... SELECT.
CREATE OR REPLACE VIEW search."VDialogDocument" AS
SELECT d."Id" AS "DialogId",
       (
         SELECT
           string_agg(
             setweight(
               to_tsvector(COALESCE(isomap."TsConfigName", 'simple')::regconfig, c."Value"), -- Fall back to simple when the language map lacks a match.
               c."Weight"::"char"
             )::text,
             ' '
           )::tsvector
         FROM search."VDialogContent" c
         LEFT JOIN search."Iso639TsVectorMap" isomap
           ON c."LanguageCode" = isomap."IsoCode"
         WHERE c."DialogId" = d."Id"
       ) AS "Document",
       d."Party" AS "Party"
FROM "Dialog" d;
