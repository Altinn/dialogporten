
import { uuidv7 } from "../../common/uuid.js";

export default function (relatedTransmissionId) {
    let transmission = {
        "id": uuidv7(),
        "createdAt": new Date().toISOString(),
        "authorizationAttribute": "element1",
        "extendedType": "string",
        "type": "Information",
        "sender": {
            "actorType": "serviceOwner"
        },
        "content": {
            "title": {
                "value": [
                    {
                        "value": "Forsendelsestittel",
                        "languageCode": "nb"
                    },
                    {
                        "languageCode": "en",
                        "value": "Transmission title"
                    }
                ],
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
                ],
            },
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
        ]
    }
    if (relatedTransmissionId != 0) {
        transmission.relatedTransmissionId = relatedTransmissionId;
    }
    return transmission;
}

export function transmissionToInsertSkd(relatedTransmissionId, orgNo) {
  let transmission = {
      "id": uuidv7(),
      "createdAt": new Date().toISOString(),
      "authorizationAttribute": "element1",
      "extendedType": "string",
      "type": "Information",
      "sender": {
          "actorType": "partyRepresentative",
          "actorId": "urn:altinn:organization:identifier-no:" + orgNo,
      },
      "content": {
          "title": {
              "value": [
                  {
                      "value": "Forsendelsestittel",
                      "languageCode": "nb"
                  },
                  {
                      "languageCode": "en",
                      "value": "Transmission title"
                  }
              ],
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
              ],
          },
          "contentReference": {
              "value": [
                  {
                      "value": "https://skatteetaten.no/api/dialogarkiv/612537",
                      "languageCode": "nb"
                  }
              ],
              "mediaType": "application/vnd.dialogporten.frontchannelembed-url;type=text/markdown"
          }
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
          },
          
          {
              "id": uuidv7(),
              "displayName": [
                  {
                      "value": "tilbakemelding_807357263_BOSP924.xml",
                      "languageCode": "nb"
                  }
              ],
              "urls": [
                  {
                      "id": "0199f11b-b990-7451-b603-9149b9c1c262",
                      "url": "https://dialogarkiv-ekom-sit.apps.utv04.paas.skead.no/api/dialogarkiv/v1/innhold/0199f11a-f1e1-76fc-9a88-5bb00f53f966",
                      "consumerType": "GUI",
                      "mediaType": "application/xml"
                  }
              ]
          },
          {
              "id": uuidv7(),
              "displayName": [
                  {
                      "value": "tilbakemelding_807357263_BOSP924.pdf",
                      "languageCode": "nb"
                  }
              ],
              "urls": [
                  {
                      "id": uuidv7(),
                      "url": "https://dialogarkiv-ekom-sit.apps.utv04.paas.skead.no/api/dialogarkiv/v1/innhold/0199f11a-f1e1-7b95-9df7-497a377a585a",
                      "consumerType": "GUI",
                      "mediaType": "application/pdf"
                  }
              ]
          }
      ]
  }
  if (relatedTransmissionId != 0) {
      transmission.relatedTransmissionId = relatedTransmissionId;
  }
  return transmission;
}