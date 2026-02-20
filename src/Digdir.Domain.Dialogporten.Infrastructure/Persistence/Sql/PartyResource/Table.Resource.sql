CREATE TABLE public."Resource"
(
    "Id" integer GENERATED ALWAYS AS IDENTITY,
    "Identifier" text NOT NULL,

    CONSTRAINT "PK_Resource" PRIMARY KEY ("Id"),
    CONSTRAINT "UX_Resource_Identifier" UNIQUE ("Identifier")
);
