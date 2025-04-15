import {default as createDialogPayload} from "../serviceowner/testdata/01-create-dialog.js"
import { sentinelPerformanceValue } from "../../common/config.js";

function cleanUp(originalPayload) {
    if (!originalPayload || typeof originalPayload !== 'object') {
        throw new Error('Invalid payload');
    }

    const payload = {
        ...originalPayload,
        searchTags: [...(originalPayload.searchTags || []), { "value": sentinelPerformanceValue }]
    };
    return payload
}

/**
 * Creates a dialog payload for performance testing
 * @param {string} endUser - Norwegian national ID number (11 digits)
 * @param {string} resource - Resource identifier
 * @returns {Object} Dialog payload
 * @throws {Error} If inputs are invalid
 */
export default function (endUser, resource) {
    if (!endUser?.match(/^\d{11}$/)) {
        throw new Error('endUser must be a 11-digit number');
    }
    if (!resource?.trim()) {
        throw new Error('resource is required');
    }
    let payload = createDialogPayload();
    payload.serviceResource = "urn:altinn:resource:" +resource;
    payload.party = "urn:altinn:person:identifier-no:" + endUser;
    delete payload.visibleFrom;

    return cleanUp(payload);
}
