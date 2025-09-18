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

WITH input AS (
    SELECT NOW() now
        ,null::text[] org
--         ,'{"pod"}'::text[] org
        ,null::text[] serviceResource
        ,'{"urn:altinn:person:identifier-no:06917699338","urn:altinn:person:identifier-no:09886998144","urn:altinn:person:identifier-no:29875597734","urn:altinn:person:identifier-no:01857396517","urn:altinn:person:identifier-no:18867397910","urn:altinn:person:identifier-no:28928397302","urn:altinn:person:identifier-no:02927997919","urn:altinn:person:identifier-no:20824699322","urn:altinn:person:identifier-no:21904199173","urn:altinn:person:identifier-no:04885799588","urn:altinn:person:identifier-no:07907399324","urn:altinn:person:identifier-no:02916298334","urn:altinn:person:identifier-no:14916099488","urn:altinn:person:identifier-no:10837099310","urn:altinn:person:identifier-no:05906599602","urn:altinn:person:identifier-no:03905398104","urn:altinn:person:identifier-no:22876598926","urn:altinn:person:identifier-no:04915199904","urn:altinn:person:identifier-no:05914998583","urn:altinn:person:identifier-no:07826998711"}'::text[] party
        ,null::text[] extendedStatus
        ,null::text externalReference
        ,null::int[] status
        ,null::timestamptz createdAfter
        ,null::timestamptz createdBefore
        ,null::timestamptz updatedAfter
        ,null::timestamptz updatedBefore
        ,null::timestamptz contentUpdatedAfter
        ,null::timestamptz contentUpdatedBefore
        ,null::timestamptz dueAfter
        ,null::timestamptz dueBefore
        ,null::text process
        ,null::boolean excludeApiOnly
        ,'attentat'::text search
        ,null::int[] AS systemLabel
)
    ,partyResourceAccess AS (
    SELECT DISTINCT s.service, p.party
    FROM jsonb_to_recordset(
             --'[{"parties":["urn:altinn:person:identifier-no:06917699338","urn:altinn:person:identifier-no:09886998144","urn:altinn:person:identifier-no:29875597734","urn:altinn:person:identifier-no:01857396517","urn:altinn:person:identifier-no:18867397910","urn:altinn:person:identifier-no:28928397302","urn:altinn:person:identifier-no:02927997919","urn:altinn:person:identifier-no:20824699322","urn:altinn:person:identifier-no:21904199173","urn:altinn:person:identifier-no:04885799588","urn:altinn:person:identifier-no:07907399324","urn:altinn:person:identifier-no:02916298334","urn:altinn:person:identifier-no:14916099488","urn:altinn:person:identifier-no:10837099310","urn:altinn:person:identifier-no:05906599602","urn:altinn:person:identifier-no:03905398104","urn:altinn:person:identifier-no:22876598926","urn:altinn:person:identifier-no:04915199904","urn:altinn:person:identifier-no:05914998583","urn:altinn:person:identifier-no:07826998711","urn:altinn:person:identifier-no:02897196746","urn:altinn:person:identifier-no:07916795933","urn:altinn:person:identifier-no:23826098759","urn:altinn:person:identifier-no:04886399370","urn:altinn:person:identifier-no:04886796116","urn:altinn:person:identifier-no:27894297765","urn:altinn:person:identifier-no:05906297568","urn:altinn:person:identifier-no:15917599510","urn:altinn:person:identifier-no:06927399960","urn:altinn:person:identifier-no:01836499324","urn:altinn:person:identifier-no:09836597599","urn:altinn:person:identifier-no:02846097964","urn:altinn:person:identifier-no:02847096260","urn:altinn:person:identifier-no:02845994504","urn:altinn:person:identifier-no:29846797805","urn:altinn:person:identifier-no:06915698164","urn:altinn:person:identifier-no:07907995903","urn:altinn:person:identifier-no:28866696375","urn:altinn:person:identifier-no:01826597182","urn:altinn:person:identifier-no:23837097983","urn:altinn:person:identifier-no:27886796175","urn:altinn:person:identifier-no:07826597369","urn:altinn:person:identifier-no:07867297132","urn:altinn:person:identifier-no:03836796984","urn:altinn:person:identifier-no:23915398146","urn:altinn:person:identifier-no:27816397540","urn:altinn:person:identifier-no:09895999323","urn:altinn:person:identifier-no:04826299733","urn:altinn:person:identifier-no:03836695584","urn:altinn:person:identifier-no:16896795523","urn:altinn:organization:identifier-no:313092887","urn:altinn:organization:identifier-no:313029514","urn:altinn:organization:identifier-no:313981568","urn:altinn:organization:identifier-no:313229963","urn:altinn:organization:identifier-no:310760765","urn:altinn:organization:identifier-no:310366323","urn:altinn:organization:identifier-no:313274527","urn:altinn:organization:identifier-no:212399892","urn:altinn:organization:identifier-no:312433508","urn:altinn:organization:identifier-no:311129066","urn:altinn:organization:identifier-no:313807541","urn:altinn:organization:identifier-no:313554031","urn:altinn:organization:identifier-no:312252007","urn:altinn:organization:identifier-no:312210134","urn:altinn:organization:identifier-no:213325612","urn:altinn:organization:identifier-no:311093819","urn:altinn:organization:identifier-no:310812935","urn:altinn:organization:identifier-no:313656446","urn:altinn:organization:identifier-no:311795201","urn:altinn:organization:identifier-no:310303801","urn:altinn:organization:identifier-no:210258272","urn:altinn:organization:identifier-no:310551112","urn:altinn:organization:identifier-no:313576159","urn:altinn:organization:identifier-no:312152258","urn:altinn:organization:identifier-no:313858022","urn:altinn:organization:identifier-no:312898497","urn:altinn:organization:identifier-no:310262501","urn:altinn:organization:identifier-no:310681423","urn:altinn:organization:identifier-no:312753812","urn:altinn:organization:identifier-no:314041593","urn:altinn:organization:identifier-no:310740306","urn:altinn:organization:identifier-no:311694162","urn:altinn:organization:identifier-no:212218952","urn:altinn:organization:identifier-no:310125202","urn:altinn:organization:identifier-no:311697021","urn:altinn:organization:identifier-no:310157899","urn:altinn:organization:identifier-no:213656902","urn:altinn:organization:identifier-no:213922092","urn:altinn:organization:identifier-no:314289390","urn:altinn:organization:identifier-no:310287377","urn:altinn:organization:identifier-no:313773167","urn:altinn:organization:identifier-no:311006398","urn:altinn:organization:identifier-no:313716805","urn:altinn:organization:identifier-no:311708309","urn:altinn:organization:identifier-no:313947009","urn:altinn:organization:identifier-no:314080416","urn:altinn:organization:identifier-no:211005572","urn:altinn:organization:identifier-no:313615405","urn:altinn:organization:identifier-no:310816086","urn:altinn:organization:identifier-no:313886255","urn:altinn:organization:identifier-no:313740145","urn:altinn:organization:identifier-no:310381950","urn:altinn:organization:identifier-no:312629631","urn:altinn:organization:identifier-no:313554880","urn:altinn:organization:identifier-no:313429903","urn:altinn:organization:identifier-no:312838257","urn:altinn:organization:identifier-no:313186792","urn:altinn:organization:identifier-no:312588927","urn:altinn:organization:identifier-no:311172514","urn:altinn:organization:identifier-no:314103238","urn:altinn:organization:identifier-no:313947386","urn:altinn:organization:identifier-no:310279765","urn:altinn:organization:identifier-no:313699188","urn:altinn:organization:identifier-no:310558605","urn:altinn:organization:identifier-no:313440761","urn:altinn:organization:identifier-no:313315061","urn:altinn:organization:identifier-no:210856102","urn:altinn:organization:identifier-no:310880701","urn:altinn:organization:identifier-no:313360210","urn:altinn:organization:identifier-no:310236802","urn:altinn:organization:identifier-no:313370712","urn:altinn:organization:identifier-no:313638898","urn:altinn:organization:identifier-no:310085251","urn:altinn:organization:identifier-no:212696722","urn:altinn:organization:identifier-no:310223395","urn:altinn:organization:identifier-no:312766132","urn:altinn:organization:identifier-no:312846586","urn:altinn:organization:identifier-no:310335339","urn:altinn:organization:identifier-no:313612473","urn:altinn:organization:identifier-no:312394375","urn:altinn:organization:identifier-no:310565342","urn:altinn:organization:identifier-no:313210235","urn:altinn:organization:identifier-no:310179566","urn:altinn:organization:identifier-no:312923882","urn:altinn:organization:identifier-no:312209136","urn:altinn:organization:identifier-no:312671131","urn:altinn:organization:identifier-no:310908169","urn:altinn:organization:identifier-no:214113872","urn:altinn:organization:identifier-no:310882364","urn:altinn:organization:identifier-no:312695669","urn:altinn:organization:identifier-no:312506599","urn:altinn:organization:identifier-no:310667161","urn:altinn:organization:identifier-no:311731831","urn:altinn:organization:identifier-no:313727564","urn:altinn:organization:identifier-no:314214617","urn:altinn:organization:identifier-no:312903547","urn:altinn:organization:identifier-no:311015877","urn:altinn:organization:identifier-no:213693522","urn:altinn:organization:identifier-no:212457892","urn:altinn:organization:identifier-no:211159022"],"services":["urn:altinn:resource:altinn-profil-api-varslingsdaresser-for-virksomheter","urn:altinn:resource:altinn_keyrole_access","urn:altinn:resource:asf-migratedcorrespondence-3996-201503","urn:altinn:resource:asf-migratedcorrespondence-4786-1","urn:altinn:resource:asf-migratedcorrespondence-5132-1","urn:altinn:resource:brg-migratedcorrespondence-3770-1","urn:altinn:resource:brg-migratedcorrespondence-3771-1","urn:altinn:resource:brg-migratedcorrespondence-3772-1","urn:altinn:resource:brg-migratedcorrespondence-3773-1","urn:altinn:resource:brg-migratedcorrespondence-3800-1","urn:altinn:resource:brg-migratedcorrespondence-4070-1","urn:altinn:resource:brg-migratedcorrespondence-4071-1","urn:altinn:resource:brg-migratedcorrespondence-4293-1","urn:altinn:resource:brg-migratedcorrespondence-4495-1","urn:altinn:resource:brg-migratedcorrespondence-6009-1","urn:altinn:resource:dagl-correspondence","urn:altinn:resource:dagl-correspondence-0","urn:altinn:resource:dagl-correspondence-1","urn:altinn:resource:dagl-correspondence-10","urn:altinn:resource:dagl-correspondence-2","urn:altinn:resource:dagl-correspondence-3","urn:altinn:resource:dagl-correspondence-4","urn:altinn:resource:dagl-correspondence-5","urn:altinn:resource:dagl-correspondence-6","urn:altinn:resource:dagl-correspondence-7","urn:altinn:resource:dagl-correspondence-8","urn:altinn:resource:dagl-correspondence-9","urn:altinn:resource:dfo-migratedcorrespondence-4351-1","urn:altinn:resource:dfo-migratedcorrespondence-4352-2","urn:altinn:resource:fors-migratedcorrespondence-5075-1","urn:altinn:resource:fors-migratedcorrespondence-5133-1","urn:altinn:resource:kmd-migratedcorrespondence-3873-201409","urn:altinn:resource:mdir-migratedcorrespondence-5226-1","urn:altinn:resource:medl-correspondence-1","urn:altinn:resource:nav-migratedcorrespondence-4503-2","urn:altinn:resource:nav-migratedcorrespondence-4751-1","urn:altinn:resource:nav-migratedcorrespondence-5062-1","urn:altinn:resource:nav-migratedcorrespondence-5278-1","urn:altinn:resource:nav-migratedcorrespondence-5516-1","urn:altinn:resource:nav-migratedcorrespondence-5516-2","urn:altinn:resource:nav-migratedcorrespondence-5516-3","urn:altinn:resource:nav-migratedcorrespondence-5516-4","urn:altinn:resource:nav-migratedcorrespondence-5516-5","urn:altinn:resource:nav-migratedcorrespondence-5793-1","urn:altinn:resource:pod-migratedcorrespondence-4737-1","urn:altinn:resource:pod-migratedcorrespondence-5482-1","urn:altinn:resource:pod-migratedcorrespondence-5484-1","urn:altinn:resource:pod-migratedcorrespondence-5485-1","urn:altinn:resource:pod-migratedcorrespondence-5486-1","urn:altinn:resource:skd-migratedcorrespondence-2506-110222","urn:altinn:resource:skd-migratedcorrespondence-2772-110914","urn:altinn:resource:skd-migratedcorrespondence-2772-120301","urn:altinn:resource:skd-migratedcorrespondence-2772-120309","urn:altinn:resource:skd-migratedcorrespondence-2772-140313","urn:altinn:resource:skd-migratedcorrespondence-2929-111209","urn:altinn:resource:skd-migratedcorrespondence-2969-120112","urn:altinn:resource:skd-migratedcorrespondence-3102-120420","urn:altinn:resource:skd-migratedcorrespondence-3665-131016","urn:altinn:resource:skd-migratedcorrespondence-4123-150602","urn:altinn:resource:skd-migratedcorrespondence-5138-180522","urn:altinn:resource:skd-migratedcorrespondence-5179-180822","urn:altinn:resource:skd-migratedcorrespondence-5227-1","urn:altinn:resource:skd-migratedcorrespondence-5228-1","urn:altinn:resource:skd-migratedcorrespondence-5229-1","urn:altinn:resource:skd-migratedcorrespondence-5355-190607","urn:altinn:resource:skd-migratedcorrespondence-5356-190607","urn:altinn:resource:skd-migratedcorrespondence-5357-190607","urn:altinn:resource:skd-migratedcorrespondence-5488-191121","urn:altinn:resource:skd-migratedcorrespondence-5601-2020","urn:altinn:resource:skd-migratedcorrespondence-5651-1","urn:altinn:resource:skd-migratedcorrespondence-5760-211028","urn:altinn:resource:skd-migratedcorrespondence-5761-211028","urn:altinn:resource:skd-migratedcorrespondence-5764-211028","urn:altinn:resource:skd-migratedcorrespondence-5766-211028","urn:altinn:resource:skd-migratedcorrespondence-5854-220907","urn:altinn:resource:skd-migratedcorrespondence-5855-220907","urn:altinn:resource:skd-migratedcorrespondence-skdpsa-2","urn:altinn:resource:skd-migratedcorrespondence-skfskb-1","urn:altinn:resource:super-simple-service","urn:altinn:resource:svv-migratedcorrespondence-4310-201511","urn:altinn:resource:svv-migratedcorrespondence-4316-201512","urn:altinn:resource:svv-migratedcorrespondence-4319-201512","urn:altinn:resource:svv-migratedcorrespondence-4320-201512","urn:altinn:resource:svv-migratedcorrespondence-4321-201512","urn:altinn:resource:svv-migratedcorrespondence-4322-201512","urn:altinn:resource:svv-migratedcorrespondence-4921-201704","urn:altinn:resource:svv-migratedcorrespondence-5230-201801","urn:altinn:resource:svv-migratedcorrespondence-5230-201802","urn:altinn:resource:ttd-altinn-events-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests-2","urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence","urn:altinn:resource:ttd-dialogporten-performance-test-01","urn:altinn:resource:ttd-dialogporten-performance-test-02","urn:altinn:resource:ttd-dialogporten-performance-test-03","urn:altinn:resource:ttd-dialogporten-performance-test-04","urn:altinn:resource:ttd-dialogporten-performance-test-05","urn:altinn:resource:ttd-dialogporten-performance-test-06","urn:altinn:resource:ttd-dialogporten-performance-test-07","urn:altinn:resource:ttd-dialogporten-performance-test-08","urn:altinn:resource:ttd-dialogporten-performance-test-09","urn:altinn:resource:ttd-dialogporten-performance-test-10"]}]'
             --'[{"parties":["urn:altinn:person:identifier-no:02895596925","urn:altinn:person:identifier-no:09886997512", "urn:altinn:person:identifier-no:06878699953", "urn:altinn:person:identifier-no:05845198324", "urn:altinn:person:identifier-no:06864996947"],"services":["urn:altinn:resource:altinn-profil-api-varslingsdaresser-for-virksomheter","urn:altinn:resource:altinn_keyrole_access","urn:altinn:resource:asf-migratedcorrespondence-3996-201503","urn:altinn:resource:asf-migratedcorrespondence-4786-1","urn:altinn:resource:asf-migratedcorrespondence-5132-1","urn:altinn:resource:brg-migratedcorrespondence-3770-1","urn:altinn:resource:brg-migratedcorrespondence-3771-1","urn:altinn:resource:brg-migratedcorrespondence-3772-1","urn:altinn:resource:brg-migratedcorrespondence-3773-1","urn:altinn:resource:brg-migratedcorrespondence-3800-1","urn:altinn:resource:brg-migratedcorrespondence-4070-1","urn:altinn:resource:brg-migratedcorrespondence-4071-1","urn:altinn:resource:brg-migratedcorrespondence-4293-1","urn:altinn:resource:brg-migratedcorrespondence-4495-1","urn:altinn:resource:brg-migratedcorrespondence-6009-1","urn:altinn:resource:dagl-correspondence","urn:altinn:resource:dagl-correspondence-0","urn:altinn:resource:dagl-correspondence-1","urn:altinn:resource:dagl-correspondence-10","urn:altinn:resource:dagl-correspondence-2","urn:altinn:resource:dagl-correspondence-3","urn:altinn:resource:dagl-correspondence-4","urn:altinn:resource:dagl-correspondence-5","urn:altinn:resource:dagl-correspondence-6","urn:altinn:resource:dagl-correspondence-7","urn:altinn:resource:dagl-correspondence-8","urn:altinn:resource:dagl-correspondence-9","urn:altinn:resource:dfo-migratedcorrespondence-4351-1","urn:altinn:resource:dfo-migratedcorrespondence-4352-2","urn:altinn:resource:fors-migratedcorrespondence-5075-1","urn:altinn:resource:fors-migratedcorrespondence-5133-1","urn:altinn:resource:kmd-migratedcorrespondence-3873-201409","urn:altinn:resource:mdir-migratedcorrespondence-5226-1","urn:altinn:resource:medl-correspondence-1","urn:altinn:resource:nav-migratedcorrespondence-4503-2","urn:altinn:resource:nav-migratedcorrespondence-4751-1","urn:altinn:resource:nav-migratedcorrespondence-5062-1","urn:altinn:resource:nav-migratedcorrespondence-5278-1","urn:altinn:resource:nav-migratedcorrespondence-5516-1","urn:altinn:resource:nav-migratedcorrespondence-5516-2","urn:altinn:resource:nav-migratedcorrespondence-5516-3","urn:altinn:resource:nav-migratedcorrespondence-5516-4","urn:altinn:resource:nav-migratedcorrespondence-5516-5","urn:altinn:resource:nav-migratedcorrespondence-5793-1","urn:altinn:resource:pod-migratedcorrespondence-4737-1","urn:altinn:resource:pod-migratedcorrespondence-5482-1","urn:altinn:resource:pod-migratedcorrespondence-5484-1","urn:altinn:resource:pod-migratedcorrespondence-5485-1","urn:altinn:resource:pod-migratedcorrespondence-5486-1","urn:altinn:resource:skd-migratedcorrespondence-2506-110222","urn:altinn:resource:skd-migratedcorrespondence-2772-110914","urn:altinn:resource:skd-migratedcorrespondence-2772-120301","urn:altinn:resource:skd-migratedcorrespondence-2772-120309","urn:altinn:resource:skd-migratedcorrespondence-2772-140313","urn:altinn:resource:skd-migratedcorrespondence-2929-111209","urn:altinn:resource:skd-migratedcorrespondence-2969-120112","urn:altinn:resource:skd-migratedcorrespondence-3102-120420","urn:altinn:resource:skd-migratedcorrespondence-3665-131016","urn:altinn:resource:skd-migratedcorrespondence-4123-150602","urn:altinn:resource:skd-migratedcorrespondence-5138-180522","urn:altinn:resource:skd-migratedcorrespondence-5179-180822","urn:altinn:resource:skd-migratedcorrespondence-5227-1","urn:altinn:resource:skd-migratedcorrespondence-5228-1","urn:altinn:resource:skd-migratedcorrespondence-5229-1","urn:altinn:resource:skd-migratedcorrespondence-5355-190607","urn:altinn:resource:skd-migratedcorrespondence-5356-190607","urn:altinn:resource:skd-migratedcorrespondence-5357-190607","urn:altinn:resource:skd-migratedcorrespondence-5488-191121","urn:altinn:resource:skd-migratedcorrespondence-5601-2020","urn:altinn:resource:skd-migratedcorrespondence-5651-1","urn:altinn:resource:skd-migratedcorrespondence-5760-211028","urn:altinn:resource:skd-migratedcorrespondence-5761-211028","urn:altinn:resource:skd-migratedcorrespondence-5764-211028","urn:altinn:resource:skd-migratedcorrespondence-5766-211028","urn:altinn:resource:skd-migratedcorrespondence-5854-220907","urn:altinn:resource:skd-migratedcorrespondence-5855-220907","urn:altinn:resource:skd-migratedcorrespondence-skdpsa-2","urn:altinn:resource:skd-migratedcorrespondence-skfskb-1","urn:altinn:resource:super-simple-service","urn:altinn:resource:svv-migratedcorrespondence-4310-201511","urn:altinn:resource:svv-migratedcorrespondence-4316-201512","urn:altinn:resource:svv-migratedcorrespondence-4319-201512","urn:altinn:resource:svv-migratedcorrespondence-4320-201512","urn:altinn:resource:svv-migratedcorrespondence-4321-201512","urn:altinn:resource:svv-migratedcorrespondence-4322-201512","urn:altinn:resource:svv-migratedcorrespondence-4921-201704","urn:altinn:resource:svv-migratedcorrespondence-5230-201801","urn:altinn:resource:svv-migratedcorrespondence-5230-201802","urn:altinn:resource:ttd-altinn-events-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests-2","urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence","urn:altinn:resource:ttd-dialogporten-performance-test-01","urn:altinn:resource:ttd-dialogporten-performance-test-02","urn:altinn:resource:ttd-dialogporten-performance-test-03","urn:altinn:resource:ttd-dialogporten-performance-test-04","urn:altinn:resource:ttd-dialogporten-performance-test-05","urn:altinn:resource:ttd-dialogporten-performance-test-06","urn:altinn:resource:ttd-dialogporten-performance-test-07","urn:altinn:resource:ttd-dialogporten-performance-test-08","urn:altinn:resource:ttd-dialogporten-performance-test-09","urn:altinn:resource:ttd-dialogporten-performance-test-10"]}]'
             --'[{"parties":["urn:altinn:organization:identifier-no:313092887"],"services":["urn:altinn:resource:altinn-profil-api-varslingsdaresser-for-virksomheter","urn:altinn:resource:altinn_keyrole_access","urn:altinn:resource:asf-migratedcorrespondence-3996-201503","urn:altinn:resource:asf-migratedcorrespondence-4786-1","urn:altinn:resource:asf-migratedcorrespondence-5132-1","urn:altinn:resource:brg-migratedcorrespondence-3770-1","urn:altinn:resource:brg-migratedcorrespondence-3771-1","urn:altinn:resource:brg-migratedcorrespondence-3772-1","urn:altinn:resource:brg-migratedcorrespondence-3773-1","urn:altinn:resource:brg-migratedcorrespondence-3800-1","urn:altinn:resource:brg-migratedcorrespondence-4070-1","urn:altinn:resource:brg-migratedcorrespondence-4071-1","urn:altinn:resource:brg-migratedcorrespondence-4293-1","urn:altinn:resource:brg-migratedcorrespondence-4495-1","urn:altinn:resource:brg-migratedcorrespondence-6009-1","urn:altinn:resource:dagl-correspondence","urn:altinn:resource:dagl-correspondence-0","urn:altinn:resource:dagl-correspondence-1","urn:altinn:resource:dagl-correspondence-10","urn:altinn:resource:dagl-correspondence-2","urn:altinn:resource:dagl-correspondence-3","urn:altinn:resource:dagl-correspondence-4","urn:altinn:resource:dagl-correspondence-5","urn:altinn:resource:dagl-correspondence-6","urn:altinn:resource:dagl-correspondence-7","urn:altinn:resource:dagl-correspondence-8","urn:altinn:resource:dagl-correspondence-9","urn:altinn:resource:dfo-migratedcorrespondence-4351-1","urn:altinn:resource:dfo-migratedcorrespondence-4352-2","urn:altinn:resource:fors-migratedcorrespondence-5075-1","urn:altinn:resource:fors-migratedcorrespondence-5133-1","urn:altinn:resource:kmd-migratedcorrespondence-3873-201409","urn:altinn:resource:mdir-migratedcorrespondence-5226-1","urn:altinn:resource:medl-correspondence-1","urn:altinn:resource:nav-migratedcorrespondence-4503-2","urn:altinn:resource:nav-migratedcorrespondence-4751-1","urn:altinn:resource:nav-migratedcorrespondence-5062-1","urn:altinn:resource:nav-migratedcorrespondence-5278-1","urn:altinn:resource:nav-migratedcorrespondence-5516-1","urn:altinn:resource:nav-migratedcorrespondence-5516-2","urn:altinn:resource:nav-migratedcorrespondence-5516-3","urn:altinn:resource:nav-migratedcorrespondence-5516-4","urn:altinn:resource:nav-migratedcorrespondence-5516-5","urn:altinn:resource:nav-migratedcorrespondence-5793-1","urn:altinn:resource:pod-migratedcorrespondence-4737-1","urn:altinn:resource:pod-migratedcorrespondence-5482-1","urn:altinn:resource:pod-migratedcorrespondence-5484-1","urn:altinn:resource:pod-migratedcorrespondence-5485-1","urn:altinn:resource:pod-migratedcorrespondence-5486-1","urn:altinn:resource:skd-migratedcorrespondence-2506-110222","urn:altinn:resource:skd-migratedcorrespondence-2772-110914","urn:altinn:resource:skd-migratedcorrespondence-2772-120301","urn:altinn:resource:skd-migratedcorrespondence-2772-120309","urn:altinn:resource:skd-migratedcorrespondence-2772-140313","urn:altinn:resource:skd-migratedcorrespondence-2929-111209","urn:altinn:resource:skd-migratedcorrespondence-2969-120112","urn:altinn:resource:skd-migratedcorrespondence-3102-120420","urn:altinn:resource:skd-migratedcorrespondence-3665-131016","urn:altinn:resource:skd-migratedcorrespondence-4123-150602","urn:altinn:resource:skd-migratedcorrespondence-5138-180522","urn:altinn:resource:skd-migratedcorrespondence-5179-180822","urn:altinn:resource:skd-migratedcorrespondence-5227-1","urn:altinn:resource:skd-migratedcorrespondence-5228-1","urn:altinn:resource:skd-migratedcorrespondence-5229-1","urn:altinn:resource:skd-migratedcorrespondence-5355-190607","urn:altinn:resource:skd-migratedcorrespondence-5356-190607","urn:altinn:resource:skd-migratedcorrespondence-5357-190607","urn:altinn:resource:skd-migratedcorrespondence-5488-191121","urn:altinn:resource:skd-migratedcorrespondence-5601-2020","urn:altinn:resource:skd-migratedcorrespondence-5651-1","urn:altinn:resource:skd-migratedcorrespondence-5760-211028","urn:altinn:resource:skd-migratedcorrespondence-5761-211028","urn:altinn:resource:skd-migratedcorrespondence-5764-211028","urn:altinn:resource:skd-migratedcorrespondence-5766-211028","urn:altinn:resource:skd-migratedcorrespondence-5854-220907","urn:altinn:resource:skd-migratedcorrespondence-5855-220907","urn:altinn:resource:skd-migratedcorrespondence-skdpsa-2","urn:altinn:resource:skd-migratedcorrespondence-skfskb-1","urn:altinn:resource:super-simple-service","urn:altinn:resource:svv-migratedcorrespondence-4310-201511","urn:altinn:resource:svv-migratedcorrespondence-4316-201512","urn:altinn:resource:svv-migratedcorrespondence-4319-201512","urn:altinn:resource:svv-migratedcorrespondence-4320-201512","urn:altinn:resource:svv-migratedcorrespondence-4321-201512","urn:altinn:resource:svv-migratedcorrespondence-4322-201512","urn:altinn:resource:svv-migratedcorrespondence-4921-201704","urn:altinn:resource:svv-migratedcorrespondence-5230-201801","urn:altinn:resource:svv-migratedcorrespondence-5230-201802","urn:altinn:resource:ttd-altinn-events-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests-2","urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence","urn:altinn:resource:ttd-dialogporten-performance-test-01","urn:altinn:resource:ttd-dialogporten-performance-test-02","urn:altinn:resource:ttd-dialogporten-performance-test-03","urn:altinn:resource:ttd-dialogporten-performance-test-04","urn:altinn:resource:ttd-dialogporten-performance-test-05","urn:altinn:resource:ttd-dialogporten-performance-test-06","urn:altinn:resource:ttd-dialogporten-performance-test-07","urn:altinn:resource:ttd-dialogporten-performance-test-08","urn:altinn:resource:ttd-dialogporten-performance-test-09","urn:altinn:resource:ttd-dialogporten-performance-test-10"]}]'
             '[{"parties":["urn:altinn:person:identifier-no:06917699338","urn:altinn:person:identifier-no:09886998144","urn:altinn:person:identifier-no:29875597734","urn:altinn:person:identifier-no:01857396517","urn:altinn:person:identifier-no:18867397910","urn:altinn:person:identifier-no:28928397302","urn:altinn:person:identifier-no:02927997919","urn:altinn:person:identifier-no:20824699322","urn:altinn:person:identifier-no:21904199173","urn:altinn:person:identifier-no:04885799588","urn:altinn:person:identifier-no:07907399324","urn:altinn:person:identifier-no:02916298334","urn:altinn:person:identifier-no:14916099488","urn:altinn:person:identifier-no:10837099310","urn:altinn:person:identifier-no:05906599602","urn:altinn:person:identifier-no:03905398104","urn:altinn:person:identifier-no:22876598926","urn:altinn:person:identifier-no:04915199904","urn:altinn:person:identifier-no:05914998583","urn:altinn:person:identifier-no:07826998711"],"services":["urn:altinn:resource:altinn-profil-api-varslingsdaresser-for-virksomheter","urn:altinn:resource:altinn_keyrole_access","urn:altinn:resource:asf-migratedcorrespondence-3996-201503","urn:altinn:resource:asf-migratedcorrespondence-4786-1","urn:altinn:resource:asf-migratedcorrespondence-5132-1","urn:altinn:resource:brg-migratedcorrespondence-3770-1","urn:altinn:resource:brg-migratedcorrespondence-3771-1","urn:altinn:resource:brg-migratedcorrespondence-3772-1","urn:altinn:resource:brg-migratedcorrespondence-3773-1","urn:altinn:resource:brg-migratedcorrespondence-3800-1","urn:altinn:resource:brg-migratedcorrespondence-4070-1","urn:altinn:resource:brg-migratedcorrespondence-4071-1","urn:altinn:resource:brg-migratedcorrespondence-4293-1","urn:altinn:resource:brg-migratedcorrespondence-4495-1","urn:altinn:resource:brg-migratedcorrespondence-6009-1","urn:altinn:resource:dagl-correspondence","urn:altinn:resource:dagl-correspondence-0","urn:altinn:resource:dagl-correspondence-1","urn:altinn:resource:dagl-correspondence-10","urn:altinn:resource:dagl-correspondence-2","urn:altinn:resource:dagl-correspondence-3","urn:altinn:resource:dagl-correspondence-4","urn:altinn:resource:dagl-correspondence-5","urn:altinn:resource:dagl-correspondence-6","urn:altinn:resource:dagl-correspondence-7","urn:altinn:resource:dagl-correspondence-8","urn:altinn:resource:dagl-correspondence-9","urn:altinn:resource:dfo-migratedcorrespondence-4351-1","urn:altinn:resource:dfo-migratedcorrespondence-4352-2","urn:altinn:resource:fors-migratedcorrespondence-5075-1","urn:altinn:resource:fors-migratedcorrespondence-5133-1","urn:altinn:resource:kmd-migratedcorrespondence-3873-201409","urn:altinn:resource:mdir-migratedcorrespondence-5226-1","urn:altinn:resource:medl-correspondence-1","urn:altinn:resource:nav-migratedcorrespondence-4503-2","urn:altinn:resource:nav-migratedcorrespondence-4751-1","urn:altinn:resource:nav-migratedcorrespondence-5062-1","urn:altinn:resource:nav-migratedcorrespondence-5278-1","urn:altinn:resource:nav-migratedcorrespondence-5516-1","urn:altinn:resource:nav-migratedcorrespondence-5516-2","urn:altinn:resource:nav-migratedcorrespondence-5516-3","urn:altinn:resource:nav-migratedcorrespondence-5516-4","urn:altinn:resource:nav-migratedcorrespondence-5516-5","urn:altinn:resource:nav-migratedcorrespondence-5793-1","urn:altinn:resource:pod-migratedcorrespondence-4737-1","urn:altinn:resource:pod-migratedcorrespondence-5482-1","urn:altinn:resource:pod-migratedcorrespondence-5484-1","urn:altinn:resource:pod-migratedcorrespondence-5485-1","urn:altinn:resource:pod-migratedcorrespondence-5486-1","urn:altinn:resource:skd-migratedcorrespondence-2506-110222","urn:altinn:resource:skd-migratedcorrespondence-2772-110914","urn:altinn:resource:skd-migratedcorrespondence-2772-120301","urn:altinn:resource:skd-migratedcorrespondence-2772-120309","urn:altinn:resource:skd-migratedcorrespondence-2772-140313","urn:altinn:resource:skd-migratedcorrespondence-2929-111209","urn:altinn:resource:skd-migratedcorrespondence-2969-120112","urn:altinn:resource:skd-migratedcorrespondence-3102-120420","urn:altinn:resource:skd-migratedcorrespondence-3665-131016","urn:altinn:resource:skd-migratedcorrespondence-4123-150602","urn:altinn:resource:skd-migratedcorrespondence-5138-180522","urn:altinn:resource:skd-migratedcorrespondence-5179-180822","urn:altinn:resource:skd-migratedcorrespondence-5227-1","urn:altinn:resource:skd-migratedcorrespondence-5228-1","urn:altinn:resource:skd-migratedcorrespondence-5229-1","urn:altinn:resource:skd-migratedcorrespondence-5355-190607","urn:altinn:resource:skd-migratedcorrespondence-5356-190607","urn:altinn:resource:skd-migratedcorrespondence-5357-190607","urn:altinn:resource:skd-migratedcorrespondence-5488-191121","urn:altinn:resource:skd-migratedcorrespondence-5601-2020","urn:altinn:resource:skd-migratedcorrespondence-5651-1","urn:altinn:resource:skd-migratedcorrespondence-5760-211028","urn:altinn:resource:skd-migratedcorrespondence-5761-211028","urn:altinn:resource:skd-migratedcorrespondence-5764-211028","urn:altinn:resource:skd-migratedcorrespondence-5766-211028","urn:altinn:resource:skd-migratedcorrespondence-5854-220907","urn:altinn:resource:skd-migratedcorrespondence-5855-220907","urn:altinn:resource:skd-migratedcorrespondence-skdpsa-2","urn:altinn:resource:skd-migratedcorrespondence-skfskb-1","urn:altinn:resource:super-simple-service","urn:altinn:resource:svv-migratedcorrespondence-4310-201511","urn:altinn:resource:svv-migratedcorrespondence-4316-201512","urn:altinn:resource:svv-migratedcorrespondence-4319-201512","urn:altinn:resource:svv-migratedcorrespondence-4320-201512","urn:altinn:resource:svv-migratedcorrespondence-4321-201512","urn:altinn:resource:svv-migratedcorrespondence-4322-201512","urn:altinn:resource:svv-migratedcorrespondence-4921-201704","urn:altinn:resource:svv-migratedcorrespondence-5230-201801","urn:altinn:resource:svv-migratedcorrespondence-5230-201802","urn:altinn:resource:ttd-altinn-events-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests","urn:altinn:resource:ttd-dialogporten-automated-tests-2","urn:altinn:resource:ttd-dialogporten-automated-tests-correspondence","urn:altinn:resource:ttd-dialogporten-performance-test-01","urn:altinn:resource:ttd-dialogporten-performance-test-02","urn:altinn:resource:ttd-dialogporten-performance-test-03","urn:altinn:resource:ttd-dialogporten-performance-test-04","urn:altinn:resource:ttd-dialogporten-performance-test-05","urn:altinn:resource:ttd-dialogporten-performance-test-06","urn:altinn:resource:ttd-dialogporten-performance-test-07","urn:altinn:resource:ttd-dialogporten-performance-test-08","urn:altinn:resource:ttd-dialogporten-performance-test-09","urn:altinn:resource:ttd-dialogporten-performance-test-10"]}]'
         ::jsonb
         ) AS x(parties text[], services text[])
    CROSS JOIN LATERAL unnest(x.services) AS s(service)
    CROSS JOIN LATERAL unnest(x.parties) AS p(party)
)
    ,accessibleDialogs AS (
    SELECT DISTINCT d."Id"
    FROM "Dialog" d
    INNER JOIN partyResourceAccess c
    ON d."ServiceResource" = c.service AND d."Party" = c.party
)
   ,systemLabelsByDialog AS (
    SELECT d."Id", array_agg(dsl."SystemLabelId") labels
    FROM  input, "Dialog" d
    INNER JOIN "DialogEndUserContext" dec on d."Id" = dec."DialogId"
    INNER JOIN "DialogEndUserContextSystemLabel" dsl on dsl."DialogEndUserContextId" = dec."Id"
    WHERE input.systemLabel is not null AND dsl."SystemLabelId" = ANY(input.systemLabel)
    GROUP BY d."Id"
)
SELECT d."Id", d."Org"
FROM input, "Dialog" d
INNER JOIN accessibleDialogs a ON d."Id" = a."Id"
LEFT JOIN "DialogSearch" ds ON ds."DialogId" = d."Id"
LEFT JOIN "DialogSearchTag" dst on d."Id" = dst."DialogId"
LEFT JOIN systemLabelsByDialog l on l."Id" = d."Id"
WHERE d."Deleted" = false
  AND (d."VisibleFrom" IS NULL or d."VisibleFrom" < input.now)
  AND (d."ExpiresAt" IS NULL or d."ExpiresAt" > input.now)
  AND (input.org IS NULL OR d."Org" = ANY(input.org))
  AND (input.serviceResource IS NULL OR d."ServiceResource" = ANY(input.serviceResource))
  AND (input.party IS NULL OR d."Party" = ANY(input.party))
  AND (input.extendedStatus IS NULL OR d."ExtendedStatus" = ANY(input.extendedStatus))
  AND (input.externalReference IS NULL OR d."ExternalReference" = input.externalReference)
  AND (input.status IS NULL OR d."StatusId" = ANY(input.status))
  AND (input.createdAfter IS NULL OR input.createdAfter <= d."CreatedAt")
  AND (input.createdBefore IS NULL OR d."CreatedAt" <= input.createdBefore)
  AND (input.updatedAfter IS NULL OR input.updatedAfter <= d."UpdatedAt")
  AND (input.updatedBefore IS NULL OR d."UpdatedAt" <= input.updatedBefore)
  AND (input.contentUpdatedAfter IS NULL OR input.contentUpdatedAfter <= d."ContentUpdatedAt")
  AND (input.contentUpdatedBefore IS NULL OR d."ContentUpdatedAt" <= input.contentUpdatedBefore)
  AND (input.dueAfter IS NULL OR  input.dueAfter <= d."DueAt")
  AND (input.dueBefore IS NULL OR d."DueAt" <= input.dueBefore)
  AND (input.process IS NULL OR d."Process" = input.process) -- It's ILike in the code - is that correct?
  AND (input.excludeApiOnly IS NULL OR input.excludeApiOnly = false OR input.excludeApiOnly = true AND d."IsApiOnly" = false)
  AND (input.search IS NULL OR ds."SearchValue" ILIKE '%' || input.search || '%' OR dst."Value" = input.search)
  AND (input.systemLabel IS NULL OR input.systemLabel <@ l.labels);
-- TODO: Add pagination parameters
-- TODO: Can we consider minimum auth level here as well?



select * from "DialogSearch" where "DialogId" ='00faafbf-f166-7f66-9467-6a0e2d3a4a64'



SELECT "Party", count(*)
FROM "Dialog"
--WHERE "Party" like 'urn:altinn:person:identifier-no:%'
GROUP BY "Party"
HAVING count(*) > 50
ORDER BY count(*) desc
limit 20
