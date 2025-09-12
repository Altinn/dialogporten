const getAllDialogsForPartyQuery = {
  query: `query getAllDialogsForParties(
                   $partyURIs: [String!], 
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
  variables: { 
    partyURIs: [], 
    limit: 100,
    label: ["DEFAULT"],
    status: ["NOT_APPLICABLE", "IN_PROGRESS", "AWAITING", "REQUIRES_ATTENTION", "COMPLETED"] },
};

export function getGraphqlParty(enduser) {
    let request = JSON.parse(JSON.stringify(getAllDialogsForPartyQuery));
    request.variables.partyURIs.push(`urn:altinn:person:identifier-no:${enduser}`);
    return request;
}