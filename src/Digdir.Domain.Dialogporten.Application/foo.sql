DELETE FROM "Dialog"
WHERE "Id" = '01964495-683f-76c3-8682-51b2b78b64e5';

select count(*) from "Dialog";

select * from "Dialog";

SELECT
    n.nspname AS schema,
    c.relname AS table_name,
    pg_size_pretty(pg_total_relation_size(c.oid)) AS total_size,
    pg_size_pretty(pg_relation_size(c.oid))       AS table_size,
    pg_size_pretty(pg_indexes_size(c.oid))        AS index_size,
    pg_size_pretty(
        pg_total_relation_size(c.oid)
            - pg_relation_size(c.oid)
            - pg_indexes_size(c.oid)
    ) AS toast_size
FROM pg_class c
         JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'r'  -- 'r' = ordinary table
  AND n.nspname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_total_relation_size(c.oid) DESC;

cret tabl pls search hack

-- SET enable_seqscan = ON;
WITH relevantLocalizationSet AS (
    SELECT DISTINCT l."LocalizationSetId"
    FROM "Localization" l
    WHERE l."Value" ILIKE '%SINT%'
),
     localizationSetDialogMap AS (
         SELECT ls."Id" localizationSetId
              ,ga."DialogId" dialogId
         FROM "LocalizationSet" ls
                  INNER JOIN "DialogGuiAction" ga ON
             ga."Id" = ls."GuiActionId"
                 OR ga."Id" = ls."DialogGuiActionPrompt_GuiActionId"

         UNION ALL SELECT ls."Id" localizationSetId
                        ,da."DialogId" dialogId
         FROM "LocalizationSet" ls
                  INNER JOIN "DialogActivity" da ON
             da."Id" = ls."ActivityId"

         UNION ALL SELECT ls."Id" localizationSetId
                        ,COALESCE(a."DialogId", dt."DialogId") dialogId
         FROM "LocalizationSet" ls
                  INNER JOIN "Attachment" a ON
             a."Id" = ls."AttachmentId"
                  LEFT JOIN "DialogTransmission" dt ON
             dt."Id" = a."TransmissionId"

         UNION ALL SELECT ls."Id" localizationSetId
                        ,c."DialogId" dialogId
         FROM "LocalizationSet" ls
                  INNER JOIN "DialogContent" c ON
             c."Id" = ls."DialogContentId"

         UNION ALL SELECT ls."Id" localizationSetId
                        ,dt."DialogId" dialogId
         FROM "LocalizationSet" ls
                  INNER JOIN "DialogTransmissionContent" c ON
             c."Id" = ls."TransmissionContentId"
                  INNER JOIN "DialogTransmission" dt ON
             dt."Id" = c."TransmissionId"
     ),
     relevantMap AS (
         SELECT DISTINCT m.dialogId
         FROM relevantLocalizationSet r
                  INNER JOIN localizationSetDialogMap m ON r."LocalizationSetId" = m.localizationSetId
     )
SELECT d."Id"
FROM "Dialog" d
         INNER JOIN relevantMap m ON d."Id" = m.dialogId
LIMIT 10000;

analyse;




alter table public."LocalizationSet"
    add constraint "FK_LocalizationSet_DialogTransmissionContent_TransmissionConte~"
        foreign key ("TransmissionContentId") references public."DialogTransmissionContent"
            on delete cascade;

alter table public."DialogTransmissionContent"
    add constraint "PK_DialogTransmissionContent"
        primary key ("Id");

create unique index PK_DialogTransmissionContent
    on public."DialogTransmissionContent" ("Id")
    include ("TransmissionId");

alter table public."DialogTransmissionContent"
    add constraint "PK_DialogTransmissionContent"
        primary key using  index PK_DialogTransmissionContent;






create unique index PK_DialogTransmission
    on public."DialogTransmission" ("Id")
    include ("DialogId");

alter table public."DialogTransmission"
    add constraint "PK_DialogTransmission"
        primary key using  index PK_DialogTransmission;


alter table public."DialogTransmissionContent"
    add constraint "FK_DialogTransmissionContent_DialogTransmission_TransmissionId"
        foreign key ("TransmissionId") references public."DialogTransmission"
            on delete cascade;

alter table public."DialogTransmission"
    add constraint "FK_DialogTransmission_DialogTransmission_RelatedTransmissionId"
        foreign key ("RelatedTransmissionId") references public."DialogTransmission"
            on delete set null;

alter table public."Actor"
    add constraint "FK_Actor_DialogTransmission_TransmissionId"
        foreign key ("TransmissionId") references public."DialogTransmission"
            on delete cascade;

alter table public."Attachment"
    add constraint "FK_Attachment_DialogTransmission_TransmissionId"
        foreign key ("TransmissionId") references public."DialogTransmission"
            on delete cascade;

alter table public."DialogActivity"
    add constraint "FK_DialogActivity_DialogTransmission_TransmissionId"
        foreign key ("TransmissionId") references public."DialogTransmission"
            on delete set null;







select count(*) from "Dialog";

-- CREATE INDEX IX_Localization_Value_SetId
--     ON "Localization" USING gin (("Value" gin_trgm_ops), "LocalizationSetId");

CREATE INDEX IX_Localization_SetId
    ON "Localization" ("LocalizationSetId");

-- remove the above index
drop index IX_Localization_SetId;


drop index IX_LocalizationSet_GuiActionId;
drop index IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId;
drop index IX_LocalizationSet_ActivityId;
drop index IX_LocalizationSet_AttachmentId;
drop index IX_LocalizationSet_DialogContentId;
drop index IX_LocalizationSet_TransmissionContentId;


CREATE UNIQUE INDEX IX_LocalizationSet_GuiActionId
    ON "LocalizationSet" ("GuiActionId")
    INCLUDE ("Id")
    WHERE "GuiActionId" IS NOT NULL;

CREATE UNIQUE INDEX IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId
    ON "LocalizationSet" ("DialogGuiActionPrompt_GuiActionId")
    INCLUDE ("Id")
    WHERE "DialogGuiActionPrompt_GuiActionId" IS NOT NULL;

CREATE UNIQUE INDEX IX_LocalizationSet_ActivityId
    ON "LocalizationSet" ("ActivityId")
    INCLUDE ("Id")
    WHERE "ActivityId" IS NOT NULL;

CREATE UNIQUE INDEX IX_LocalizationSet_AttachmentId
    ON "LocalizationSet" ("AttachmentId")
    INCLUDE ("Id")
    WHERE "AttachmentId" IS NOT NULL;

CREATE UNIQUE INDEX IX_LocalizationSet_DialogContentId
    ON "LocalizationSet" ("DialogContentId")
    INCLUDE ("Id")
    WHERE "DialogContentId" IS NOT NULL;

CREATE UNIQUE INDEX IX_LocalizationSet_TransmissionContentId
    ON "LocalizationSet" ("TransmissionContentId")
    INCLUDE ("Id")
    WHERE "TransmissionContentId" IS NOT NULL;




-- CREATE INDEX IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId
--     ON "LocalizationSet" ("DialogGuiActionPrompt_GuiActionId")
--     INCLUDE ("Id");
--
-- -- For DialogActivity join
-- CREATE INDEX IX_LocalizationSet_ActivityId
--     ON "LocalizationSet" ("ActivityId")
--     INCLUDE ("Id");
--
-- -- For Attachment join
-- CREATE INDEX IX_LocalizationSet_AttachmentId
--     ON "LocalizationSet" ("AttachmentId")
--     INCLUDE ("Id");
--
-- -- For DialogContent join
-- CREATE INDEX IX_LocalizationSet_DialogContentId
--     ON "LocalizationSet" ("DialogContentId")
--     INCLUDE ("Id");
--
-- -- For TransmissionContent join
-- CREATE INDEX IX_LocalizationSet_TransmissionContentId
--     ON "LocalizationSet" ("TransmissionContentId")
--     INCLUDE ("Id");


-- nytt schema, content search
-- dialogId, concat all content gin index

-- 1️⃣ Create the table without constraints
CREATE TABLE "DialogSearch" (
    "DialogId" UUID NOT NULL,
    "SearchValue" TEXT NOT NULL
);

-- 2️⃣ Add primary key constraint
ALTER TABLE "DialogSearch"
    ADD CONSTRAINT pk_dialog_search PRIMARY KEY ("DialogId");

-- 3️⃣ Add foreign key constraint
ALTER TABLE "DialogSearch"
    ADD CONSTRAINT fk_dialog_search_dialog
        FOREIGN KEY ("DialogId")
            REFERENCES "Dialog"("Id")
            ON DELETE CASCADE;

-- 4️⃣ Create GIN index on searchValue (trigram search)
CREATE INDEX dialog_search_searchvalue_gin
    ON "DialogSearch"
        USING gin ("SearchValue" gin_trgm_ops);
