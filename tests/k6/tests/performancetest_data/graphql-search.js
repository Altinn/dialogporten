

export function getGraphqlParty(inputs) {
    let inputStr  = "";
    const notListElements = ["createdAfter", "createdBefore", "updatedAfter", "updatedBefore", "dueAfter", "dueBefore", "search"];
    for (var key in inputs) {
        if (inputStr.length > 0) {
            inputStr += ", ";
        }
        if (notListElements.includes(key)) {
            inputStr += `${key}: "${inputs[key]}" `
        }
        else {
            inputStr += `${key}:  [ "${inputs[key]}" ] `
        }
    }

    return `
        query getAllDialogsForParties {
            searchDialogs(input: { ${inputStr} }) {
            items {
                id
                party
                org
                progress
                guiAttachmentCount
                status
                createdAt
                updatedAt
                extendedStatus
                seenSinceLastUpdate {
                    id
                    seenAt
                    seenBy {
                        actorType
                        actorId
                        actorName
                    }
                    isCurrentEndUser
                }
                latestActivity {
                    description {
                        value
                        languageCode
                    }
                    performedBy {
                        actorType
                        actorId
                        actorName
                    }
                }
                content {
                    title {
                        mediaType
                        value {
                            value
                            languageCode
                        }
                    }
                    summary {
                        mediaType
                        value {
                            value
                        languageCode
                    }
                }
                senderName {
                    mediaType
                    value {
                        value
                        languageCode
                    }
                }
                extendedStatus {
                    mediaType
                    value {
                        value
                        languageCode
                    }
                }
            }
            systemLabel
          }
        }
    }`
}