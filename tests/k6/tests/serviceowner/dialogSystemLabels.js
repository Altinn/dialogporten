import {
    describe,
    expect,
    expectStatusFor,
    postSO,
    putSO,
    getSO,
    purgeSO,
    uuidv4
} from '../../common/testimports.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js'
import { getDefaultEnduserSsn } from '../../common/token.js'

export default function () {
    let dialogId = null;
    const enduserId = 'urn:altinn:person:identifier-no:' + getDefaultEnduserSsn();

    describe('Create dialog', () => {
        let r = postSO('dialogs', dialogToInsert());
        expectStatusFor(r).to.equal(201);
        dialogId = r.json();
    });

    describe('Update label as service owner', () => {
        let body = {
            'systemLabels': ['Bin']
        }
        let r = putSO(`dialogs/${dialogId}/endusercontext/systemlabels?enduserId=${enduserId}`, body);
        expectStatusFor(r).to.equal(204);
    });

    describe('Verify dialog has updated label', () => {
        let r = getSO(`dialogs/${dialogId}?endUserId=${enduserId}`);
        expectStatusFor(r).to.equal(200);
        expect(r.json()['systemLabel'], 'system label').to.equal('Bin');
    });

    describe('Reject multiple system labels', () => {
        let body = {
            'systemLabels': ['Bin', 'Archive']
        }
        let r = putSO(`dialogs/${dialogId}/endusercontext/systemlabels?enduserId=${enduserId}`, body);
        expectStatusFor(r).to.equal(400);
    });

    describe('Invalid revision if-match header results in 412 Precondition Failed', () => {
        let body = {
            'systemLabels': ['Archive']
        }
        let params = {
            headers: {
                'If-Match': uuidv4()
            }
        }
        let r = putSO(`dialogs/${dialogId}/endusercontext/systemlabels?enduserId=${enduserId}`, body, params);
        expectStatusFor(r).to.equal(412);
    });

    describe('Cleanup', () => {
        let r = purgeSO('dialogs/' + dialogId);
        expectStatusFor(r).to.equal(204);
    });
}
