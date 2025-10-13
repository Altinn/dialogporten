import {
    describe, expect, expectStatusFor,
    getEU,
    getSysEU,
    setVisibleFrom,
    setTitle,
    postSO,
    purgeSO
} from '../../common/testimports.js'
import {getDefaultEnduserSsn} from '../../common/token.js';
import {default as dialogToInsert} from '../serviceowner/testdata/01-create-dialog.js';

export default function () {
    describe('Get Single Dialog with SystemUser with empty party list', () => {
        // Arrange
        let dialog = dialogToInsert();
        setVisibleFrom(dialog, null);

        let r = postSO("dialogs", dialog);
        expectStatusFor(r).to.equal(201);
        let dialogId = r.json();

        // Assert
        let getResponse = getSysEU('dialogs/' + dialogId);
        expectStatusFor(getResponse).to.equal(404);

        // Clean up
        let purgeResponse = purgeSO("dialogs/" + dialogId);
        expect(purgeResponse.status, 'response status').to.equal(204);
    });


    describe('SystemUser with empty party list', () => {
        // Arrange
        let dialog = dialogToInsert();
        let title = "system titles";
        let defaultParty = "urn:altinn:person:identifier-no:" + getDefaultEnduserSsn();
        setVisibleFrom(dialog, null);
        setTitle(dialog, title);

        let r = postSO("dialogs", dialog);
        expectStatusFor(r).to.equal(201);
        let dialogId = r.json();

        // Assert
        let getResponse = getSysEU(`dialogs/?Search=${title}&Party=${defaultParty}`);
        expectStatusFor(getResponse).to.equal(200);
        expect(getResponse, 'response').to.have.validJsonBody();
        expect(getResponse.json(), 'response json').to.not.have.property('items')

        // Clean up
        let purgeResponse = purgeSO("dialogs/" + dialogId);
        expect(purgeResponse.status, 'response status').to.equal(204);
    });
}

