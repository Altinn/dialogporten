/**
 * This script purges dialogs that have not been cleaned up by the tests.
 * The script is intended to be run after the main tests have completed.
 *
 * The script retrieves all dialogs that contain a sentinel value in the search query.
 * It then purges these dialogs.
 *
 * Run: k6 run tests/k6/tests/serviceowner/performance/purge-dialogs.js -e env=yt01
 */
import { uuidv4 } from '../../../common/k6-utils.js';
import { getSO, purgeSO } from '../../../common/request.js';
import { serviceOwners } from '../../performancetest_common/readTestdata.js';
import { expect, expectStatusFor } from "../../../common/testimports.js";
import { describe } from '../../../common/describe.js';
import { sentinelPerformanceValue as sentinelValue } from '../../../common/config.js';

const traceCalls = (__ENV.traceCalls ?? 'false') === 'true';

/**
 * Retrieves the dialog ids to purge.
 *
 * @param {Object} serviceOwner - The service owner object.
 * @returns {Array} - The dialog ids to purge.
 */
function getDialogs(serviceOwner) {
    var traceparent = uuidv4();
    var paramsWithToken = {
        headers: {
            Authorization: "Bearer " + serviceOwner.token,
            traceparent: traceparent
        },
        tags: { name: 'search dialogs' }
    }

    let hasNextPage = false;
    let continuationToken = "";
    let dialogIdsToPurge = [];
    do {
        traceparent = uuidv4();
        paramsWithToken.headers.traceparent = traceparent;
        if (traceCalls) {
            paramsWithToken.tags.traceparent = traceparent;
        }
        let r = getSO('dialogs/?Search=' + encodeURIComponent(sentinelValue) + continuationToken, paramsWithToken);
        expectStatusFor(r).to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
        let response = r.json();
        if (response.items && response.items.length > 0) {
            response.items.forEach((item) => {
                dialogIdsToPurge.push(item.id);
            });
            if (response.continuationToken) {
                continuationToken = "&continuationToken=" + response.continuationToken;
            } else {
                continuationToken = "";
            }
        }
        console.log("Found " + dialogIdsToPurge.length + " unpurged dialogs");
        hasNextPage = response.hasNextPage;
    } while (hasNextPage);
    return dialogIdsToPurge;
}

export let options = {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.5)', 'p(99.9)', 'count'],
    thresholds: {
        "http_req_duration{name:purge dialog}": [],
        "http_reqs{name:purge dialog}": []
    },
    setupTimeout: '4m',
};

export function setup() {
    let data = [];
    if (!serviceOwners || serviceOwners.length === 0) {
        throw new Error('No service owners loaded for testing');
    }
    for (const serviceOwner of serviceOwners) {
        let dialogIdsToPurge = getDialogs(serviceOwner);
        data.push({token: serviceOwner.token, dialogIdsToPurge: dialogIdsToPurge});
    }

    return data;
}

/**
 * Purges dialogs.
 * In single user mode, the first service owner is used. Only one iteration is performed.
 * In multi user mode, all service owners are used.
 */
export default function(serviceOwners) {
    if (!serviceOwners || serviceOwners.length === 0) {
        throw new Error('No service owners loaded for testing');
    }

    const isSingleUserMode = (options.vus ?? 1) === 1 && (options.iterations ?? 1) === 1 && (options.duration ?? 0) === 0;
    if (isSingleUserMode) {
      purgeDialogs(serviceOwners[0]);
    }
    else {
        for (const serviceOwner of serviceOwners) {
            purgeDialogs(serviceOwner);
        }
    }
  }

/**
 * Purges dialogs.
 *
 * @param {Object} serviceOwner - The service owner object.
 */
export function purgeDialogs(serviceOwner) {
    var traceparent = uuidv4();
    var paramsWithToken = {
        headers: {
            Authorization: "Bearer " + serviceOwner.token,
            traceparent: traceparent
            },
        tags: { name: 'purge dialog'}
    }

    describe('Post run: checking for unpurged dialogs', () => {
        let dialogIdsToPurge = serviceOwner.dialogIdsToPurge;
        if (dialogIdsToPurge.length > 0) {
            console.error("Found " + dialogIdsToPurge.length + " unpurged dialogs, make sure that all tests clean up after themselves. Purging ...");
            for(var i = dialogIdsToPurge.length - 1; i>=0; i--) {
                traceparent = uuidv4();
                paramsWithToken.headers.traceparent = traceparent;
                if (traceCalls) {
                    paramsWithToken.tags.traceparent = traceparent;
                }
                let r = purgeSO('dialogs/' + dialogIdsToPurge[i], paramsWithToken);
                if (r.status != 204) {
                    console.error("Failed to purge dialog with id: " + dialogIdsToPurge[i]);
                    console.log(r);
                }
                else {
                    console.log("Purged dialog with id: " + dialogIdsToPurge[i]);
                    dialogIdsToPurge.splice(i, 1);
                }
            }
        }

        // Fail the test after purging for visibility
        expect(dialogIdsToPurge.length, 'unpurged dialogs').to.equal(0);
    });
}
