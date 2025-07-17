import { customConsole as console } from './console.js';
import { sentinelValue } from './config.js';

export function setIsApiOnly(dialog, isApiOnly = true) {
    dialog.isApiOnly = isApiOnly;
}

export function setTitle(dialog, title, language = "nb") {
    setContent(dialog, "Title", title, language);
}

export function setAdditionalInfo(dialog, additionalInfo, language = "nb") {
    setContent(dialog, "AdditionalInfo", additionalInfo, language);
}

export function setContent(dialog, type, value, language = "nb") {
    if (typeof value !== "string") {
        throw new Error("Invalid value provided.");
    }

    dialog.content = dialog.content || {};

    if(dialog.content[`${type}`]) {
        const lang_index = dialog.content[`${type}`].value.findIndex(t => t.languageCode === language);
        if (lang_index !== -1) {
            dialog.content[`${type}`].value[lang_index].value = value;
        }
        else {
            dialog.content[`${type}`].value.push({ languageCode: language, value: value });
        }
    }
    else {
        dialog.content[`${type}`] = {
            value: [ { languageCode: language, value: value } ]
        }
    }
}

export function setSearchTags(dialog, searchTags) {
    if (!Array.isArray(searchTags) || searchTags.some(tag => typeof tag !== "string")) {
        throw new Error("Invalid search tags provided.");
    }
    let tags = [];
    searchTags.forEach((t) => {
        tags.push({ "value": t });
    })

    dialog.searchTags = tags;
}

export function setSenderName(dialog, senderName, language = "nb") {
    setContent(dialog, "SenderName", senderName, language);
}

export function setStatus(dialog, status) {
    const validStatuses = ["notApplicable", "inProgress", "draft", "awaiting", "requiresAttention", "completed"];

    if (!validStatuses.includes(status)) {
        throw new Error("Invalid status provided.");
    }

    dialog.status = status;
}

export function setExtendedStatus(dialog, extendedStatus) {
    if (!isValidURI(extendedStatus)) {
        throw new Error("Invalid extended status provided.");
    }

    dialog.extendedStatus = extendedStatus;
}

export function setServiceResource(dialog, serviceResource) {
    if (typeof serviceResource !== "string" || !serviceResource.startsWith("urn:altinn:resource:")) {
        throw new Error("Invalid service resource provided.");
    }

    dialog.serviceResource = serviceResource;
}

export function setParty(dialog, party) {
    const partyRegex = /^urn:altinn:([\w-]{5,20}):([\w-]{4,20}):([\w-]{5,36})$/;

    if (!partyRegex.test(party)) {
        throw new Error("Invalid party provided.");
    }

    dialog.party = party;
}

export function setProcess(dialog, process) {
    if (!isValidURI(process)) {
        throw new Error("Invalid process provided. " + process);
    }
    dialog.process = process;
}

export function setDueAt(dialog, dueAt) {
    if (dueAt == null) {
        delete dialog.dueAt;
        return;
    }

    if (dueAt instanceof Date) {
        dueAt = dateToUTCString(dueAt);
    }

    if (!validateUTCDate(dueAt)) {
        throw new Error("Invalid dueAt date provided: " + dueAt);
    }

    dialog.dueAt = dueAt;
}

export function setExpiresAt(dialog, expiresAt) {
    if (expiresAt == null) {
        delete dialog.expiresAt;
        return;
    }

    if (expiresAt instanceof Date) {
        expiresAt = dateToUTCString(expiresAt);
    }

    if (!validateUTCDate(expiresAt)) {
        throw new Error("Invalid expiresAt date provided: " + expiresAt);
    }

    dialog.expiresAt = expiresAt;
}

export function setVisibleFrom(dialog, visibleFrom) {
    if (visibleFrom == null) {
        delete dialog.visibleFrom;
        return;
    }

    if (visibleFrom instanceof Date) {
        visibleFrom = dateToUTCString(visibleFrom);
    }

    if (!validateUTCDate(visibleFrom)) {
        throw new Error("Invalid visibleFrom date provided:" + visibleFrom);
    }

    dialog.visibleFrom = visibleFrom;
}
export function setSystemLabel(dialog, systemLabel) {
    let validLabels = ['Default', 'Bin', 'Archive']
    if (!validLabels.includes(systemLabel)) {
       throw new Error("Invalid systemLabel provided.");
    }
    dialog.systemLabel = systemLabel;
}

export function setServiceOwnerLabels(dialog, serviceOwnerLabels) {
    if (!Array.isArray(serviceOwnerLabels) || serviceOwnerLabels.some(label => typeof label !== "string")) {
        throw new Error("Invalid service owner labels provided");
        console.log(serviceOwnerLabels);
    }
    let labels = [];
    serviceOwnerLabels.forEach((l) => {
        labels.push({ "value": l });
    })

    dialog.serviceOwnerContext = dialog.serviceOwnerContext || {};

    // Always add the sentinel value to the labels to ensure dialogs are cleaned up
    labels.push({ "value": sentinelValue });

    dialog.serviceOwnerContext.ServiceOwnerLabels = labels;
}

export function setActivities(dialog, activities) {
    if (activities == null) {
        delete dialog.activities;
        return;
    }
    dialog.activities = activities;
}

export function addActivity(dialog, activity) {
   if (activity == null) return;
   if (dialog.activities == null) {
        dialog.activities = [];
   }
   dialog.activities.push(activity);
}

function dateToUTCString(date) {
    let dateStr = date.toISOString();
    const ms = ('000' + date.getUTCMilliseconds()).slice(-3);
    return dateStr.substr(0, 20) + ms + '0000' + 'Z';
}

function validateUTCDate(date) {
    const dateRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{7}Z$/;
    return dateRegex.test(date);
}

function isValidURI(uri) {
    // A basic regex for RFC 2396 URI validation
    var pattern = /^([a-zA-Z][a-zA-Z0-9+-\.]*:)(\/\/[^/?#]*)?([^?#]*)(\?[^#]*)?(#.*)?$/;
    return pattern.test(uri);
}
