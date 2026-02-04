import { expect, expectStatusFor, describe, getSO, purgeSO } from './testimports.js'
import { sentinelValue } from './config.js';

export default function () {

    let dialogId = null;
    const tokenOptions = {
        scopes: "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search digdir:dialogporten.serviceprovider.admin digdir:dialogporten.correspondence"
    }
    describe('Post run: checking for unpurged dialogs', () => {
        let hasNextPage = false;
        let continuationToken = "";
        let dialogIdsToPurge = [];
        do {
            let r = getSO('dialogs/?Limit=1000&ServiceOwnerLabels=' + sentinelValue + continuationToken, null, tokenOptions);
            expectStatusFor(r).to.equal(200);
            expect(r, 'response').to.have.validJsonBody();
            let response = r.json();
            if (response.items && response.items.length > 0) {
                response.items.forEach((item) => {
                    dialogIdsToPurge.push(item.id);
                });
                continuationToken = "&continuationToken=" + response.continuationToken;
            }
            hasNextPage = response.hasNextPage;
        } while (hasNextPage);

        if (dialogIdsToPurge.length > 0) {
            console.error("Found " + dialogIdsToPurge.length + " unpurged dialogs, make sure that all tests clean up after themselves. Purging ...");
            dialogIdsToPurge = dialogIdsToPurge.filter((id) => {
                console.warn("Sentinel purging dialog with id: " + id)
                let r = purgeSO('dialogs/' + id, null, tokenOptions);
                if (r.status != 204) {
                    console.error("Failed to purge dialog with id: " + id);
                    return true; // Keep in the array if purge failed
                }
                return false; // Remove from the array if purge succeeded
            });
        }

        // Fail the test after purging for visibility
        expect(dialogIdsToPurge.length, 'unpurged dialogs').to.equal(0);
    });
}
