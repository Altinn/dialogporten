CREATE TABLE public."PartyResource"
(
    "PartyId" integer NOT NULL,
    "ResourceId" integer NOT NULL,

    CONSTRAINT "PK_PartyResource" PRIMARY KEY ("PartyId", "ResourceId"),
    CONSTRAINT "FK_PartyResource_Party_PartyId"
        FOREIGN KEY ("PartyId") REFERENCES public."Party" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PartyResource_Resource_ResourceId"
        FOREIGN KEY ("ResourceId") REFERENCES public."Resource" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PartyResource_ResourceId_PartyId"
    ON public."PartyResource" ("ResourceId", "PartyId");
