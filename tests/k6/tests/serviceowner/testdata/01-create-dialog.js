import { uuidv4 } from '../../../common/testimports.js'
import { getDefaultEnduserSsn } from "../../../common/token.js";
import { sentinelValue, defaultServiceOwnerOrgNo } from "../../../common/config.js";

export default function (endUser = getDefaultEnduserSsn()) {
    return {
        "serviceResource": "urn:altinn:resource:ttd-dialogporten-automated-tests", // urn starting with urn:altinn:resource:
        "party": "urn:altinn:person:identifier-no:" + endUser, // or urn:altinn:organization:identifier-no:<9 digits>
        "status": "notApplicable", // valid values: notApplicable, inprogress, draft, awaiting, equiresAttention, completed
        "extendedStatus": "urn:any/valid/uri",
        "dueAt": "2033-11-25T06:37:54.2920190Z", // must be UTC
        "expiresAt": "2053-11-25T06:37:54.2920190Z", // must be UTC
        "visibleFrom": "2032-11-25T06:37:54.2920190Z", // must be UTC
        "process": "urn:test:process:1",
        "serviceOwnerContext": {
            "serviceOwnerLabels": []
        },
        "searchTags": [
            { "value": "something searchable" },
            { "value": "something else searchable" },
            { "value": sentinelValue } // Do not remove this, this is used to look for unpurged dialogs after a run
        ],
        "content": {
            "Title": {
                "value": [{ "languageCode": "nb", "value": "Skjema for rapportering av et eller annet" }]
            },
            "SenderName": {
                "value": [{ "languageCode": "nb", "value": "Avsendernavn" }]
            },
            "Summary": {
                "value": [{ "languageCode": "nb", "value": "Et sammendrag her. Maks 200 tegn, ingen HTML-støtte. Påkrevd. Vises i liste." }]
            },
            "AdditionalInfo": {
                "mediaType": "text/plain",
                "value": [{ "languageCode": "nb", "value": "Utvidet forklaring (enkel HTML-støtte, inntil 1023 tegn). Ikke påkrevd. Vises kun i detaljvisning." }]
            },
            "ExtendedStatus": {
                "value": [{ "languageCode": "nb", "value": "Utvidet Status" }]
            },
        },
        "transmissions": [
            {
                "type": "Information",
                "authorizationAttribute": "element1",
                "sender": {
                    "actorType": "serviceOwner",
                },
                "attachments": [
                    {
                        "displayName": [
                            {
                                "languageCode": "nb",
                                "value": "Forsendelse visningsnavn"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission attachment display name"
                            }
                        ],
                        "urls": [
                            {
                                "url": "https://digdir.apps.tt02.altinn.no/some-other-url",
                                "consumerType": "Gui"
                            }
                        ]
                    }
                ],
                "content": {
                    "title": {
                        "value": [
                            {
                                "languageCode": "nb",
                                "value": "Forsendelsestittel"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission title"
                            }
                        ]
                    },
                    "summary": {
                        "value": [
                            {
                                "languageCode": "nb",
                                "value": "Forsendelse oppsummering"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission summary"
                            }
                        ]
                    }
                }
            },
            {
                "type": "Information",
                "sender": {
                    "actorType": "serviceOwner"
                },
                "attachments": [
                    {
                        "displayName": [
                            {
                                "languageCode": "nb",
                                "value": "Visningsnavn for forsendelsesvedlegg "
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission attachment display name"
                            }
                        ],
                        "urls": [
                            {
                                "url": "https://digdir.apps.tt02.altinn.no/some-other-url",
                                "consumerType": "Gui"
                            }
                        ]
                    }
                ],
                "content": {
                    "title": {
                        "value": [
                            {
                                "languageCode": "nb",
                                "value": "Forsendelsesstittel"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission title"
                            }
                        ]
                    },
                    "summary": {
                        "value": [
                            {
                                "languageCode": "nb",
                                "value": "Transmisjon oppsummering"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission summary"
                            }
                        ]
                    }
                }
            },
            {
                "type": "Information",
                "authorizationAttribute": "elementius",
                "sender": {
                    "actorType": "serviceOwner"
                },
                "attachments": [
                    {
                        "displayName": [
                            {
                                "languageCode": "nb",
                                "value": "Visningsnavn for forsendelsesvedlegg"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission attachment display name"
                            }
                        ],
                        "urls": [
                            {
                                "url": "https://digdir.apps.tt02.altinn.no/some-other-url",
                                "consumerType": "Gui"
                            }
                        ]
                    }
                ],
                "content": {
                    "title": {
                        "value": [
                            {
                                "languageCode": "nb",
                                "value": "Forsendelsetittel"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission title"
                            }
                        ]
                    },
                    "summary": {
                        "value": [
                            {
                                "languageCode": "nb",
                                "value": "Forsendelsesoppsummering"
                            },
                            {
                                "languageCode": "en",
                                "value": "Transmission summary"
                            }
                        ]
                    }
                }
            }
        ],
        "guiActions": [
            {
                "action": "read",
                "url": "https://digdir.no",
                "priority": "Primary",
                "title": [
                    {
                        "value": "Gå til dialog",
                        "languageCode": "nb"
                    }
                ]
            },
            {
                "action": "read",
                "url": "https://digdir.no",
                "priority": "Secondary",
                "httpMethod": "POST",
                "title": [
                    {
                        "value": "Utfør handling uten navigasjon",
                        "languageCode": "nb"
                    }
                ],
                "prompt": [{ "value": "Er du sikker?", "languageCode": "nb" }]
            }
        ],
        "apiActions": [
            {
                "action": "some_unauthorized_action",
                "name": "confirm",
                "endPoints": [
                    {
                        "url": "https://digdir.no",
                        "httpMethod": "GET"
                    },
                    {
                        "url": "https://digdir.no/deprecated",
                        "httpMethod": "GET",
                        "deprecated": true
                    }
                ]
            }
        ],
        "attachments": [
            {
                "displayName": [
                    {
                        "languageCode": "nb",
                        "value": "Et vedlegg"
                    }
                ],
                "urls": [
                    {
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mediaType": "application/pdf"
                    }
                ]
            },
            {
                "displayName": [
                    {
                        "languageCode": "nb",
                        "value": "Et annet vedlegg"
                    }
                ],
                "urls": [
                    {
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mediaType": "application/pdf"
                    }
                ]
            },
            {
                "displayName": [
                    {
                        "languageCode": "nb",
                        "value": "Nok et vedlegg"
                    }
                ],
                "urls": [
                    {
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mediaType": "application/pdf"
                    }
                ]
            }
        ],
        "activities": [
            {
                "type": "DialogCreated",
                "performedBy": {
                    "actorType": "partyRepresentative",
                    "actorName": "Some custom name"
                }
            },
            {
                "type": "PaymentMade",
                "performedBy": {
                    "actorType": "serviceOwner"
                }
            },
            {
                "type": "Information",
                "performedBy": {
                    "actorType": "partyRepresentative",
                    "actorId": "urn:altinn:organization:identifier-no:" + defaultServiceOwnerOrgNo
                },
                "description": [
                    {
                        "value": "Brukeren har begått skattesvindel",
                        "languageCode": "nb"
                    },
                    {
                        "value": "Tax fraud",
                        "languageCode": "en"
                    }
                ]
            }
        ]
    };
};
