import {describe, expectStatusFor, expect, purgeSO, freezeSO, patchSO, postSO} from '../../common/testimports.js'
import {default as dialogToInsert} from './testdata/01-create-dialog.js';

export default function () {

    let dialogIds = [];
    let dialogId = null;

    describe('Perform dialog create', () => {
        let r = postSO('dialogs', dialogToInsert());
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/)

        dialogId = r.json();
        dialogIds.push(dialogId);
    });

    describe('Perform dialog freeze', () => {

        let adminScope = {
            scopes: "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search digdir:dialogporten.serviceprovider.admin"
        };
        const r = freezeSO('dialogs/' + dialogId, null , adminScope);
        console.log(r); 
        expectStatusFor(r).to.equal(204);
    });

    describe('Try updated frozen Dialog without admin', () => {
        const patchDocument = [
            {
                "op": "replace",
                "path": "/progress",
                "value": 98
            }
        ];
        const r = patchSO('dialogs/' + dialogId, patchDocument);
        expectStatusFor(r).to.equal(403)
    });


    describe('Try updated frozen Dialog as admin', () => {
        const patchDocument = [
            {
                "op": "replace",
                "path": "/progress",
                "value": 98
            }
        ];
        let adminScope = {
            scopes: "digdir:dialogporten.serviceprovider digdir:dialogporten.serviceprovider.search digdir:dialogporten.serviceprovider.admin",
        };
        const r = patchSO('dialogs/' + dialogId, patchDocument, null, adminScope);
        expectStatusFor(r).to.equal(204)
    });

    describe('Clean up dialogs', () => {
        for (const x in dialogIds) {
            let r = purgeSO('dialogs/' + dialogIds[x]);
            console.log(r)
            expectStatusFor(r).to.equal(204);
        }
    });
}
