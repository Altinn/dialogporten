SELECT * FROM unnest(to_tsvector('english',
                                 'I''m going to make him an offer he can''t refuse. Refusing is not an option.'));


SELECT * FROM unnest(setweight(to_tsvector('norwegian',
                                           'Jeg skal gi han et tilbud han ikke kan avslå. Å avslå er ikke et alternativ.'), 'C'));

SELECT to_tsvector('norwegian',
                   'Jeg skal gi han et tilbud han ikke kan avslå. Å avslå er ikke et alternativ.');
SELECT levenshtein_less_equal('pdfa', 'pregant', 2);

SELECT cfgname FROM pg_ts_config;

SELECT websearch_to_tsquery('english', 'the darth vader');

SELECT websearch_to_tsquery('english', 'darth vader') @@
       to_tsvector('english',
                   'Darth Vader is my father.');




ALTER TABLE movies ADD search tsvector GENERATED ALWAYS AS
    (setweight(to_tsvector('english', Title), 'A') || ' ' ||
     to_tsvector('english', Plot) || ' ' ||
     to_tsvector('simple', Director) || ' ' ||
     to_tsvector('simple', Genre) || ' ' ||
     to_tsvector('simple', Origin) || ' ' ||
     to_tsvector('simple', Casting)
    ) STORED;


SELECT title, ts_rank(search, websearch_to_tsquery('english', 'jedi')) rank
FROM movies
WHERE search @@ websearch_to_tsquery('english','jedi')
ORDER BY rank DESC;

CREATE INDEX pgweb_idx ON pgweb
    USING GIN (to_tsvector('english', body))
    WHERE languageCode = 'en';

CREATE INDEX searchvalue_idx ON "DialogSearch"
    USING GIN (to_tsvector('norwegian', "SearchValue"))
    WITH (fastupdate = off);

CREATE TABLE "DialogSearch" (
                                "DialogId" UUID,
                                "SearchValue" TEXT
);

ALTER TABLE "DialogSearch" ADD CONSTRAINT "DialogSearch_PK" PRIMARY KEY ("DialogId");

CREATE INDEX searchvalue_idx ON "DialogSearch" USING GIN (to_tsvector('norwegian', "SearchValue"));



SELECT "DialogId", "SearchValue"
FROM "DialogSearch"
WHERE to_tsvector('norwegian', "SearchValue") @@ websearch_to_tsquery('norwegian', '"åkerhøne målesystem"')
Limit 10;

with cte AS (
    SELECT "DialogId"
    FROM "DialogSearch"
    WHERE to_tsvector('norwegian', "SearchValue") @@ websearch_to_tsquery('på')
--         OR to_tsvector('norwegian', "SearchValue") @@ websearch_to_tsquery('english', 'arbeid')
--     UNION SELECT "DialogId"
--     FROM "DialogSearch"
--     WHERE to_tsvector('norwegian', "SearchValue") @@ websearch_to_tsquery('arbeid')
)
SELECT *
FROM cte
LIMIT 100;


SELECT to_tsvector('sirle å åkerhøne målesystem murbygning industriland ikkje-fornybar orgelpipe fostbrorskap berggrunn dagstid klubbmeister noe sæde å andre hans kunne kompensere kyssing hovudavdeling gravar nå slike dokumentfals reinferdig blomeplante sagblad sluttmerknad handelslovgiving blindtarmsbetennelse vært skulle eukalyptol flugefangar bulevardavis campingbil girspak milliarddels hagesnigel konsul skittvatn perige opp fagpresse innvarsle basme dusør underbreisle bogekorps vele kollektivtilbod hadde ulenkeleg selv kammerladingsgevær paranoikar flask kjellarleilegheit tyrikubbe godnamn rett-tenkande telefonoppringing underliggar water hore rekkjetal gabardinbukse omnssverte villstyring kleppete sa sammen detonator på opphevjing raljere vaktbåt mutar skjørheit ha hvor går rettvise høgtidsam ovalys kvinnfolk man lommeslagskip steppe kreditor tåresekk terte puselanke antikrympebehandle solhylle tviband sidesaum når syltefor to snøstorm dem varenett må når kopparrete delløysing alt avlatskremmar skogland rusthandsame pirre riesling garp hóvdyr ingen eineståande låse ham tilstundande stupflyging skjemtedikt stikkontakt mot maisåker musisk gåvebok byggekonstruksjon fotskammel sentralafrikansk vaskarklut kake skittord astigmatisme systue over tagetes avfallsplass terapi dublungnavar hugsår vêrhane kryptisk balsamgran soldathue frita eksodus idésystem polypp trygdingslag sjelsevne ersia-morvinsk eftarøde ville typografi attentatforsøk bli kløktig breislejente varmestråling dralon i overskjønn haldnad stemningsbølgje galehus tvare prege klone ålmente barnepensjon chassis rigorisme filmografi kvinand synkronistisk fråstandsmålar skolebygg sauegjødsel gebet johannesbrødtre vanhug konsepsjon hudkreft yppast skyggelue hun småsparar at standard plattform innerst variabilitet ugilde aksessorisk skal hokjønnsselle dødpunkt fotostatkopiering opp hovudtillitsvald lesebrille utpantar selv svikthopp sin kvinnesterk veiter korsblomfamilie bergland slynge ynskjemål skattelette strategikar inn ferdselsveg kunne fråstolen havannasigar som det bølgepapp der må')
           @@ websearch_to_tsquery('avlatskremmar');

SELECT *
FROM unnest(to_tsvector('english', 'sirle å åkerhøne målesystem murbygning industriland ikkje-fornybar orgelpipe fostbrorskap berggrunn dagstid klubbmeister noe sæde å andre hans kunne kompensere kyssing hovudavdeling gravar nå slike dokumentfals reinferdig blomeplante sagblad sluttmerknad handelslovgiving blindtarmsbetennelse vært skulle eukalyptol flugefangar bulevardavis campingbil girspak milliarddels hagesnigel konsul skittvatn perige opp fagpresse innvarsle basme dusør underbreisle bogekorps vele kollektivtilbod hadde ulenkeleg selv kammerladingsgevær paranoikar flask kjellarleilegheit tyrikubbe godnamn rett-tenkande telefonoppringing underliggar water hore rekkjetal gabardinbukse omnssverte villstyring kleppete sa sammen detonator på opphevjing raljere vaktbåt mutar skjørheit ha hvor går rettvise høgtidsam ovalys kvinnfolk man lommeslagskip steppe kreditor tåresekk terte puselanke antikrympebehandle solhylle tviband sidesaum når syltefor to snøstorm dem varenett må når kopparrete delløysing alt avlatskremmar skogland rusthandsame pirre riesling garp hóvdyr ingen eineståande låse ham tilstundande stupflyging skjemtedikt stikkontakt mot maisåker musisk gåvebok byggekonstruksjon fotskammel sentralafrikansk vaskarklut kake skittord astigmatisme systue over tagetes avfallsplass terapi dublungnavar hugsår vêrhane kryptisk balsamgran soldathue frita eksodus idésystem polypp trygdingslag sjelsevne ersia-morvinsk eftarøde ville typografi attentatforsøk bli kløktig breislejente varmestråling dralon i overskjønn haldnad stemningsbølgje galehus tvare prege klone ålmente barnepensjon chassis rigorisme filmografi kvinand synkronistisk fråstandsmålar skolebygg sauegjødsel gebet johannesbrødtre vanhug konsepsjon hudkreft yppast skyggelue hun småsparar at standard plattform innerst variabilitet ugilde aksessorisk skal hokjønnsselle dødpunkt fotostatkopiering opp hovudtillitsvald lesebrille utpantar selv svikthopp sin kvinnesterk veiter korsblomfamilie bergland slynge ynskjemål skattelette strategikar inn ferdselsveg kunne fråstolen havannasigar som det bølgepapp der må'))

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


