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
import { defaultServiceOwnerOrgNo } from '../../common/config.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js'
import { getDefaultEnduserSsn, getDefaultServiceOwnerOrgNo } from '../../common/token.js'

export default function () {
    let dialogId = null;
    let dialogIdNotAuthorized = null;
    const accessibleDialogs = [];
    const enduserId = 'urn:altinn:person:identifier-no:' + getDefaultEnduserSsn();

    describe('Create dialogs to bulk update', () => {
        for (let i = 0; i < 2; i++) {
            let d = dialogToInsert();
            const r = postSO('dialogs', d, null);
            expectStatusFor(r).to.equal(201);
            accessibleDialogs.push(r.json());
        }
    });

    describe('Bulk set labels for accessible dialogs SO', () => {
        const body = { dialogs: accessibleDialogs.map(id => ({ dialogId: id })), systemLabels: ['Bin'] }
        const r = postSO(`dialogs/endusercontext/systemlabels/actions/bulkset?enduserId=${enduserId}`, body);
        expectStatusFor(r).to.equal(204);
        accessibleDialogs.forEach(id => {
            const r2 = getSO('dialogs/' + id);
            expectStatusFor(r2).to.equal(200);
            expect(r2.json().endUserContext.systemLabels).to.be.an('array').that.includes('Bin');
        });
    });
    
    describe('Create dialogs', () => {
        let r = postSO('dialogs', dialogToInsert());
        expectStatusFor(r).to.equal(201);
        dialogId = r.json();

        let d2 = dialogToInsert();
        d2.party = 'urn:altinn:organization:identifier-no:' + getDefaultServiceOwnerOrgNo();
        r = postSO('dialogs', d2);
        expectStatusFor(r).to.equal(201);
        dialogIdNotAuthorized = r.json();
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
        expect(r.json().endUserContext.systemLabels).to.be.an('array').that.includes('Bin');

    });

    describe('Accept multiple system labels', () => {
        let body = {
            'systemLabels': ['Bin', 'Archive']
        }
        let r = putSO(`dialogs/${dialogId}/endusercontext/systemlabels?enduserId=${enduserId}`, body);
        expectStatusFor(r).to.equal(204);
    });

    describe('Invalid revision if-match header results in 412 Precondition Failed', () => {
        let body = {
            'addLabels': ['Bin']
        }
        let params = {
            headers: {
                'If-Match': uuidv4()
            }
        }
        let r = putSO(`dialogs/${dialogId}/endusercontext/systemlabels?enduserId=${enduserId}`, body, params);
        expectStatusFor(r).to.equal(412);
    });

    describe('Reject unauthorized dialog update', () => {
        let body = {
            'systemLabels': ['Archive']
        };
        let r = putSO(`dialogs/${dialogIdNotAuthorized}/endusercontext/systemlabels?enduserId=${enduserId}`, body);
        expectStatusFor(r).to.equal(404);
    });

    describe('Cleanup', () => {
        for (let id of [dialogId, dialogIdNotAuthorized]) {
            let r = purgeSO('dialogs/' + id);
            expectStatusFor(r).to.equal(204);
        }
    });
}
