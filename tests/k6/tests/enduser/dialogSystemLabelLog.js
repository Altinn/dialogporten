import {
    describe, expect, expectStatusFor, getEU, putEU, postSO, putSO, purgeSO, uuidv4
} from '../../common/testimports.js'

import {default as dialogToInsert} from '../serviceowner/testdata/01-create-dialog.js';

export default function () {
    let dialogId;
    let dialog;
    describe('Arrange: Create a dialog to test against', () => {
        let d = dialogToInsert();
        let r = postSO("dialogs", d);
        expectStatusFor(r).to.equal(201);
        dialogId = r.json();
        dialog = d;
    });

    describe('Update label as enduser', () => {
        let body = {
            'label': 'Bin'
        }
        let response = putEU('dialogs/' + dialogId + '/context/systemlabels', body);
        expectStatusFor(response).to.equal(204);
        response = getEU('dialogs/' + dialogId + '/labellog');
        expectStatusFor(response).to.equal(200);
        expect(response, 'response').to.have.validJsonBody();
        expect(response.json(), 'response body').to.have.lengthOf(1);
    });

    describe('Changing labels trigger 2 logs', () => {

        let body = {
            'label': 'archive'
        }
        let response = putEU('dialogs/' + dialogId + '/context/systemlabels', body);
        expectStatusFor(response).to.equal(204);
        response = getEU('dialogs/' + dialogId + '/labellog');
        expectStatusFor(response).to.equal(200);
        expect(response, 'response').to.have.validJsonBody();
        expect(response.json(), 'response body').to.have.lengthOf(3);
    })

    describe('Invalid revision if-match header results in 412 Precondition Failed', () => {

        let body = {
            'label': 'bin'
        }

        let invalidRevision = uuidv4();
        let params = {
            headers: {
                'If-Match': invalidRevision
            }
        }

        let response = putEU('dialogs/' + dialogId + '/context/systemlabels', body, params);
        expectStatusFor(response).to.equal(412);
    })

    describe('Dialog update set system label to default', () => {
        dialog.progress = "60";
        let response = putSO('dialogs/' + dialogId, dialog);
        expectStatusFor(response).to.equal(204);
        response = getEU('dialogs/' + dialogId + '/labellog');
        expectStatusFor(response).to.equal(200);
        expect(response, 'response').to.have.validJsonBody();
        expect(response.json(), 'response body').to.have.lengthOf(4);
    })

    describe('Cleanup', () => {
        let response = purgeSO("dialogs/" + dialogId);
        expectStatusFor(response).to.equal(204);
    })
}
