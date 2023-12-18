import { uuidv4 } from '../../../common/testimports.js'

export default function () {
    let dialogElementId = uuidv4();

    // Note! We assume that "super-simple-service" exists and is owned by 991825827
    return {
        "serviceResource": "urn:altinn:resource:super-simple-service", // urn starting with urn:altinn:resource:
        "party": "/org/991825827", // or /person/<11 digits>
        "status": "unspecified", // valid values: unspecified, inprogress, waiting, signing, cancelled, completed
        "extendedStatus": "urn:any/valid/uri",
        "dueAt": "2033-11-25T06:37:54.2920190Z", // must be UTC
        "expiresAt": "2053-11-25T06:37:54.2920190Z", // must be UTC
        "visibleFrom": "2032-11-25T06:37:54.2920190Z", // must be UTC
        "searchTags": [ 
            { "value": "something searchable" },
            { "value": "something else searchable" }
        ],
        "content": [
            { "type": "Title", "value": [ { "cultureCode": "nb_NO", "value": "Skjema for rapportering av et eller annet" } ] },
            { "type": "SenderName", "value": [ { "cultureCode": "nb_NO", "value": "Avsendernavn" } ] },
            { "type": "Summary", "value": [ { "cultureCode": "nb_NO", "value": "Et sammendrag her. Maks 200 tegn, ingen HTML-støtte. Påkrevd. Vises i liste." } ] },
            { "type": "AdditionalInfo", "value": [ { "cultureCode": "nb_NO", "value": "Utvidet forklaring (enkel HTML-støtte, inntil 1023 tegn). Ikke påkrevd. Vises kun i detaljvisning." } ] }
        ],
        "apiActions": [
            {
                "action": "some:action",
                "dialogElementId": dialogElementId,
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
        "elements": [
            {
                "id": dialogElementId,
                "type": "some:type",
                "displayName": [
                    {
                        "cultureCode": "nb_NO",
                        "value": "Et vedlegg"
                    }
                ],
                "urls": [
                    {
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mimeType": "application/pdf"
                    }
                ]
            },
            {
                "relatedDialogElementId": dialogElementId,
                "type": "some:type",
                "displayName": [
                    {
                        "cultureCode": "nb_NO",
                        "value": "Et annet vedlegg"
                    }
                ],
                "urls": [
                    {
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mimeType": "application/pdf"
                    }
                ]
            },
            {
                "type": "some:type",
                "displayName": [
                    {
                        "cultureCode": "nb_NO",
                        "value": "Nok et vedlegg"
                    }
                ],
                "urls": [
                    {
                        "consumerType": "gui",
                        "url": "https://foo.com/foo.pdf",
                        "mimeType": "application/pdf"
                    }
                ]
            }
        ],
        "activities": [
            {
                "type": "Error",
                "dialogElementId": dialogElementId,
                "performedBy": [
                    {
                        "value": "Politiet",
                        "cultureCode":"nb_no"
                    },
                    {
                        "value": "La policia",
                        "cultureCode":"es_es"
                    }
                ],
                "description": [
                    {
                        "value": "Lovbrudd",
                        "cultureCode": "nb_no"
                    },
                    {
                        "value": "Ofensa",
                        "cultureCode": "es_es"
                    }
                ]
            },
            {
                "type": "Submission",
                "performedBy": [
                    {
                        "value": "NAV",
                        "cultureCode": "nb_no"
                    }
                ],
                "description": [
                    {
                        "value": "Brukeren har levert et viktig dokument.",
                        "cultureCode": "nb_no"
                    }
                ]
            },
            {
                "type": "Submission",
                "performedBy": [
                    {
                        "value": "Skatteetaten",
                        "cultureCode": "nb_no"
                    },
                    {
                        "value": "IRS",
                        "cultureCode": "en_us"
                    }
                ],
                "description": [
                    {
                        "value": "Brukeren har begått skattesvindel",
                        "cultureCode": "nb_no"
                    },
                    {
                        "value": "Tax fraud",
                        "cultureCode": "en_us"
                    }
                ]
            }
        ]
    };
};