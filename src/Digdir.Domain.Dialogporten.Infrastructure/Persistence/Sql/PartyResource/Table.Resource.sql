CREATE TABLE partyresource."Resource"
(
    "Id" integer GENERATED ALWAYS AS IDENTITY,
    "UnprefixedResourceIdentifier" text NOT NULL,

    CONSTRAINT "PK_Resource" PRIMARY KEY ("Id"),
    CONSTRAINT "UX_Resource_UnprefixedResourceIdentifier" UNIQUE ("UnprefixedResourceIdentifier")
);
