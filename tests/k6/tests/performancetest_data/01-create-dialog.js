import {default as createDialogPayload} from "../serviceowner/testdata/01-create-dialog.js"
import { sentinelPerformanceValue } from "../../common/config.js";

const ACTIVITY_TYPE_INFORMATION = 'Information';

function cleanUp(originalPayload) {
    if (!originalPayload || typeof originalPayload !== 'object') {
        throw new Error('Invalid payload');
    }

    const payload = {
        ...originalPayload,
        searchTags: [...(originalPayload.searchTags || []), { "value": sentinelPerformanceValue }]
    };
    const { visibleFrom, ...payloadWithoutVisibleFrom } = payload;

    const activities = payload.activities?.map(activity => {
        if (activity.type !== ACTIVITY_TYPE_INFORMATION) {
            return activity;
        }

        const { performedBy, ...rest } = activity;
        const { actorId, ...performedByRest } = performedBy;

        return {
            ...rest,
            performedBy: {
                ...performedByRest,
                actorName: "some name"
            }
        };
    }) ?? [];

    return {
        ...payloadWithoutVisibleFrom,
        activities
    };
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

    return cleanUp(payload);
}
