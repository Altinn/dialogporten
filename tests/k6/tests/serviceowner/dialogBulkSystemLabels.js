import { describe, expect, expectStatusFor, postSO, getSO, purgeSO } from '../../common/testimports.js'
import { default as dialogToInsert } from './testdata/01-create-dialog.js'
import { getDefaultEnduserSsn } from '../../common/token.js'

export default function () {
    const dialogIds = [];
    const enduserId = 'urn:altinn:person:identifier-no:' + getDefaultEnduserSsn();

    describe('Create dialogs for bulk update', () => {
        for (let i = 0; i < 2; i++) {
            const r = postSO('dialogs', dialogToInsert());
            expectStatusFor(r).to.equal(201);
            dialogIds.push(r.json());
        }
    });

    describe('Bulk set system labels as service owner', () => {
        const body = {
            dialogs: dialogIds.map(id => ({ dialogId: id })),
            systemLabels: ['Archive']
        };
        const r = postSO(`dialogs/endusercontext/systemlabels/actions/bulkset?enduserId=${enduserId}`, body);
        expectStatusFor(r).to.equal(204);
    });

    describe('Verify dialogs have updated labels', () => {
        dialogIds.forEach(id => {
            const r = getSO(`dialogs/${id}?endUserId=${enduserId}`);
            expectStatusFor(r).to.equal(200);
            let result = r.json();
            expect(result.endUserContext.systemLabels).to.be.an('array').that.includes('Archive');
        });
    });

    describe('Cleanup', () => {
        dialogIds.forEach(id => {
            const r = purgeSO('dialogs/' + id);
            expectStatusFor(r).to.equal(204);
        });
    });
}
