-- Produces weighted tsvectors per dialog so upserts remain a simple INSERT ... SELECT.
CREATE OR REPLACE VIEW search."VDialogDocument" AS
SELECT d."Id" AS "DialogId",
       (
         SELECT
           string_agg(
             setweight(
               to_tsvector(
                   -- Fall back to simple when the language map lacks a match.
                   COALESCE(isomap."TsConfigName", 'simple')::regconfig, 
                   -- Truncate to avoid overly large documents and errors.
                   LEFT(c."Value", 100000)), 
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
