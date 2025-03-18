import { describe, expect, expectStatusFor, getSO, postSO, putSO, patchSO, deleteSO, purgeSO } from '../../common/testimports.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {

    let validSo = null;
    let invalidSo = { orgName: "other", orgNo: "310778737" };
    let validSoAdmin = { orgName: "other", orgNo: "991825827", scopes: "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.admin" };

    let dialog = dialogToInsert();
    let transmissionId = null; // known after successful insert
    let dialogId = null; // known after successful insert
    let activity = dialog.activities[0];
    let activityId = null; // known after successful insert

    let expectEither = function (statusCodeSuccess, statusCodeFailure, r, shouldSucceed) {
        expectStatusFor(r).to.equal(shouldSucceed ? statusCodeSuccess : statusCodeFailure);
    }

    let permutations = [
        [ true, validSo ],
        [ false, invalidSo ],
        [ true, validSoAdmin ]
    ];

    permutations.forEach(([shouldSucceed, tokenOptions]) => {
        let logPrefix = shouldSucceed ? "Allow" : "Deny";
        let logSuffix = shouldSucceed ? "valid serviceowner" : "invalid serviceowner"
        if (tokenOptions && tokenOptions["scopes"] && tokenOptions["scopes"].indexOf("admin") > -1) {
            logSuffix += " (admin)";
        }

        describe(`${logPrefix} dialog create as ${logSuffix}`, () => {
            let r = postSO('dialogs', dialog, null, tokenOptions);
            expectEither(201, 403, r, shouldSucceed);
            if (r.status == 201) {
                dialogId = r.json();
            }
        });

        describe(`${logPrefix} getting dialog as ${logSuffix}`, () => {
            let r = getSO('dialogs/' + dialogId, null, tokenOptions);
            expectEither(200, 404, r, shouldSucceed);
            if (r.status == 200) {
                let d = r.json();
                activityId = d.activities[0].id;
                transmissionId = d.transmissions[0].id;
            }
        });

        describe(`${logPrefix} patching dialog as ${logSuffix}`, () => {
            let patchDocument = [
                {
                    "op": "replace",
                    "path": "/apiActions/0/endpoints/1/url",
                    "value": "https://vg.no"
                }
            ];
            let r = patchSO('dialogs/' + dialogId, patchDocument, null, tokenOptions);
            expectEither(204, 404, r, shouldSucceed);
        });

        describe(`${logPrefix} updating dialog as ${logSuffix}`, () => {
            let r = putSO('dialogs/' + dialogId, dialog, null, tokenOptions);
            expectEither(204, 404, r, shouldSucceed);
        });


        describe(`${logPrefix} deleting dialog as ${logSuffix}`, () => {
            // Only check that we cannot delete is as invalid SO so
            // that the dialog still exists for subsequent tests
            if (shouldSucceed) return;
            let r = deleteSO('dialogs/' + dialogId, null, tokenOptions);
            expectEither(204, 404, r, shouldSucceed);
        });


        describe(`${logPrefix} getting dialog transmission list as ${logSuffix}`, () => {
            let r = getSO('dialogs/' + dialogId + "/transmissions/", null, tokenOptions);
            expectEither(200, 404, r, shouldSucceed);
        });

        describe(`${logPrefix} getting dialog transmission as ${logSuffix}`, () => {
            let r = getSO('dialogs/' + dialogId + "/transmissions/" + transmissionId, null, tokenOptions);
            expectEither(200, 404, r, shouldSucceed);
        });

        describe(`${logPrefix} getting activities as ${logSuffix}`, () => {
            let r = getSO('dialogs/' + dialogId + "/activities", null, tokenOptions);
            expectEither(200, 404, r, shouldSucceed);
        });

        describe(`${logPrefix} getting activity entry as ${logSuffix}`, () => {
            let r = getSO('dialogs/' + dialogId + "/activities/" + activityId, null, tokenOptions);
            expectEither(200, 404, r, shouldSucceed);
        });

        describe(`${logPrefix} posting activity as ${logSuffix}`, () => {
            let r = postSO('dialogs/' + dialogId + "/activities", activity, null, tokenOptions);
            expectEither(201, 404, r, shouldSucceed);
        });

    });

    // Finally, cleanup by deleting the dialog
    describe('Allow purging dialog as valid serviceowner', () => {
        let r = purgeSO('dialogs/' + dialogId);
        expect(r.status, 'response status').to.equal(204);
    });

    // Check that searching cannot be done without search scope
    describe('Deny searching dialogs without search-scope', () => {
        let r = getSO('dialogs', null, { "scopes": "digdir:dialogporten.serviceprovider" } );
        expect(r.status, 'response status').to.equal(403);
    });

}
