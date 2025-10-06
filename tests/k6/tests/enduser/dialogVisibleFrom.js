import {
    describe, expect, expectStatusFor,
    getEU,
    setVisibleFrom,
    postSO,
    purgeSO
} from '../../common/testimports.js'

import {default as dialogToInsert} from '../serviceowner/testdata/01-create-dialog.js';

export default function () {

    describe('Getting Non Visible Dialog returns 404', () => {
        // Arrange
        let dialog = dialogToInsert();

        // Make sure Date is always in the future
        let visibleFrom = new Date();
        visibleFrom.setMonth(visibleFrom.getMonth() + 1);
        visibleFrom.setMilliseconds(0);

        setVisibleFrom(dialog, visibleFrom);

        let r = postSO("dialogs", dialog);
        expectStatusFor(r).to.equal(201);
        let dialogId = r.json();

        // Assert
        let getResponse = getEU('dialogs/' + dialogId);
        expectStatusFor(getResponse).to.equal(404);
        expect(new Date(getResponse.headers.Expires).toISOString(), 'Expires at').to.equal(visibleFrom.toISOString());

        // Clean up
        let purgeResponse = purgeSO("dialogs/" + dialogId);
        expect(purgeResponse.status, 'response status').to.equal(204);
    });
    
    describe('Getting Visible Dialog returns 200', () => {

        // Arrange
        let dialog = dialogToInsert();
        setVisibleFrom(dialog, null);

        let r = postSO("dialogs", dialog);
        expectStatusFor(r).to.equal(201);
        let dialogId = r.json();

        // Assert
        let getResponse = getEU('dialogs/' + dialogId);
        expectStatusFor(getResponse).to.equal(200);
        expect(r, 'Response').to.have.validJsonBody();
        let id = r.json();
        expect(id, 'dialog id').to.equal(dialogId)

        // Clean up
        let purgeResponse = purgeSO("dialogs/" + dialogId);
        expect(purgeResponse.status, 'response status').to.equal(204);
    })
}
