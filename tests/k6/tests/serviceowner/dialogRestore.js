import {
    describe,
    expect,
    expectStatusFor,
    getSO,
    postSO,
    patchSO,
    deleteSO,
    purgeSO,
    setSystemLabel
} from '../../common/testimports.js'
import {default as dialogToInsert} from './testdata/01-create-dialog.js'

export default function () {
    let dialogId = null;
    let initialSystemLabel = "Bin";
    let initialEtag = null;
    let initialUpdatedAt = null;

    const restoreDialog = (dialogId, eTag = null) => {
        let header = eTag ? {'headers': {'Etag': eTag}} : null
        return postSO('dialogs/' + dialogId + '/actions/restore', null, header);
    }

    describe('Setup', () => {
        let dialog = dialogToInsert();
        setSystemLabel(dialog, initialSystemLabel);
        let r = postSO('dialogs', dialog);
        expectStatusFor(r).to.equal(201);
        expect(r, 'response').to.have.validJsonBody();
        expect(r.json(), 'response json').to.match(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

        dialogId = r.json();
        initialEtag = r.headers.Etag;

        let getResponse = getSO('dialogs/' + dialogId);
        expectStatusFor(getResponse).to.equal(200);
        expect(getResponse, 'get response').to.have.validJsonBody();
        expect(getResponse.json(), 'get response body').to.have.property('updatedAt');
        initialUpdatedAt = getResponse.json()['updatedAt']
    });

    describe('Restore not deleted dialog', () => {
        let r = restoreDialog(dialogId);
        expectStatusFor(r).to.equal(204);
        expect(r.headers.Etag, 'response Etag').to.equal(initialEtag);
    });

    describe('Restore deleted dialog', () => {
        let deleteResponse = deleteSO('dialogs/' + dialogId);
        expectStatusFor(deleteResponse).to.equal(204);
        let Etag = deleteResponse.headers.Etag;

        let r = restoreDialog(dialogId, Etag);
        expectStatusFor(r).to.equal(204);
        expect(r.headers.Etag).to.not.equal(Etag);

        let getResponse = getSO('dialogs/' + dialogId);
        expectStatusFor(getResponse).to.equal(200);
        expect(getResponse, 'get response').to.have.validJsonBody();
        expect(getResponse.json().endUserContext.systemLabels).to.be.an('array').that.includes(initialSystemLabel);
        expect(getResponse.json()['updatedAt'], 'get response updatedAt').to.equal(initialUpdatedAt);
    });

    describe('Clean up', () => {
        purgeSO('dialogs/' + dialogId);
    });

}
