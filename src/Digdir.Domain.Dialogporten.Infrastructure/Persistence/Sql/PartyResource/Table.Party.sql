CREATE TABLE public."Party"
(
    "Id" integer GENERATED ALWAYS AS IDENTITY,
    "ShortPrefix" char(1) NOT NULL,
    "Identifier" text NOT NULL,

    CONSTRAINT "PK_Party" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Party_ShortPrefix" CHECK ("ShortPrefix" IN ('o', 'p', 's', 'e', 'i', 'f')),
    CONSTRAINT "UX_Party_ShortPrefix_Identifier" UNIQUE ("ShortPrefix", "Identifier")
);
