import { describe, expect, expectStatusFor, postSO, postBatchSO, purgeSO } from '../../common/testimports.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js';

export default function () {

    let dialogId = null;

    describe('Perform dialog create', () => {
        let r = postSO('dialogs', dialogToInsert());
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/)

        dialogId = r.json();
    });

    describe('Attempt to add a few child entities concurrently', async () => {

        let batch = [];

        for (let i=0; i<10; i++) {
            let activity = { type: "Information", performedBy: { actorType: "serviceOwner" }, description: [ { value: i.toString(), languageCode: "nb"}]};
            batch.push(['dialogs/' + dialogId + '/activities?' + i, activity]);
        }

        const results = postBatchSO(batch);

        // Cleanup here, as we're in another thread
        purgeSO('dialogs/' + dialogId);

        results.forEach((r) => {
            expect([201, 409], 'status code for concurrently added child entity').to.contain(r.status);
        });

    });
}
