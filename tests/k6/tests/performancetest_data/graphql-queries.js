export const getAllDialogsForPartyQuery = {
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

export function getGraphqlRequestBodyForAllDialogsForParty(variables) {
  let request = JSON.parse(JSON.stringify(getAllDialogsForPartyQuery));
  request.variables = {...request.variables, ...variables};
  return request;
}