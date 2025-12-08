const getAllDialogsForPartyQuery = {
  query: `query getAllDialogsForParties(
                   $partyURIs: [String!], 
                   $serviceResource: [String!],
                   $search: String, 
                   $org: [String!], 
                   $status: [DialogStatus!], 
                   $continuationToken: String, 
                   $limit: Int, 
                   $label: [SystemLabel!], 
                   $createdAfter: DateTime, 
                   $createdBefore: DateTime, 
                   $updatedAfter: DateTime, 
                   $updatedBefore: DateTime) 
                   {
                       searchDialogs(
                          input: {
                            party: $partyURIs, 
                            search: $search,
                            serviceResource: $serviceResource, 
                            org: $org, 
                            status: $status, 
                            continuationToken: $continuationToken, 
                            orderBy: {
                              createdAt: null, 
                              updatedAt: null, 
                              dueAt: null, 
                              contentUpdatedAt: DESC
                            }, 
                            systemLabel: $label, 
                            createdAfter: $createdAfter, 
                            createdBefore: $createdBefore, 
                            updatedAfter: $updatedAfter, 
                            updatedBefore: $updatedBefore, 
                            limit: $limit, 
                            excludeApiOnly: true
                          }
                        ) {
                            items {
                                ...SearchDialogFields
                                }
                            hasNextPage
                            continuationToken 
                            errors { message }
                          }
                    }
                          
                    fragment SearchDialogFields on SearchDialog {
                      id
                      party
                      org
                      progress
                      guiAttachmentCount
                      status
                      createdAt
                      updatedAt
                      dueAt
                      contentUpdatedAt
                      hasUnopenedContent
                      extendedStatus
                      seenSinceLastContentUpdate {
                        ...SeenLogFields
                      }  
                      fromServiceOwnerTransmissionsCount
                      fromPartyTransmissionsCount
                      content {
                        title {
                          ...DialogContentFields
                        }
                        summary {
                          ...DialogContentFields
                        }
                        senderName {
                          ...DialogContentFields
                        }
                        extendedStatus {
                          ...DialogContentFields
                        }
                      }
                      endUserContext {
                        systemLabels
                      }
                    }
                      
                    fragment SeenLogFields on SeenLog {
                      id
                      seenAt
                      seenBy {
                        actorType
                        actorId
                        actorName
                      }
                      isCurrentEndUser
                    }
                    
                    fragment DialogContentFields on ContentValue {
                      mediaType
                      value {
                        value
                        languageCode
                      }
                  }`,
  variables: {},
};

const getAllDialogsForCountQuery = {
  query: `query getAllDialogsForCount($partyURIs: [String!], $search: String, $org: [String!], $status: [DialogStatus!], $label: [SystemLabel!], $createdAfter: DateTime, $createdBefore: DateTime, $updatedAfter: DateTime, $updatedBefore: DateTime, $limit: Int) {\n  searchDialogs(\n    input: {party: $partyURIs, search: $search, org: $org, status: $status, orderBy: {createdAt: null, updatedAt: null, dueAt: null, contentUpdatedAt: DESC}, systemLabel: $label, createdAfter: $createdAfter, createdBefore: $createdBefore, updatedAfter: $updatedAfter, updatedBefore: $updatedBefore, limit: $limit, excludeApiOnly: true}\n  ) {\n    items {\n      ...CountableDialogFields\n    }\n    hasNextPage\n errors { message }  }\n}\n\nfragment CountableDialogFields on SearchDialog {\n  id\n  org\n  party\n  updatedAt\n  status\n  endUserContext {\n    systemLabels\n  }\n  seenSinceLastContentUpdate {\n    isCurrentEndUser\n  }\n}`,
  variables: {},
};

const partiesQuery = {
  query: `
        query parties {
            parties {
            ...partyFields
                subParties {
                ...subPartyFields
                }
            }
        }
            
        fragment partyFields on AuthorizedParty {
            party
            hasOnlyAccessToSubParties
            partyType
            subParties {
                ...subPartyFields
                }
            isAccessManager
            isMainAdministrator
            name
            isCurrentEndUser
            isDeleted
            partyUuid
        }
            
        fragment subPartyFields on AuthorizedSubParty {
            party
            partyType
            isAccessManager
            isMainAdministrator
            name
            isCurrentEndUser
            isDeleted
            partyUuid
        }`,
}

const searchAutoComplete = {
  query: `query getSearchAutocompleteDialogs($partyURIs: [String!], $search: String, $org: [String!], $status: [DialogStatus!], $createdAfter: DateTime, $createdBefore: DateTime) {
    searchDialogs(
      input: {party: $partyURIs, search: $search, org: $org, status: $status, orderBy: {createdAt: null, updatedAt: null, dueAt: null, contentUpdatedAt: DESC}, createdAfter: $createdAfter, createdBefore: $createdBefore, excludeApiOnly: true}
    ) {
      items {
        ...SearchAutocompleteDialogFields
      }
    }
  }
  
  fragment SearchAutocompleteDialogFields on SearchDialog {
    id
    seenSinceLastContentUpdate {
      ...SeenLogFields
    }
    content {
      title {
        ...DialogContentFields
      }
      summary {
        ...DialogContentFields
      }
    }
  }
  
  fragment SeenLogFields on SeenLog {
    id
    seenAt
    seenBy {
      actorType
      actorId
      actorName
    }
    isCurrentEndUser
  }
  
  fragment DialogContentFields on ContentValue {
    mediaType
    value {
      value
      languageCode
    }
  }`
}

const getDialogByIdQuery = {
  operationName: 'getDialogById',
  query: `query getDialogById($id: UUID!) {\n  dialogById(dialogId: $id) {\n    dialog {\n      ...DialogByIdFields\n    }\n    errors {\n      __typename\n      message\n    }\n  }\n}\n\nfragment DialogByIdFields on Dialog {\n  id\n  dialogToken\n  party\n  org\n  progress\n  serviceResourceType\n  fromServiceOwnerTransmissionsCount\n  fromPartyTransmissionsCount\n  attachments {\n    ...AttachmentFields\n  }\n  activities {\n    ...DialogActivity\n  }\n  guiActions {\n    ...GuiActionFields\n  }\n  seenSinceLastContentUpdate {\n    ...SeenLogFields\n  }\n  transmissions {\n    ...TransmissionFields\n  }\n  status\n  dueAt\n  createdAt\n  contentUpdatedAt\n  endUserContext {\n    systemLabels\n  }\n  content {\n    title {\n      ...DialogContentFields\n    }\n    summary {\n      ...DialogContentFields\n    }\n    senderName {\n      ...DialogContentFields\n    }\n    additionalInfo {\n      ...DialogContentFields\n    }\n    extendedStatus {\n      ...DialogContentFields\n    }\n    mainContentReference {\n      ...DialogContentFields\n    }\n  }\n}\n\nfragment AttachmentFields on Attachment {\n  id\n  displayName {\n    value\n    languageCode\n  }\n  urls {\n    ...AttachmentUrlFields\n  }\n}\n\nfragment AttachmentUrlFields on AttachmentUrl {\n  id\n  url\n  consumerType\n  mediaType\n}\n\nfragment DialogActivity on Activity {\n  id\n  transmissionId\n  performedBy {\n    actorType\n    actorId\n    actorName\n  }\n  description {\n    value\n    languageCode\n  }\n  type\n  createdAt\n}\n\nfragment GuiActionFields on GuiAction {\n  id\n  url\n  isAuthorized\n  isDeleteDialogAction\n  action\n  authorizationAttribute\n  priority\n  httpMethod\n  title {\n    languageCode\n    value\n  }\n  prompt {\n    value\n    languageCode\n  }\n}\n\nfragment SeenLogFields on SeenLog {\n  id\n  seenAt\n  seenBy {\n    actorType\n    actorId\n    actorName\n  }\n  isCurrentEndUser\n}\n\nfragment TransmissionFields on Transmission {\n  id\n  isAuthorized\n  createdAt\n  type\n  sender {\n    actorType\n    actorId\n    actorName\n  }\n  relatedTransmissionId\n  content {\n    title {\n      value {\n        value\n        languageCode\n      }\n      mediaType\n    }\n    summary {\n      value {\n        value\n        languageCode\n      }\n      mediaType\n    }\n    contentReference {\n      value {\n        value\n        languageCode\n      }\n      mediaType\n    }\n  }\n  attachments {\n    id\n    displayName {\n      value\n      languageCode\n    }\n    urls {\n      id\n      url\n      consumerType\n      mediaType\n    }\n  }\n}\n\nfragment DialogContentFields on ContentValue {\n  mediaType\n  value {\n    value\n    languageCode\n  }\n}`,
  variables: { id: '' },
};

export function getGraphqlRequestBodyForDialogById(dialogId) {
  let request = JSON.parse(JSON.stringify(getDialogByIdQuery));
  request.variables.id = dialogId;
  return request;
}


export function getGraphqlRequestBodyForAllDialogsForParty(variables) {
  let request = JSON.parse(JSON.stringify(getAllDialogsForPartyQuery));
  request.variables = {...request.variables, ...variables};
  return request;
}

export function getGraphqlRequestBodyForAllDialogsForCount(variables) {
  let request = JSON.parse(JSON.stringify(getAllDialogsForCountQuery));
  request.variables = {...request.variables, ...variables};
  return request;
}

export function getPartiesRequestBody() {
  let request = JSON.parse(JSON.stringify(partiesQuery));
  return request;
}

export function getSearchAutoCompleteRequestBody(variables) {
  let request = JSON.parse(JSON.stringify(searchAutoComplete));
  request.variables = {...request.variables, ...variables};
  return request;
}