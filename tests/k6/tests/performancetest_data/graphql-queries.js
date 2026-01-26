const getAllDialogsForPartyQuery = {
  query: "query getAllDialogsForParties($partyURIs: [String!], $search: String, $org: [String!], $status: [DialogStatus!], $continuationToken: String, $limit: Int, $label: [SystemLabel!], $createdAfter: DateTime, $createdBefore: DateTime, $updatedAfter: DateTime, $updatedBefore: DateTime, $searchLanguageCode: String, $serviceResources: [String!]) {\n  searchDialogs(\n    input: {party: $partyURIs, search: $search, org: $org, status: $status, continuationToken: $continuationToken, orderBy: {createdAt: null, updatedAt: null, dueAt: null, contentUpdatedAt: DESC}, systemLabel: $label, createdAfter: $createdAfter, createdBefore: $createdBefore, contentUpdatedAfter: $updatedAfter, contentUpdatedBefore: $updatedBefore, limit: $limit, excludeApiOnly: true, searchLanguageCode: $searchLanguageCode, serviceResource: $serviceResources}\n  ) {\n    items {\n      ...SearchDialogFields\n    }\n    hasNextPage\n    continuationToken\n  }\n}\n\nfragment SearchDialogFields on SearchDialog {\n  id\n  party\n  org\n  progress\n  guiAttachmentCount\n  status\n  createdAt\n  dueAt\n  contentUpdatedAt\n  hasUnopenedContent\n  serviceResourceType\n  serviceResource\n  seenSinceLastContentUpdate {\n    ...SeenLogFields\n  }\n  fromServiceOwnerTransmissionsCount\n  fromPartyTransmissionsCount\n  content {\n    title {\n      ...DialogContentFields\n    }\n    summary {\n      ...DialogContentFields\n    }\n    senderName {\n      ...DialogContentFields\n    }\n    extendedStatus {\n      ...DialogContentFields\n    }\n  }\n  endUserContext {\n    systemLabels\n  }\n}\n\nfragment SeenLogFields on SeenLog {\n  id\n  seenAt\n  seenBy {\n    actorType\n    actorId\n    actorName\n  }\n  isCurrentEndUser\n}\n\nfragment DialogContentFields on ContentValue {\n  mediaType\n  value {\n    value\n    languageCode\n  }\n}",
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