import { postSO, purgeSO, expect, describe, getServiceOwnerTokenFromGenerator } from "../../../common/testimports.js";
import { default as dialogToInsert } from '../testdata/01-create-dialog.js';

export function setup() {
    // Get the token during setup stage so that it doesn't interfere with timings
    return {
        headers: {
            Authorization: "Bearer " + getServiceOwnerTokenFromGenerator()
        }
    }
}


export default function(paramsWithToken) {
    let dialogId = null;
    describe('create dialog', () => {
        let r = postSO('dialogs', dialogToInsert(), paramsWithToken);
        expect(r.status, 'response status').to.equal(201);
        dialogId = r.json();
    });

    describe('remove dialog', () => {
        if (dialogId) {
            let r = purgeSO('dialogs/' + dialogId, paramsWithToken);
            expect(r.status, 'response status').to.equal(204);
        }
    });
}
